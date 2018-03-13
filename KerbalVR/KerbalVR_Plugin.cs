using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    // Start plugin on entering the Flight scene
    //
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class KerbalVR_Plugin : MonoBehaviour
    {
        // this function allows importing DLLs from a given path
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);


        #region Properties

        // enable and disable VR functionality
        private bool _hmdIsEnabled;
        public bool HmdIsEnabled {
            get { return _hmdIsEnabled; }
            set {
                _hmdIsEnabled = value;

                if (_hmdIsEnabled) {
                    InitVRScene();
                    ResetInitialHmdPosition();
                }
            }
        }

        // check if VR can be enabled
        public bool HmdIsAllowed { get; private set; }

        #endregion


        #region Private Members

        // hold a reference to the app launcher GUI
        private KerbalVR_GUI gui;

        // keep track of when the HMD is rendering images
        private bool hmdIsInitialized = false;
        private bool hmdIsRunning = false;
        private bool hmdIsRunningPrev = false;

        // defines the bounds to texture bounds for rendering
        private VRTextureBounds_t hmdTextureBounds;

        // these arrays each hold one object for the corresponding eye, where
        // index 0 = Left_Eye, index 1 = Right_Eye
        private Texture_t[] hmdEyeTexture = new Texture_t[2];
        private RenderTexture[] hmdEyeRenderTexture = new RenderTexture[2];

        // store the initial position when VR is started
        private Vector3 ivaInitialPosition;
        private Quaternion ivaInitialRotation;

        // an array containing the cameras to render to the HMD
        private int numCamerasToRender;
        private KerbalVR_Types.CameraData[] camerasToRender;

        // store the tracked device poses
        private TrackedDevicePose_t[] vrDevicePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private TrackedDevicePose_t[] vrRenderPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private TrackedDevicePose_t[] vrGamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

        #endregion


        #region Debug
        private uint controlIndexL = 0;
        private uint controlIndexR = 0;
        #endregion


        /// <summary>
        /// Overrides the Awake method for a MonoBehaviour plugin.
        /// Initialize class members.
        /// </summary>
        void Awake() {
            Utils.LogInfo("KerbalVR plugin starting...");

            // init objects
            gui = new KerbalVR_GUI(this);
            _hmdIsEnabled = false;
            HmdIsAllowed = false;

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
            Utils.LogInfo("KerbalVrPlugin OnDestroy");
            CloseHMD();
        }

        /// <summary>
        /// Overrides the LateUpdate method, called every frame after all objects' Update.
        /// </summary>
        void LateUpdate() {
            // do nothing unless we are in IVA
            HmdIsAllowed = HighLogic.LoadedSceneIsFlight &&
                (CameraManager.Instance != null) &&
                (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA);

            // check if we are running the HMD
            hmdIsRunning = HmdIsAllowed && hmdIsInitialized && HmdIsEnabled;

            // perform regular updates if HMD is initialized
            if (hmdIsRunning) {
                EVRCompositorError vrCompositorError = EVRCompositorError.None;

                try {
                    // TODO: investigate if we should really be capturing poses in LateUpdate

                    // detect controllers
                    for (uint idx = 0; idx < OpenVR.k_unMaxTrackedDeviceCount; idx++) {
                        if ((controlIndexL == 0) && (OpenVR.System.GetTrackedDeviceClass(idx) == ETrackedDeviceClass.Controller)) {
                            controlIndexL = idx;
                        } else if ((controlIndexR == 0) && (OpenVR.System.GetTrackedDeviceClass(idx) == ETrackedDeviceClass.Controller)) {
                            controlIndexR = idx;
                        }
                    }

                    // get latest HMD pose
                    float secondsToPhotons = Utils.CalculatePredictedSecondsToPhotons();
                    OpenVR.System.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseSeated, secondsToPhotons, vrDevicePoses);
                    HmdMatrix34_t vrLeftEyeTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Left);
                    HmdMatrix34_t vrRightEyeTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Right);
                    vrCompositorError = OpenVR.Compositor.WaitGetPoses(vrRenderPoses, vrGamePoses);

                    if (vrCompositorError != EVRCompositorError.None) {
                        throw new Exception("WaitGetPoses failed: (" + (int)vrCompositorError + ") " + vrCompositorError.ToString());
                    }

                    // convert SteamVR poses to Unity coordinates
                    var hmdTransform = new SteamVR_Utils.RigidTransform(vrDevicePoses[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking);
                    SteamVR_Utils.RigidTransform[] hmdEyeTransform = new SteamVR_Utils.RigidTransform[2];
                    hmdEyeTransform[0] = new SteamVR_Utils.RigidTransform(vrLeftEyeTransform);
                    hmdEyeTransform[1] = new SteamVR_Utils.RigidTransform(vrRightEyeTransform);

                    if (controlIndexL > 0) {
                        SteamVR_Utils.RigidTransform ctrlPoseLeft = new SteamVR_Utils.RigidTransform(vrDevicePoses[controlIndexL].mDeviceToAbsoluteTracking);
                    }

                    // render each eye
                    for (int i = 0; i < 2; i++) {
                        RenderHmdCameras(
                            (EVREye)i,
                            hmdTransform,
                            hmdEyeTransform[i],
                            hmdEyeRenderTexture[i],
                            hmdEyeTexture[i]);
                    }
                    OpenVR.Compositor.PostPresentHandoff();

                } catch (Exception e) {
                    Utils.LogError(e);
                    HmdIsEnabled = false;
                    hmdIsRunning = false;
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
            if (!hmdIsRunning && hmdIsRunningPrev) {
                Utils.LogInfo("HMD is now off, resetting cameras...");
                foreach (KerbalVR_Types.CameraData camData in camerasToRender) {
                    camData.camera.targetTexture = null;
                    camData.camera.projectionMatrix = camData.originalProjectionMatrix;
                    camData.camera.enabled = true;
                }
            }
            
            hmdIsRunningPrev = hmdIsRunning;
        }

        private void RenderHmdCameras(
            EVREye eye,
            SteamVR_Utils.RigidTransform hmdTransform,
            SteamVR_Utils.RigidTransform hmdEyeTransform,
            RenderTexture hmdEyeRenderTexture,
            Texture_t hmdEyeTexture) {

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
             *  ivaInitialPosition and ivaInitialRotation are the Unity world coordinates where
             *  we initialize the VR scene, i.e. the origin of a coordinate system that maps
             *  1-to-1 with physical space.
             *
             *  1. Calculate the position of the eye in the physical coordinate system.
             *  2. Transform the calculated position into Unity world coordinates, offset from
             *     ivaInitialPosition and ivaInitialRotation.
             */

            // position of the eye in the VR reference frame
            Vector3 positionToEye = hmdTransform.pos + hmdTransform.rot * hmdEyeTransform.pos;

            // position of the eye in the IVA reference frame
            InternalCamera.Instance.transform.position = ivaInitialPosition + ivaInitialRotation * positionToEye;
            InternalCamera.Instance.transform.rotation = ivaInitialRotation * hmdTransform.rot;

            // move the FlightCamera to match the position of the InternalCamera (so the outside world moves accordingly)
            FlightCamera.fetch.transform.position = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
            FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);

            // render the set of cameras
            for (int i = 0; i < numCamerasToRender; i++) {
                KerbalVR_Types.CameraData camData = camerasToRender[i];

                // set projection matrix
                camData.camera.projectionMatrix = (eye == EVREye.Eye_Left) ?
                    camData.hmdProjectionMatrixL : camData.hmdProjectionMatrixR;

                // set texture to render to, then render
                camData.camera.targetTexture = hmdEyeRenderTexture;
                camData.camera.Render();
            }

            // Submit frames to HMD
            EVRCompositorError vrCompositorError = OpenVR.Compositor.Submit(eye, ref hmdEyeTexture, ref hmdTextureBounds, EVRSubmitFlags.Submit_Default);
            if (vrCompositorError != EVRCompositorError.None) {
                throw new Exception("Submit (" + eye + ") failed: (" + (int)vrCompositorError + ") " + vrCompositorError.ToString());
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
                    throw (new Exception("DirectX 9 not supported"));
                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D11:
                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D12:
                    textureType = ETextureType.DirectX;
                    break;
                default:
                    throw (new Exception(SystemInfo.graphicsDeviceType.ToString() + " not supported"));
            }

            // initialize render textures (for displaying on HMD)
            for (int i = 0; i < 2; i++) {
                hmdEyeRenderTexture[i] = new RenderTexture((int)renderTextureWidth, (int)renderTextureHeight, 24, RenderTextureFormat.ARGB32);
                hmdEyeRenderTexture[i].Create();
                hmdEyeTexture[i].handle = hmdEyeRenderTexture[i].GetNativeTexturePtr();
                hmdEyeTexture[i].eColorSpace = EColorSpace.Auto;
                hmdEyeTexture[i].eType = textureType;
            }

            // set rendering bounds on texture to render
            hmdTextureBounds.uMin = 0.0f;
            hmdTextureBounds.uMax = 1.0f;
            hmdTextureBounds.vMin = 1.0f; // flip the vertical coordinate for some reason
            hmdTextureBounds.vMax = 0.0f;
            
            hmdIsInitialized = true;

            return retVal;
        }

        private void InitVRScene() {
            /*foreach (Camera camera in Camera.allCameras) {
                Utils.LogInfo("KSP Camera: " + camera.name);
            }*/

            // search for the cameras to render
            numCamerasToRender = Globals.FLIGHT_SCENE_CAMERAS.Length;
            camerasToRender = new KerbalVR_Types.CameraData[numCamerasToRender];
            for (int i = 0; i < numCamerasToRender; i++) {

                Camera foundCamera = Array.Find(Camera.allCameras, cam => cam.name.Equals(Globals.FLIGHT_SCENE_CAMERAS[i]));
                if (foundCamera == null) {
                    Utils.LogError("Could not find camera \"" + Globals.FLIGHT_SCENE_CAMERAS[i] + "\" in the scene!");

                } else {
                    // determine clip plane and new projection matrices
                    float nearClipPlane = (foundCamera.name.Equals("Camera 01")) ? 0.05f : foundCamera.nearClipPlane;
                    HmdMatrix44_t projectionMatrixL = OpenVR.System.GetProjectionMatrix(EVREye.Eye_Left, nearClipPlane, foundCamera.farClipPlane);
                    HmdMatrix44_t projectionMatrixR = OpenVR.System.GetProjectionMatrix(EVREye.Eye_Right, nearClipPlane, foundCamera.farClipPlane);

                    // store information about the camera
                    camerasToRender[i].camera = foundCamera;
                    camerasToRender[i].originalProjectionMatrix = foundCamera.projectionMatrix;
                    camerasToRender[i].hmdProjectionMatrixL = MathUtils.Matrix4x4_OpenVr2UnityFormat(ref projectionMatrixL);
                    camerasToRender[i].hmdProjectionMatrixR = MathUtils.Matrix4x4_OpenVr2UnityFormat(ref projectionMatrixR);

                    // disable the camera so we can call Render directly
                    foundCamera.enabled = false;
                }
                
            }

            // capture the initial viewpoint position
            ivaInitialPosition = InternalCamera.Instance.transform.position;
            ivaInitialRotation = InternalCamera.Instance.transform.rotation;
            
        }

        /// <summary>
        /// Sets the current real-world position of the HMD as the seated origin in IVA.
        /// </summary>
        public void ResetInitialHmdPosition() {
            if (hmdIsInitialized) {
                OpenVR.System.ResetSeatedZeroPose();
            }
        }

        /// <summary>
        /// Shuts down the OpenVR API.
        /// </summary>
        private void CloseHMD() {
            HmdIsEnabled = false;
            OpenVR.Shutdown();
            hmdIsInitialized = false;
        }

    } // class KerbalVR_Plugin
} // namespace KerbalVR
