using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    /// <summary>
    /// Scene is a singleton class that encapsulates the code that positions
    /// the game cameras correctly for rendering them to the VR headset,
    /// according to the current KSP scene (flight, editor, etc).
    ///
    /// The camera system is designed as follows:
    ///
    ///   At startup, create a "VR Camera Rig". It is a list of Camera pairs,
    ///   one Camera per eye, and one pair per KSP Camera. KSP Cameras are
    ///   those named in the KSP_CAMERA_NAMES_ALL array below. These are the
    ///   cameras that we are interested in rendering in VR.
    ///
    ///   Some of these KSP Cameras come in and out of existence during gameplay,
    ///   e.g. the InternalCamera is not created until we enter IVA in the game.
    ///   So, every frame, attempt to find the KSP Camera so we can copy its
    ///   parameters into the corresponding VR Camera pair.
    ///
    ///   Via this design, we do not have to manipulate the actual KSP
    ///   Cameras (FlightCamera, etc.), as we have done in previous designs
    ///   (KerbalVR version 3.x.x and prior). I found that manipulating the
    ///   KSP Cameras can introduce weird side effects, as many of these
    ///   cameras have other controllers attached which are also trying to
    ///   manipulate the KSP Cameras.
    ///
    ///   Create a Dictionary of CameraStates, keyed by KSP Camera name.
    ///   The CameraState simply indicates whether the KSP Camera should
    ///   be rendering right now or not. Maintiain this Dictionary of states
    ///   on every change of scenery (change in GameScenes, or changing
    ///   CameraMode during Flight).
    ///
    ///   On every frame (PreRender), do a check on every KSP Camera to see
    ///   whether it should be rendering or not, according to the CameraState.
    ///   Enable/disable the KSP Camera accordingly. We do this check every
    ///   frame, instead of when a scenery change happens, because some
    ///   internal KSP code may decide to asynchronously turn cameras on or
    ///   off. So, strive to maintain the state of KSP Cameras according to
    ///   our Dictionary of CameraStates.
    /// </summary>
    public class Scene : MonoBehaviour
    {
        #region Constants
        public static readonly string[] KSP_CAMERA_NAMES_MAINMENU = {
            "GalaxyCamera",
            "Landscape Camera",
        };
        public static readonly string[] KSP_CAMERA_NAMES_SPACECENTER = {
            "GalaxyCamera",
            "Camera ScaledSpace",
            "Camera 00",
        };
        public static readonly string[] KSP_CAMERA_NAMES_TRACKSTATION = {
            "GalaxyCamera",
            "Camera ScaledSpace",
        };
        public static readonly string[] KSP_CAMERA_NAMES_EDITOR = {
            "GalaxyCamera",
            "sceneryCam",
            "Main Camera",
        };
        public static readonly string[] KSP_CAMERA_NAMES_FLIGHT = {
            "GalaxyCamera",
            "Camera ScaledSpace",
            "Camera 00",
        };
        public static readonly string[] KSP_CAMERA_NAMES_FLIGHT_IVA = {
            "GalaxyCamera",
            "Camera ScaledSpace",
            "Camera 00",
            "InternalCamera",
        };
        public static readonly string[] KSP_CAMERA_NAMES_FLIGHT_MAP = {
            "GalaxyCamera",
            "Camera ScaledSpace",
        };

        // note: any camera in the above arrays should also be present here:
        public static readonly string[] KSP_CAMERA_NAMES_ALL = {
            "GalaxyCamera",
            "Landscape Camera",
            "Camera ScaledSpace",
            "Camera 00",
            "InternalCamera",
            "sceneryCam",
            "Main Camera",
        };
        #endregion

        #region Singleton
        /// <summary>
        /// This is a singleton class, and there must be exactly one GameObject with this Component in the scene.
        /// </summary>
        private static Scene _instance;
        public static Scene Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<Scene>();
                    if (_instance == null) {
                        Utils.LogError("The scene needs to have one active GameObject with a Scene script attached!");
                    } else {
                        _instance.Initialize();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// One-time initialization for this singleton class.
        /// </summary>
        private void Initialize() {
            foreach (string name in KSP_CAMERA_NAMES_ALL) {
                kspCamerasTargetState.Add(name, new Types.CameraState());
            }
        }
        #endregion


        #region Properties
        // The initial world position of the cameras for the current scene. This
        // position corresponds to the origin in the real world physical device
        // coordinate system.
        public Vector3 InitialPosition { get; private set; }
        public Quaternion InitialRotation { get; private set; }

        // The current world position of the cameras for the current scene. This
        // position corresponds to the origin in the real world physical device
        // coordinate system.
        public Vector3 CurrentPosition { get; set; }
        public Quaternion CurrentRotation { get; set; }

        // defines the tracking method to use
        public ETrackingUniverseOrigin TrackingSpace { get; private set; } = ETrackingUniverseOrigin.TrackingUniverseSeated;

        public List<Types.VRCameraSet> VRCameraSets { get; private set; } = new List<Types.VRCameraSet>();
        public bool IsVrCamerasEnabled { get; private set; } = false;
        public bool IsVrAllowed {
            get {
                return HighLogic.LoadedScene == GameScenes.MAINMENU ||
                    HighLogic.LoadedScene == GameScenes.SPACECENTER ||
                    HighLogic.LoadedScene == GameScenes.TRACKSTATION ||
                    HighLogic.LoadedScene == GameScenes.EDITOR ||
                    HighLogic.LoadedScene == GameScenes.FLIGHT;
            }
        }
        #endregion


        #region Private Members
        protected bool isVrCameraRigCreated = false;
        protected string[] currentCameraNames = null;
        protected GameScenes currentScene = GameScenes.LOADING;
        protected GameScenes previousScene = GameScenes.LOADING;

        protected Dictionary<string, Types.CameraState> kspCamerasTargetState = new Dictionary<string, Types.CameraState>();
        #endregion


        protected void Awake() { }

        protected void OnEnable() {
            // setup callback functions for events
            GameEvents.OnIVACameraKerbalChange.Add(OnIvaCameraChange);
            GameEvents.OnCameraChange.Add(OnCameraChange);
            KerbalVR.Events.HmdStatusUpdated.Listen(OnHmdStatusUpdated);
            Camera.onPreRender += OnCameraPreRender;
        }

        protected void OnDisable() {
            // remove callback functions
            GameEvents.OnIVACameraKerbalChange.Remove(OnIvaCameraChange);
            GameEvents.OnCameraChange.Remove(OnCameraChange);
            KerbalVR.Events.HmdStatusUpdated.Remove(OnHmdStatusUpdated);
            Camera.onPreRender -= OnCameraPreRender;
        }


        protected void Update() {
            currentScene = HighLogic.LoadedScene;
            if (currentScene != previousScene) {
#if DEBUG
                Utils.Log("Scene change " + previousScene + " -> " + currentScene);
#endif
                StartCoroutine("OnSceneChange");
            }
            previousScene = currentScene;
        }


        protected void LateUpdate() {
            if (KerbalVR.Core.IsOpenVrReady) {
                // check if we need to build VR cameras
                BuildVrCameraRig();

                // update cameras with pose data
                UpdateCameraPositions();
            }
        }

        protected IEnumerator OnSceneChange() {
            // okay I know this is super hacky, but, hear me out.
            // some things in the game aren't immediately ready as soon as the scene changes.
            // so, wait a few frames before executing the changes we need for VR.
            // yield return new WaitForSeconds(0.08f);
            yield return null;

            // a change in scene has triggered, reset the cameras
            SetVrCamerasEnabled(KerbalVR.Core.IsVrRunning);
            SetVrCameraPositions();
        }

        protected void BuildVrCameraRig() {
            // initially, create the set of all VR cameras
            if (!isVrCameraRigCreated) {
                for (int camIdx = 0; camIdx < KSP_CAMERA_NAMES_ALL.Length; ++camIdx) {
                    Types.VRCameraSet cameraSet = new Types.VRCameraSet();
                    string kspCameraName = KSP_CAMERA_NAMES_ALL[camIdx];

                    // create a camera for each eye
                    cameraSet.vrCameras = new Types.VREyeCamera[2];
                    for (int eyeIdx = 0; eyeIdx < 2; ++eyeIdx) {
                        EVREye eye = (EVREye)eyeIdx;
                        string kvrCameraName = "KVR_Eye_Camera (" + eye.ToString() + ") (" + kspCameraName + ")";
                        GameObject kvrCameraGameObject = new GameObject(kvrCameraName);
                        DontDestroyOnLoad(kvrCameraGameObject);
                        Camera kvrCameraComponent = kvrCameraGameObject.AddComponent<Camera>();
                        kvrCameraComponent.enabled = false;
                        kvrCameraComponent.targetTexture = KerbalVR.Core.HmdEyeRenderTexture[eyeIdx];

                        // save the camera references
                        cameraSet.vrCameras[eyeIdx].cameraGameObject = kvrCameraGameObject;
                        cameraSet.vrCameras[eyeIdx].cameraComponent = kvrCameraComponent;
                    }

                    // save the camera set
                    cameraSet.kspCameraName = kspCameraName;
                    cameraSet.kspCameraComponent = null;
                    cameraSet.isInitialized = false;
                    VRCameraSets.Add(cameraSet);
                }
                isVrCameraRigCreated = true;
            }

            // then, need to check if the KSP camera has come into existance, so we
            // can copy its parameters
            for (int camIdx = 0; camIdx < VRCameraSets.Count; ++camIdx) {
                if (VRCameraSets[camIdx].isInitialized) continue;
                string kspCameraName = VRCameraSets[camIdx].kspCameraName;

                // TODO: optimize here; we should not be calling Find every frame
                GameObject kspCameraGameObject = GameObject.Find(kspCameraName);
                if (kspCameraGameObject == null) {
                    continue;
                }
                Camera kspCameraComponent = kspCameraGameObject.GetComponent<Camera>();

                // copy camera params for each eye
                for (int eyeIdx = 0; eyeIdx < 2; ++eyeIdx) {
                    EVREye eye = (EVREye)eyeIdx;

                    Camera kvrCameraComponent = VRCameraSets[camIdx].vrCameras[eyeIdx].cameraComponent;
                    kvrCameraComponent.depth = kspCameraComponent.depth + (eyeIdx * 0.5f);
                    kvrCameraComponent.clearFlags = kspCameraComponent.clearFlags;
                    kvrCameraComponent.backgroundColor = kspCameraComponent.backgroundColor;
                    kvrCameraComponent.cullingMask = kspCameraComponent.cullingMask;
                    kvrCameraComponent.orthographic = kspCameraComponent.orthographic;
                    kvrCameraComponent.nearClipPlane = kspCameraComponent.nearClipPlane;
                    kvrCameraComponent.farClipPlane = kspCameraComponent.farClipPlane;
                    kvrCameraComponent.depthTextureMode = kspCameraComponent.depthTextureMode;

                    // camera settings overrides
                    if (kspCameraName == "Landscape Camera" ||
                        kspCameraName == "InternalCamera" ||
                        kspCameraName == "Main Camera" ||
                        kspCameraName == "Camera 00") {
                        kvrCameraComponent.nearClipPlane = 0.01f;
                    }

                    // set VR specific settings
                    HmdMatrix44_t projectionMatrix = OpenVR.System.GetProjectionMatrix(
                        eye, kvrCameraComponent.nearClipPlane, kvrCameraComponent.farClipPlane);
                    kvrCameraComponent.projectionMatrix = MathUtils.Matrix4x4_OpenVr2UnityFormat(ref projectionMatrix);
                }

                VRCameraSets[camIdx].kspCameraComponent = kspCameraComponent;
                VRCameraSets[camIdx].isInitialized = true;
            }
        }

        protected void UpdateKspCamerasTargetState() {
            // get the names of cameras that are applicable to the current scene
            string[] currentCameraNames = GetCameraNamesForCurrentScene();
            if (currentCameraNames == null) {
                Utils.LogWarning("Unsupported VR scene");
                return;
            }

            // set all KSP camera states to off
            foreach (string name in KSP_CAMERA_NAMES_ALL) {
                kspCamerasTargetState[name].enabled = false;
            }

            // set all the cameras in the list to the opposite of the VR-enabled setting
            foreach (string name in currentCameraNames) {
                kspCamerasTargetState[name].enabled = !KerbalVR.Core.IsVrRunning;
            }
        }

        protected void UpdateCameraPositions() {
            // re-position cameras as modified by game controls (e.g. Kerbals moving about)
            switch (HighLogic.LoadedScene) {
                case GameScenes.FLIGHT:
                    if (FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.isEVA) {
                        CurrentPosition = FlightGlobals.ActiveVessel.evaController.helmetTransform.position;
                        CurrentRotation = FlightGlobals.ActiveVessel.evaController.helmetTransform.rotation;
                    }
                    break;
            }

            // get transforms to eye positions
            HmdMatrix34_t vrLeftEyeTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Left);
            HmdMatrix34_t vrRightEyeTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Right);

            // convert SteamVR poses to Unity coordinates
            var hmdTransform = new SteamVR_Utils.RigidTransform(KerbalVR.Core.GamePoses[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking);
            SteamVR_Utils.RigidTransform[] hmdEyeTransform = new SteamVR_Utils.RigidTransform[2];
            hmdEyeTransform[0] = new SteamVR_Utils.RigidTransform(vrLeftEyeTransform);
            hmdEyeTransform[1] = new SteamVR_Utils.RigidTransform(vrRightEyeTransform);

            /**
             * hmdEyeTransform is in a coordinate system that follows the headset, where
             * the origin is the headset device position. Therefore the eyes are at a fixed
             * offset from the device.
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

            // set camera positions to match device positions
            for (int camIdx = 0; camIdx < VRCameraSets.Count; ++camIdx) {
                Vector3[] eyeDisplacements = {
                     hmdTransform.rot * hmdEyeTransform[0].pos,
                     hmdTransform.rot * hmdEyeTransform[1].pos
                };
                Vector3[] updatedPositions = {
                    hmdTransform.pos + eyeDisplacements[0],
                    hmdTransform.pos + eyeDisplacements[1],
                };
                Quaternion updatedRotation = hmdTransform.rot;
                for (int eyeIdx = 0; eyeIdx < 2; ++eyeIdx) {
                    Types.VREyeCamera camStruct = VRCameraSets[camIdx].vrCameras[eyeIdx];
                    if (VRCameraSets[camIdx].kspCameraName == "GalaxyCamera" ||
                        VRCameraSets[camIdx].kspCameraName == "Camera ScaledSpace") {
                        // "GalaxyCamera" gets special treatment. we place both eyes at
                        // zero (origin) so that the skybox appears infinitely distant.
                        // in reality this skybox is like a 1m x 1m x 1m box that encloses
                        // the player. for funsies, try setting position to `eyeDisplacement[eyeIdx]`
                        // and watch what happens ;)  #easteregg
                        //
                        // "Camera ScaledSpace" also needs to stay at origin.
                        //
                        // Special special case: in IVA, need to convert Internal coordinates

                        if (IsInIVA()) {
                            camStruct.cameraGameObject.transform.rotation = InternalSpace.InternalToWorld(DevicePoseToWorld(updatedRotation));
                        }
                        else {
                            camStruct.cameraGameObject.transform.rotation = DevicePoseToWorld(updatedRotation);
                        }
                        camStruct.cameraGameObject.transform.position = Vector3.zero;
                    }
                    else {
                        if (IsInIVA()) {
                            // special case when we are in IVA. transform the "outside" cameras into
                            // internal space coordinates
                            if (VRCameraSets[camIdx].kspCameraName != "InternalCamera") {
                                camStruct.cameraGameObject.transform.position = InternalSpace.InternalToWorld(DevicePoseToWorld(updatedPositions[eyeIdx]));
                                camStruct.cameraGameObject.transform.rotation = InternalSpace.InternalToWorld(DevicePoseToWorld(updatedRotation));
                            }
                            else {
                                camStruct.cameraGameObject.transform.position = DevicePoseToWorld(updatedPositions[eyeIdx]);
                                camStruct.cameraGameObject.transform.rotation = DevicePoseToWorld(updatedRotation);
                            }
                        }
                        else {
                            // everything else moves according to tracked device positions
                            camStruct.cameraGameObject.transform.position = DevicePoseToWorld(updatedPositions[eyeIdx]);
                            camStruct.cameraGameObject.transform.rotation = DevicePoseToWorld(updatedRotation);
                        }
                    }
                }
            }
        }

        protected void OnIvaCameraChange(Kerbal kerbal) {
#if DEBUG
            Utils.Log("OnIvaCameraChange " + kerbal.crewMemberName);
#endif
            SetVrCamerasEnabled(KerbalVR.Core.IsVrRunning);
            SetVrCameraPositions();
        }

        protected void OnCameraChange(CameraManager.CameraMode mode) {
#if DEBUG
            Utils.Log("OnCameraChange " + mode);
#endif
            SetVrCamerasEnabled(KerbalVR.Core.IsVrRunning);
            SetVrCameraPositions();
        }

        protected void OnHmdStatusUpdated(bool isRunning) {
#if DEBUG
            Utils.Log("OnHmdStatusUpdated " + isRunning);
#endif
            // if enabling VR, ensure camera rig is built
            if (isRunning) {
                BuildVrCameraRig();
            }
            SetVrCamerasEnabled(isRunning);
            SetVrCameraPositions();
        }

        protected void OnCameraPreRender(Camera camera) {
            Types.CameraState targetCameraState;
            if (kspCamerasTargetState.TryGetValue(camera.name, out targetCameraState)) {
                camera.enabled = targetCameraState.enabled;
            }
        }

        protected void SetVrCameraPositions() {
            GameScenes scene = HighLogic.LoadedScene;
#if DEBUG
            Utils.Log("SetVrCameraPositions " + scene);
#endif

            // most scenese are seated tracking
            TrackingSpace = ETrackingUniverseOrigin.TrackingUniverseSeated;

            switch (scene) {
                case GameScenes.MAINMENU:
                    InitialPosition = Vector3.zero;
                    InitialRotation = Quaternion.identity;
                    break;

                case GameScenes.SPACECENTER:
                    // initial position is in the sky above the SpaceCenter
                    // InitialPosition = new Vector3(51.7f, -601.8f, 878.4f); // world coordinates
                    InitialPosition = SpaceCenter.Instance.cb.GetWorldSurfacePosition(-0.15426, -74.67217, 500); // approx lat/lon equivalent

                    // for rotation, look toward the SpaceCenter, but aligned with the surface normal
                    double lat, lon, alt;
                    SpaceCenter.Instance.cb.GetLatLonAlt(SpaceCenter.Instance.transform.position, out lat, out lon, out alt);
                    Vector3d spaceCenterPos = SpaceCenter.Instance.cb.GetWorldSurfacePosition(lat, lon, 500);
                    Vector3 lookTargetSc = spaceCenterPos - InitialPosition;
                    Vector3d surfaceNormal = SpaceCenter.Instance.cb.GetSurfaceNVector(-0.15426, -74.67217);
                    InitialRotation = Quaternion.LookRotation(lookTargetSc, surfaceNormal);
                    break;

                case GameScenes.TRACKSTATION:
                    // we are looking at things through the Scaled Space camera.
                    // initial position should always be zero (the Scaled Space camera does not move).
                    InitialPosition = Vector3.zero;

                    // look towards current body (NOT WORKING)
                    // Vector3 lookTargetCb = ScaledSpace.LocalToScaledSpace(Planetarium.fetch.CurrentMainBody.position);
                    Vector3 lookTargetCb = Planetarium.fetch.CurrentMainBody.scaledBody.transform.position;
                    InitialRotation = Quaternion.LookRotation(lookTargetCb, Vector3.up);

                    // Utils.Log("CB Position = " + Planetarium.fetch.CurrentMainBody.transform.position.ToString("F4"));
                    // Utils.Log("ScaledBody Position = " + Planetarium.fetch.CurrentMainBody.scaledBody.transform.position.ToString("F4"));
                    // Utils.Log("SS SceneTransform = " + ScaledSpace.SceneTransform.position.ToString("F3"));
                    break;

                case GameScenes.EDITOR:
                    TrackingSpace = ETrackingUniverseOrigin.TrackingUniverseStanding;
                    InitialPosition = new Vector3(0f, 0f, -5f);
                    InitialRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                    break;

                case GameScenes.FLIGHT:
                    if (CameraManager.Instance != null) {
                        switch (CameraManager.Instance.currentCameraMode) {
                            case CameraManager.CameraMode.IVA:
                                InitialPosition = InternalCamera.Instance.transform.position;
                                InitialRotation = InternalCamera.Instance.transform.rotation;
                                break;

                            case CameraManager.CameraMode.Map:
                                InitialPosition = Vector3.zero;
                                InitialRotation = Quaternion.identity;
                                break;

                            case CameraManager.CameraMode.Flight:
                                if (IsInEVA()) {
                                    Vector3 neckPos = FlightGlobals.ActiveVessel.evaController.helmetTransform.position;
                                    Quaternion neckRot = FlightGlobals.ActiveVessel.evaController.helmetTransform.rotation;
                                    InitialPosition = neckPos;
                                    InitialRotation = neckRot;
                                } else {
                                    InitialPosition = FlightCamera.fetch.GetCameraTransform().position;
                                    InitialRotation = FlightCamera.fetch.GetCameraTransform().rotation;
                                }
                                
                                break;

                            default:
                                Utils.LogWarning("SetVrCameraPositions unhandled flight scene");
                                break;
                        }
                    }
                    else {
                        InitialPosition = FlightCamera.fetch.GetCameraTransform().position;
                        InitialRotation = FlightCamera.fetch.GetCameraTransform().rotation;
                    }
                    break;

                default:
                    Utils.LogWarning("SetVrCameraPositions unhandled scene");
                    break;
            }
            CurrentPosition = InitialPosition;
            CurrentRotation = InitialRotation;

            // set the tracking space for this scene
            KerbalVR.Core.SetHmdTrackingSpace(TrackingSpace);
        }

        /// <summary>
        /// Set the VR cameras on or off, while taking care that the KSP
        /// cameras are turned back on if necessary.
        /// </summary>
        /// <param name="isVrEnabled">True to turn on VR cameras, false otherwise</param>
        protected void SetVrCamerasEnabled(bool isVrEnabled) {
            if (isVrEnabled) {
                EnableVrCameras();
            } else {
                DisableVrCameras();
            }
        }

        protected void EnableVrCameras() {
            // get all the cameras that should be turned on for this scene
            string[] currentCameraNames = GetCameraNamesForCurrentScene();
            if (currentCameraNames == null) {
                Utils.LogWarning("No VR cameras to enable in current scene");
                UpdateKspCamerasTargetState();
                return;
            }

            // turn off all VR cameras
            foreach (var camSet in VRCameraSets) {
                camSet.vrCameras[0].cameraComponent.enabled = false;
                camSet.vrCameras[1].cameraComponent.enabled = false;
            }

            // turn on VR cameras from list
            foreach (string name in currentCameraNames) {
                foreach (var camSet in VRCameraSets) {
                    if (camSet.kspCameraName == name) {
                        camSet.vrCameras[0].cameraComponent.enabled = true;
                        camSet.vrCameras[1].cameraComponent.enabled = true;
                    }
                }
            }

            // update KSP target states
            UpdateKspCamerasTargetState();
        }

        protected void DisableVrCameras() {
            // turn off all VR cameras
            foreach (var camSet in VRCameraSets) {
                camSet.vrCameras[0].cameraComponent.enabled = false;
                camSet.vrCameras[1].cameraComponent.enabled = false;
            }

            // turn on KSP cameras you would have already turned off previously
            string[] currentCameraNames = GetCameraNamesForCurrentScene();
            if (currentCameraNames == null) {
                Utils.LogWarning("DisableVrCameras unexpected state: currentCameraNames is null");
                UpdateKspCamerasTargetState();
                return;
            }

            foreach (string kspCameraName in currentCameraNames) {
                foreach (var cameraSet in VRCameraSets) {
                    if (cameraSet.kspCameraName == kspCameraName) {
                        if (cameraSet.kspCameraComponent != null) {
                            cameraSet.kspCameraComponent.enabled = true;
                        }
                        else {
                            // if this component is suddenly a null reference,
                            // try to find the camera again
                            GameObject kspCameraGameObject = GameObject.Find(kspCameraName);
                            if (kspCameraGameObject == null) {
                                Utils.LogWarning("DisableVrCameras: Unexpected state, cannot find camera GameObject " + kspCameraName);
                                continue;
                            }
                            cameraSet.kspCameraComponent = kspCameraGameObject.GetComponent<Camera>();
                            if (kspCameraGameObject == null) {
                                Utils.LogWarning("DisableVrCameras: Unexpected state, cannot find camera Component " + kspCameraName);
                                continue;
                            }
                            cameraSet.kspCameraComponent.enabled = true;
                        }
                    }
                }
            }

            // update KSP target states
            UpdateKspCamerasTargetState();
        }

        protected string[] GetCameraNamesForCurrentScene() {
            string[] cameraNames = null;
            GameScenes scene = HighLogic.LoadedScene;
            switch (HighLogic.LoadedScene) {
                case GameScenes.MAINMENU:
                    cameraNames = KSP_CAMERA_NAMES_MAINMENU;
                    break;

                case GameScenes.SPACECENTER:
                    cameraNames = KSP_CAMERA_NAMES_SPACECENTER;
                    break;

                case GameScenes.TRACKSTATION:
                    cameraNames = KSP_CAMERA_NAMES_TRACKSTATION;
                    break;

                case GameScenes.EDITOR:
                    cameraNames = KSP_CAMERA_NAMES_EDITOR;
                    break;

                case GameScenes.FLIGHT:
                    if (CameraManager.Instance != null) {
                        CameraManager.CameraMode mode = CameraManager.Instance.currentCameraMode;
                        switch (mode) {
                            case CameraManager.CameraMode.IVA:
                                cameraNames = KSP_CAMERA_NAMES_FLIGHT_IVA;
                                break;

                            case CameraManager.CameraMode.Map:
                                cameraNames = KSP_CAMERA_NAMES_FLIGHT_MAP;
                                break;

                            case CameraManager.CameraMode.Flight:
                                cameraNames = KSP_CAMERA_NAMES_FLIGHT;
                                break;
                        }
                    }
                    else {
                        cameraNames = KSP_CAMERA_NAMES_FLIGHT;
                    }
                    break;
            }

#if DEBUG
            if (cameraNames == null) {
                Utils.Log("GetCameraNamesForCurrentScene null");
            }
            else {
                string logMsg = "GetCameraNamesForCurrentScene " + scene + " " +
                    ((scene == GameScenes.FLIGHT && CameraManager.Instance != null) ?
                    CameraManager.Instance.currentCameraMode.ToString() : "") + "\n";
                foreach (string name in cameraNames) {
                    logMsg += name + "\n";
                }
                Utils.Log(logMsg);
            }
#endif
            return cameraNames;
        }

        /// <summary>
        /// Convert a device position to Unity world coordinates for this scene.
        /// </summary>
        /// <param name="devicePosition">Device position in the device space coordinate system.</param>
        /// <returns>Unity world position corresponding to the device position.</returns>
        public Vector3 DevicePoseToWorld(Vector3 devicePosition) {
            return CurrentPosition + CurrentRotation * devicePosition;
        }

        /// <summary>
        /// Convert a device rotation to Unity world coordinates for this scene.
        /// </summary>
        /// <param name="deviceRotation">Device rotation in the device space coordinate system.</param>
        /// <returns>Unity world rotation corresponding to the device rotation.</returns>
        public Quaternion DevicePoseToWorld(Quaternion deviceRotation) {
            return CurrentRotation * deviceRotation;
        }

        public static bool IsInIVA() {
            return (CameraManager.Instance != null) && (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA);
        }

        public static bool IsInEVA() {
            return (FlightGlobals.ActiveVessel != null) && FlightGlobals.ActiveVessel.isEVA;
        }
    } // class Scene
} // namespace KerbalVR
