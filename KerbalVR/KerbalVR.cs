using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    // start plugin at startup
    //
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class KerbalVR : MonoBehaviour
    {
        // this function allows importing DLLs from a given path
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);


        #region Properties

        // enable and disable VR functionality
        private static bool _hmdIsEnabled;
        public static bool HmdIsEnabled {
            get { return _hmdIsEnabled; }
            set {
                _hmdIsEnabled = value;

                if (_hmdIsEnabled && hmdIsInitialized) {
                    Scene.SetupScene();
                    ResetInitialHmdPosition();
                }
            }
        }

        // check if VR can be enabled
        public static bool HmdIsAllowed { get; private set; }

        // keep track of HMD running
        public static bool HmdIsRunning { get; private set; }

        #endregion


        #region Private Members

        // hold a reference to the app launcher GUI
        private AppGUI gui;

        // keep track of when the HMD is rendering images
        private static bool hmdIsInitialized = false;
        private static bool hmdIsRunningPrev = false;

        // defines the bounds to texture bounds for rendering
        private VRTextureBounds_t hmdTextureBounds;

        // these arrays each hold one object for the corresponding eye, where
        // index 0 = Left_Eye, index 1 = Right_Eye
        private Texture2D[] leftEyeRenderTextures = new Texture2D[5];
        private Texture2D[] rightEyeRenderTextures = new Texture2D[5];
        private RenderTexture[] renderTextures = new RenderTexture[5];

        private Texture_t openvrTexture;

        // store the tracked device poses
        private static TrackedDevicePose_t[] devicePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private static TrackedDevicePose_t[] renderPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private static TrackedDevicePose_t[] gamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        public static int qualityLevel = 0;
        #endregion

        #region Debug
        #endregion


        /// <summary>
        /// Overrides the Awake method for a MonoBehaviour plugin.
        /// Initialize class members.
        /// </summary>
        void Awake() {
            Utils.Log(Globals.KERBALVR_NAME + " plugin starting...");
            
            // init objects
            gui = new AppGUI();
            _hmdIsEnabled = false;
            HmdIsAllowed = false;

            // init GameObjects
            GameObject deviceManager = new GameObject("VR_DeviceManager");
            deviceManager.AddComponent<DeviceManager>();
            DeviceManager deviceManagerComponent = DeviceManager.Instance; // init the singleton
            DontDestroyOnLoad(deviceManager);

            // add an event triggered when game scene changes, to handle
            // shutting off the HMD outside of Flight scene
            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);

            // initialize the OpenVR API
            bool success = InitHMD();
            if (!success) {
                Utils.LogError("Unable to initialize VR headset!");
            }

            // when ready for a GUI, load it
            GameEvents.onGUIApplicationLauncherReady.Add(gui.OnAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(gui.OnAppLauncherDestroyed);

            // don't destroy this object when switching scenes
            DontDestroyOnLoad(this);
        }

        /// <summary>
        /// Overrides the OnGUI method to render the application launcher GUI.
        /// </summary>
        void OnGUI() {
            gui.OnGUI();
        }

        /// <summary>
        /// Overrides the OnDestroy method, called when plugin is destroyed.
        /// </summary>
        void OnDestroy() {
            Utils.Log(Globals.KERBALVR_NAME + " OnDestroy");
            CloseHMD();
        }

        /// <summary>
        /// Overrides the LateUpdate method, called every frame after all objects' Update.
        /// </summary>
        void LateUpdate() {
            // dispatch any OpenVR events
            if (hmdIsInitialized) {
                DispatchOpenVREvents();
            }

            // check if the current scene allows VR
            HmdIsAllowed = Scene.SceneAllowsVR();

            // check if we are running the HMD
            HmdIsRunning = HmdIsAllowed && hmdIsInitialized && HmdIsEnabled;

            // perform regular updates if HMD is initialized
            if (HmdIsRunning) {
                EVRCompositorError vrCompositorError = EVRCompositorError.None;

                try {
                    // TODO: investigate if we should really be capturing poses in LateUpdate
                    
                    // get latest device poses
                    float secondsToPhotons = Utils.CalculatePredictedSecondsToPhotons();
                    OpenVR.System.GetDeviceToAbsoluteTrackingPose(Scene.TrackingSpace, secondsToPhotons, devicePoses);
                    SteamVR_Events.NewPoses.Send(devicePoses);

                    HmdMatrix34_t vrLeftEyeTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Left);
                    HmdMatrix34_t vrRightEyeTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Right);
                    vrCompositorError = OpenVR.Compositor.WaitGetPoses(renderPoses, gamePoses);

                    if (vrCompositorError != EVRCompositorError.None) {
                        throw new Exception("WaitGetPoses failed: (" + (int)vrCompositorError + ") " + vrCompositorError.ToString());
                    }

                    // convert SteamVR poses to Unity coordinates
                    var hmdTransform = new SteamVR_Utils.RigidTransform(devicePoses[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking);
                    SteamVR_Utils.RigidTransform[] hmdEyeTransform = new SteamVR_Utils.RigidTransform[2];
                    hmdEyeTransform[0] = new SteamVR_Utils.RigidTransform(vrLeftEyeTransform);
                    hmdEyeTransform[1] = new SteamVR_Utils.RigidTransform(vrRightEyeTransform);

                    // render each eye
                    for (int i = 0; i < 2; i++) {
                        RenderHmdCameras(
                            (EVREye)i,
                            hmdTransform,
                            hmdEyeTransform[i],
                            i == 0 ? leftEyeRenderTextures[qualityLevel]:rightEyeRenderTextures[qualityLevel],
                            renderTextures[qualityLevel]);
                    }

                    OpenVR.Compositor.PostPresentHandoff();

                } catch (Exception e) {
                    Utils.LogError(e);
                    HmdIsEnabled = false;
                    HmdIsRunning = false;
                }

                // disable highlighting of parts due to mouse
                // TODO: there needs to be a better way to do this. this affects the Part permanently
                Part hoveredPart = Mouse.HoveredPart;
                if (hoveredPart != null) {
                    hoveredPart.HighlightActive = false;
                    hoveredPart.highlightColor.a = 0f;
                }
            }

            // reset cameras when HMD is turned off
            if (!HmdIsRunning && hmdIsRunningPrev) {
                Utils.Log("HMD is now off, resetting cameras...");
                Scene.CloseScene();
            }


#if DEBUG
            if (Input.GetKeyDown(KeyCode.Y)) {
                Utils.PrintAllCameras();
                Utils.PrintAllLayers();
                Utils.PrintDebug();
            }
#endif

            hmdIsRunningPrev = HmdIsRunning;
        }

        private void DispatchOpenVREvents() {
            // copied from SteamVR_Render
            var vrEvent = new VREvent_t();
            var size = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VREvent_t));
            for (int i = 0; i < 64; i++) {
                if (!OpenVR.System.PollNextEvent(ref vrEvent, size))
                    break;

                switch ((EVREventType)vrEvent.eventType) {
                    case EVREventType.VREvent_InputFocusCaptured: // another app has taken focus (likely dashboard)
                        if (vrEvent.data.process.oldPid == 0) {
                            SteamVR_Events.InputFocus.Send(false);
                        }
                        break;
                    case EVREventType.VREvent_InputFocusReleased: // that app has released input focus
                        if (vrEvent.data.process.pid == 0) {
                            SteamVR_Events.InputFocus.Send(true);
                        }
                        break;
                    case EVREventType.VREvent_ShowRenderModels:
                        SteamVR_Events.HideRenderModels.Send(false);
                        break;
                    case EVREventType.VREvent_HideRenderModels:
                        SteamVR_Events.HideRenderModels.Send(true);
                        break;
                    default:
                        SteamVR_Events.System((EVREventType)vrEvent.eventType).Send(vrEvent);
                        break;
                }
            }
        }

        private bool failed;

        private void RenderHmdCameras(
            EVREye eye,
            SteamVR_Utils.RigidTransform hmdTransform,
            SteamVR_Utils.RigidTransform hmdEyeTransform,
            Texture2D hmdEyeRenderTexture,
            RenderTexture renderTexture) {
            if (failed) return;
            /**
             * hmdEyeTransform is in a coordinate system that follows the headset, where
             * the origin is the headset device position. Therefore the eyes are at a constant
             * offset from the device. hmdEyeTransform does not change (per eye).
             *      hmdEyeTransform.x+  towards the right of the headset
             *      hmdEyeTransform.y+  towards the top the headset
             *      hmdEyeTransform.z+  towards the front of the headset
             *
             * hmdTransform is in a coordinate system set in physical space, where the
             * origin is the initial seated position.
             *      hmdTransform.x+     towards the right
             *      hmdTransform.y+     upwards
             *      hmdTransform.z+     towards the front
             *
             *  Scene.InitialPosition and Scene.InitialRotation are the Unity world coordinates where
             *  we initialize the VR scene, i.e. the origin of a coordinate system that maps
             *  1-to-1 with physical space.
             *
             *  1. Calculate the position of the eye in the physical coordinate system.
             *  2. Transform the calculated position into Unity world coordinates, offset from
             *     InitialPosition and InitialRotation.
             */

            // position of the eye in the VR reference frame
           // Vector3 positionToEye = hmdTransform.pos + hmdTransform.rot * hmdEyeTransform.pos;

            // update position of the cameras
            Scene.UpdateScene(hmdTransform, hmdEyeTransform);

            // render the set of cameras
            for (int i = 0; i < Scene.NumVRCameras; i++) {
                Types.CameraData camData = Scene.VRCameras[i];

                // set projection matrix
                camData.camera.projectionMatrix = (eye == EVREye.Eye_Left) ?
                    camData.hmdProjectionMatrixL : camData.hmdProjectionMatrixR;

                // set texture to render to, then render
                camData.camera.targetTexture = renderTexture;
                camData.camera.Render();
            }

            Graphics.CopyTexture(renderTexture, hmdEyeRenderTexture);

            openvrTexture.handle = hmdEyeRenderTexture.GetNativeTexturePtr(); //this syncs with the render thread.

            // Submit frames to HMD
            EVRCompositorError vrCompositorError = OpenVR.Compositor.Submit(eye, ref openvrTexture, ref hmdTextureBounds, EVRSubmitFlags.Submit_Default);
            if (vrCompositorError != EVRCompositorError.None) {
                Debug.Log("Submit (" + eye + ") failed: (" + (int)vrCompositorError + ") " + vrCompositorError.ToString());
                failed = true;
            }
        }

        /// <summary>
        /// An event called when the game is switching scenes. The VR headset should be disabled.
        /// </summary>
        /// <param name="scene">The scene being switched into.</param>
        public void OnGameSceneLoadRequested(GameScenes scene) {
            HmdIsEnabled = false;
        }

        /// <summary>
        /// Initialize HMD using OpenVR API calls.
        /// </summary>
        /// <returns>True on success, false otherwise. Errors logged.</returns>
        private bool InitHMD() {
            bool retVal = false;

            // return if HMD has already been initialized
            if (hmdIsInitialized) {
                return true;
            }

            // set the location of the OpenVR DLL
            SetDllDirectory(Globals.OpenVRDllPath);

            // check if HMD is connected on the system
            retVal = OpenVR.IsHmdPresent();
            if (!retVal) {
                Utils.LogError("HMD not found on this system.");
                return retVal;
            }

            // check if SteamVR runtime is installed
            retVal = OpenVR.IsRuntimeInstalled();
            if (!retVal) {
                Utils.LogError("SteamVR runtime not found on this system.");
                return retVal;
            }

            // initialize HMD
            EVRInitError hmdInitErrorCode = EVRInitError.None;
            OpenVR.Init(ref hmdInitErrorCode, EVRApplicationType.VRApplication_Scene);
            retVal = (hmdInitErrorCode == EVRInitError.None);
            if (!retVal) {
                Utils.LogError("Failed to initialize HMD. Init returned: " + OpenVR.GetStringForHmdError(hmdInitErrorCode));
                return retVal;
            }

            // reset "seated position" and capture initial position. this means you should hold the HMD in
            // the position you would like to consider "seated", before running this code.
            ResetInitialHmdPosition();

            // get HMD render target size
            uint renderTextureWidth = 0;
            uint renderTextureHeight = 0;
            OpenVR.System.GetRecommendedRenderTargetSize(ref renderTextureWidth, ref renderTextureHeight);

            // at the moment, only Direct3D12 is working with Kerbal Space Program
            ETextureType textureType = ETextureType.DirectX;
            switch (SystemInfo.graphicsDeviceType) {
                case UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore:
                case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2:
                case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3:
                    textureType = ETextureType.OpenGL;
                    break; // doesn't work
                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D9:
                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D11:
                    textureType = ETextureType.DirectX;
                    break; // doesn't work
                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D12:
                    textureType = ETextureType.DirectX; // do not use DirectX12
                    break;
                default:
                    throw (new Exception(SystemInfo.graphicsDeviceType.ToString() + " not supported"));
            }

            openvrTexture = new Texture_t();
            openvrTexture.eType = textureType;
            openvrTexture.eColorSpace = EColorSpace.Auto;

            Debug.Log("Graphics Type: " + SystemInfo.graphicsDeviceType);

            // initialize render textures (for displaying on HMD)

            rightEyeRenderTextures[0] = new Texture2D((int)(renderTextureWidth * 1.5), (int)(renderTextureHeight * 1.5), TextureFormat.ARGB32, false);
            leftEyeRenderTextures[0] = new Texture2D((int)(renderTextureWidth * 1.5), (int)(renderTextureHeight * 1.5), TextureFormat.ARGB32, false);
            renderTextures[0] = new RenderTexture((int)(renderTextureWidth * 1.5), (int)(renderTextureHeight * 1.5), 24, RenderTextureFormat.ARGB32);

            rightEyeRenderTextures[1] = new Texture2D((int)(renderTextureWidth), (int)(renderTextureHeight), TextureFormat.ARGB32, false);
            leftEyeRenderTextures[1] = new Texture2D((int)(renderTextureWidth), (int)(renderTextureHeight), TextureFormat.ARGB32, false);
            renderTextures[1] = new RenderTexture((int)(renderTextureWidth), (int)(renderTextureHeight), 24, RenderTextureFormat.ARGB32);

            rightEyeRenderTextures[2] = new Texture2D((int)(renderTextureWidth * .75), (int)(renderTextureHeight * .75), TextureFormat.ARGB32, false);
            leftEyeRenderTextures[2] = new Texture2D((int)(renderTextureWidth * .75), (int)(renderTextureHeight * .75), TextureFormat.ARGB32, false);
            renderTextures[2] = new RenderTexture((int)(renderTextureWidth * .75), (int)(renderTextureHeight * .75), 24, RenderTextureFormat.ARGB32);

            rightEyeRenderTextures[3] = new Texture2D((int)(renderTextureWidth * .5), (int)(renderTextureHeight * .5), TextureFormat.ARGB32, false);
            leftEyeRenderTextures[3] = new Texture2D((int)(renderTextureWidth * .5), (int)(renderTextureHeight * .5), TextureFormat.ARGB32, false);
            renderTextures[3] = new RenderTexture((int)(renderTextureWidth * .5), (int)(renderTextureHeight * .5), 24, RenderTextureFormat.ARGB32);

            rightEyeRenderTextures[4] = new Texture2D((int)(renderTextureWidth * .25), (int)(renderTextureHeight * .25), TextureFormat.ARGB32, false);
            leftEyeRenderTextures[4] = new Texture2D((int)(renderTextureWidth * .25), (int)(renderTextureHeight * .25), TextureFormat.ARGB32, false);
            renderTextures[4] = new RenderTexture((int)(renderTextureWidth * .25), (int)(renderTextureHeight * .25), 24, RenderTextureFormat.ARGB32);

            // set rendering bounds on texture to render
            hmdTextureBounds.uMin = 0.0f;
            hmdTextureBounds.uMax = 1.0f;
            hmdTextureBounds.vMin = 1.0f; // flip the vertical coordinate for some reason
            hmdTextureBounds.vMax = 0.0f;


            hmdIsInitialized = true;

            return retVal;
        }

        /// <summary>
        /// Sets the current real-world position of the HMD as the seated origin in IVA.
        /// </summary>
        public static void ResetInitialHmdPosition() {
            if (hmdIsInitialized) {
                OpenVR.System.ResetSeatedZeroPose();
            }
        }

        /// <summary>
        /// Check if we can reset the seated pose (only when in Seated Mode)
        /// </summary>
        /// <returns>True if seated pose can be reset.</returns>
        public static bool CanResetSeatedPose() {
            return HmdIsRunning && (Scene.TrackingSpace == ETrackingUniverseOrigin.TrackingUniverseSeated);
        }

        /// <summary>
        /// Shuts down the OpenVR API.
        /// </summary>
        private void CloseHMD() {
            HmdIsEnabled = false;
            OpenVR.Shutdown();
            hmdIsInitialized = false;
        }

        public static bool IsDeviceConnected(uint deviceIndex) {
            if (deviceIndex >= OpenVR.k_unMaxTrackedDeviceCount) {
                throw new ArgumentOutOfRangeException(
                    "deviceIndex",
                    deviceIndex,
                    "deviceIndex must be less than " + OpenVR.k_unMaxTrackedDeviceCount);
            }
            return devicePoses[deviceIndex].bDeviceIsConnected;
        }

    } // class KerbalVR
} // namespace KerbalVR
