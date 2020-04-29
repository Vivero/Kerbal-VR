using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;

namespace KerbalVR {
    /// <summary>
    /// The entry point for the KerbalVR plugin. This mod should
    /// start up once, when the game starts.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class Core : MonoBehaviour {
        // this function allows importing DLLs from a given path
        [DllImport("kernel32.dll", SetLastError = true)]
        protected static extern bool SetDllDirectory(string lpPathName);

        // import KerbalVR_Renderer plugin functions
        [DllImport("KerbalVR_Renderer")]
        private static extern IntPtr GetRenderEventFunc();

        [DllImport("KerbalVR_Renderer")]
        private static extern void SetTextureFromUnity(
            int textureIndex,
            System.IntPtr textureHandle,
            float boundsUMin,
            float boundsUMax,
            float boundsVMin,
            float boundsVMax);


        #region Types
        public enum OpenVrState {
            Uninitialized,
            Initializing,
            Initialized,
            InitFailed,
        }
        #endregion


        #region Properties
        /// <summary>
        /// Returns true if VR is currently enabled. Enabled
        /// does not necessarily mean VR is currently running, only
        /// that the user allowing VR to be activated.
        /// Set to true to enable VR; false to disable VR.
        /// </summary>
        public static bool VrIsEnabled { get; set; } = false;

        /// <summary>
        /// Returns true if VR is allowed to run in the current scene.
        /// </summary>
        public static bool VrIsAllowed { get; private set; } = true;

        /// <summary>
        /// Returns true if VR is currently running, i.e. tracking devices
        /// and rendering images to the headset.
        /// </summary>
        public static bool VrIsRunning { get; private set; } = false;

        // these arrays each hold one object for the corresponding eye, where
        // index 0 = Left_Eye, index 1 = Right_Eye
        public static RenderTexture[] HmdEyeRenderTexture { get; private set; } = new RenderTexture[2];
        #endregion


        #region Private Members
        // keep track of when the HMD is rendering images
        protected static OpenVrState openVrState = OpenVrState.Uninitialized;
        protected static bool vrIsRunningPrev = false;
        protected static DateTime openVrInitLastAttempt;

        // store the tracked device poses
        public static TrackedDevicePose_t[] devicePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        public static TrackedDevicePose_t[] renderPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        public static TrackedDevicePose_t[] gamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        #endregion


        /// <summary>
        /// Initialize the KerbalVR-related GameObjects, singleton classes, and initialize OpenVR.
        /// </summary>
        protected void Awake() {
            // set the location of the native plugin DLLs
            SetDllDirectory(Globals.EXTERNAL_DLL_PATH);

            // initialize KerbalVR GameObjects
            GameObject kvrConfiguration = new GameObject("KVR_Configuration");
            kvrConfiguration.AddComponent<KerbalVR.Configuration>();
            Configuration kvrConfigurationComponent = Configuration.Instance; // init the singleton
            DontDestroyOnLoad(kvrConfiguration);

            GameObject kvrAssetLoader = new GameObject("KVR_AssetLoader");
            kvrAssetLoader.AddComponent<KerbalVR.AssetLoader>();
            AssetLoader kvrAssetLoaderComponent = AssetLoader.Instance; // init the singleton
            DontDestroyOnLoad(kvrAssetLoader);

            /*
            GameObject kvrScene = new GameObject("KVR_Scene");
            kvrScene.AddComponent<KerbalVR.Scene>();
            Scene kvrSceneComponent = Scene.Instance; // init the singleton
            DontDestroyOnLoad(kvrScene);
            */

            // initialize OpenVR immediately if allowed in config
            if (KerbalVR.Configuration.Instance.InitOpenVrAtStartup) {
                TryInitializeOpenVr();
            }

            // add an event triggered when game scene changes
            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);

            // don't destroy this object when switching scenes
            DontDestroyOnLoad(this);

            // ensure various settings to minimize latency
            Application.targetFrameRate = -1;
            Application.runInBackground = true; // don't require companion window focus
            QualitySettings.maxQueuedFrames = -1;
            QualitySettings.vSyncCount = 0; // this applies to the companion window
        }

        /// <summary>
        /// Start is called before the first frame update, to queue up the plugin renderer coroutine.
        /// </summary>
        IEnumerator Start() {
            yield return StartCoroutine("CallPluginAtEndOfFrames");
        }

        /// <summary>
        /// The plugin renderer coroutine which must run after all Cameras have rendered,
        /// then issues a callback to the native renderer, so that the native code runs on
        /// Unity's renderer thread.
        /// </summary>
        IEnumerator CallPluginAtEndOfFrames() {
            while (true) {
                // wait until all frame rendering is done
                yield return new WaitForEndOfFrame();

                // if VR is active, issue the render callback
                if (VrIsRunning) {
                    // the "0" currently does nothing on the native plugin code,
                    // so this can actually just be any int.
                    GL.IssuePluginEvent(GetRenderEventFunc(), 0);
                }
            }
        }

        /// <summary>
        /// Overrides the OnDestroy method, called when plugin is destroyed.
        /// </summary>
        protected void OnDestroy() {
            Utils.Log(KerbalVR.Globals.KERBALVR_NAME + " is shutting down...");
            CloseVr();
        }

        /// <summary>
        /// On Update, dispatch OpenVR events, retrieve tracked device poses.
        /// </summary>
        protected void Update() {
            // debug hooks
            if (Input.GetKeyDown(KeyCode.Y)) {
                Utils.Log("Debug");
            }

            // dispatch any OpenVR events
            DispatchOpenVrEvents();

            // process the state of OpenVR
            ProcessOpenVrState();

            // check if we are running the HMD
            VrIsRunning = (openVrState == OpenVrState.Initialized) && VrIsEnabled;

            if (VrIsRunning) {
                // we've just started VR
                if (!vrIsRunningPrev) {
                    Utils.Log("VR is now turned on");
                    ResetInitialHmdPosition();
                }

                // get latest device poses, emit an event to indicate devices have been updated
                // float secondsToPhotons = Utils.CalculatePredictedSecondsToPhotons();
                // OpenVR.System.GetDeviceToAbsoluteTrackingPose(Scene.Instance.TrackingSpace, secondsToPhotons, devicePoses);
                // SteamVR_Events.NewPoses.Send(devicePoses);

                // HmdMatrix34_t vrLeftEyeTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Left);
                // HmdMatrix34_t vrRightEyeTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Right);

                // convert SteamVR poses to Unity coordinates
                /*var hmdTransform = new SteamVR_Utils.RigidTransform(devicePoses[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking);
                SteamVR_Utils.RigidTransform[] hmdEyeTransform = new SteamVR_Utils.RigidTransform[2];
                hmdEyeTransform[0] = new SteamVR_Utils.RigidTransform(vrLeftEyeTransform);
                hmdEyeTransform[1] = new SteamVR_Utils.RigidTransform(vrRightEyeTransform);*/

                /**
                 * hmdEyeTransform is in a coordinate system that follows the headset, where
                 * the origin is the headset device position. Therefore the eyes are at a constant
                 * offset from the device. hmdEyeTransform does not change (per eye).
                 *      hmdEyeTransform.x+  towards the right of the headset
                 *      hmdEyeTransform.y+  towards the top the headset
                 *      hmdEyeTransform.z+  towards the front of the headset
                 *
                 * hmdTransform is in a coordinate system set in physical space, where the
                 * origin is the initial seated position. Or for room-scale, the physical origin of the room.
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
            }

            // VR has been deactivated
            if (!VrIsRunning && vrIsRunningPrev) {
                Utils.Log("VR is now turned off");
            }

            // emit an update if the running status changed
            if (VrIsRunning != vrIsRunningPrev) {
                KerbalVR.Events.HmdStatusUpdated.Send(VrIsRunning);
            }
            vrIsRunningPrev = VrIsRunning;
        }

        /// <summary>
        /// Dispatch other miscellaneous OpenVR-specific events.
        /// </summary>
        protected void DispatchOpenVrEvents() {
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

        protected static void ProcessOpenVrState() {
            switch (openVrState) {
                case OpenVrState.Uninitialized:
                    if (VrIsEnabled) {
                        openVrState = OpenVrState.Initializing;
                    }
                    break;

                case OpenVrState.Initializing:
                    TryInitializeOpenVr();
                    break;

                case OpenVrState.InitFailed:
                    if (DateTime.Now.Subtract(openVrInitLastAttempt).TotalSeconds > 10) {
                        openVrState = OpenVrState.Uninitialized;
                    }
                    break;
            }
        }

        protected static void TryInitializeOpenVr() {
            openVrInitLastAttempt = DateTime.Now;
            try {
                InitializeOpenVr();
                openVrState = OpenVrState.Initialized;
                Utils.Log("Initialized OpenVR");

            } catch (Exception e) {
                openVrState = OpenVrState.InitFailed;
                Utils.LogError("InitializeOpenVr failed with error: " + e);
                VrIsEnabled = false;
            }
        }

        /// <summary>
        /// An event called when the game is switching scenes.
        /// </summary>
        /// <param name="scene">The scene being switched into</param>
        protected void OnGameSceneLoadRequested(GameScenes scene) {
        }

        /// <summary>
        /// Initialize VR using OpenVR API calls. Throws an exception on error.
        /// </summary>
        protected static void InitializeOpenVr() {
            // return if OpenVR has already been initialized
            if (openVrState == OpenVrState.Initialized) {
                return;
            }

            // check if HMD is connected on the system
            if (!OpenVR.IsHmdPresent()) {
                throw new InvalidOperationException("HMD not found on this system");
            }

            // check if SteamVR runtime is installed
            if (!OpenVR.IsRuntimeInstalled()) {
                throw new InvalidOperationException("SteamVR runtime not found on this system");
            }

            // initialize OpenVR
            EVRInitError initErrorCode = EVRInitError.None;
            OpenVR.Init(ref initErrorCode, EVRApplicationType.VRApplication_Scene);
            if (initErrorCode != EVRInitError.None) {
                throw new Exception("OpenVR error: " + OpenVR.GetStringForHmdError(initErrorCode));
            }

            // reset "seated position" and capture initial position. this means you should hold the HMD in
            // the position you would like to consider "seated", before running this code.
            ResetInitialHmdPosition();

            // get HMD render target size
            uint renderTextureWidth = 0;
            uint renderTextureHeight = 0;
            OpenVR.System.GetRecommendedRenderTargetSize(ref renderTextureWidth, ref renderTextureHeight);
            Utils.Log("HMD texture size per eye: " + renderTextureWidth + " x " + renderTextureHeight);

            // check the graphics device
            switch (SystemInfo.graphicsDeviceType) {
                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D11:
                    // at the moment, only Direct3D11 is working with Kerbal Space Program
                    break;
                default:
                    throw new InvalidOperationException(SystemInfo.graphicsDeviceType.ToString() + " is not supported");
            }

            // initialize render textures (for displaying on HMD)
            for (int i = 0; i < 2; i++) {
                HmdEyeRenderTexture[i] = new RenderTexture((int)renderTextureWidth, (int)renderTextureHeight, 24, RenderTextureFormat.ARGB32);
                HmdEyeRenderTexture[i].Create();

                /**
                 * texture rendering bounds:
                 * uMin = 0f
                 * uMax = 1f
                 * vMin = 1f   flip the vertical coord for some reason (I think it's a D3D11 thing)
                 * vMax = 0f
                 */

                // send the textures to the native renderer plugin
                SetTextureFromUnity(i, HmdEyeRenderTexture[i].GetNativeTexturePtr(), 0f, 1f, 1f, 0f);
            }
        }

        /// <summary>
        /// Sets the current real-world position of the HMD as the seated origin.
        /// </summary>
        public static void ResetInitialHmdPosition() {
            if (openVrState == OpenVrState.Initialized) {
                OpenVR.System.ResetSeatedZeroPose();
            }
        }

        /// <summary>
        /// Shuts down the OpenVR API.
        /// </summary>
        protected void CloseVr() {
            VrIsEnabled = false;
            OpenVR.Shutdown();
            openVrState = OpenVrState.Uninitialized;
        }

    } // class Core
} // namespace KerbalVR
