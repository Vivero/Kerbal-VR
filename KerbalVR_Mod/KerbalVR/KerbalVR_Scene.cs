using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    /// <summary>
    /// Scene is a singleton class that encapsulates the code that positions
    /// the game cameras correctly for rendering them to the VR headset,
    /// according to the current KSP scene (flight, editor, etc).
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
        public static readonly string[] KSP_CAMERA_NAMES_EDITOR = {
            "GalaxyCamera",
            "sceneryCam",
            "Main Camera",
            // "markerCam",
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

        public static readonly string[] KSP_CAMERA_NAMES_ALL = {
            "GalaxyCamera",
            "Landscape Camera",
            "Camera ScaledSpace",
            "Camera 00",
            "InternalCamera",
            "sceneryCam",
            "Main Camera",
            // "markerCam",
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
        private void Initialize() { }
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
        #endregion


        protected void Awake() { }

        protected void OnEnable() {
            // setup callback functions for events
            GameEvents.OnIVACameraKerbalChange.Add(OnIvaCameraChange);
            GameEvents.OnCameraChange.Add(OnCameraChange);
            KerbalVR.Events.HmdStatusUpdated.Listen(OnHmdStatusUpdated);
        }

        protected void OnDisable() {
            // remove callback functions
            GameEvents.OnIVACameraKerbalChange.Remove(OnIvaCameraChange);
            GameEvents.OnCameraChange.Remove(OnCameraChange);
            KerbalVR.Events.HmdStatusUpdated.Remove(OnHmdStatusUpdated);
        }


        protected void Update() {
            string logMsg = "Camera States\n";
            foreach (string name in KSP_CAMERA_NAMES_ALL) {
                logMsg += name + ": ";
                GameObject camObj = GameObject.Find(name);
                if (camObj == null) {
                    logMsg += "no object\n";
                    continue;
                }
                Camera cam = camObj.GetComponent<Camera>();
                if (cam == null) {
                    logMsg += "no component\n";
                    continue;
                }
                logMsg += (cam.enabled ? "enabled" : "disabled") + "\n";
            }
            // Utils.SetDebugText(logMsg);

            currentScene = HighLogic.LoadedScene;
            if (currentScene != previousScene) {
                // a change in scene has triggered, reset the cameras
                Utils.Log("Game Scene Change (" + previousScene + " -> " + currentScene + ")");
                SetVrCamerasEnabled(KerbalVR.Core.IsVrRunning);
                SetVrCameraPositions();
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

        protected void BuildVrCameraRig() {
            // initially, create the set of all VR cameras
            if (!isVrCameraRigCreated) {
                Utils.Log("Building VR Camera Rig...");
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

                GameObject kspCameraGameObject = GameObject.Find(kspCameraName);
                if (kspCameraGameObject == null) {
                    continue;
                }
                Camera kspCameraComponent = kspCameraGameObject.GetComponent<Camera>();

                // copy camera params for each eye
                for (int eyeIdx = 0; eyeIdx < 2; ++eyeIdx) {
                    EVREye eye = (EVREye)eyeIdx;

                    Camera kvrCameraComponent = VRCameraSets[camIdx].vrCameras[eyeIdx].cameraComponent;
                    if (kvrCameraComponent == null) Utils.Log("NULL for " + eyeIdx + " " + kspCameraName);
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

        protected void UpdateCameraPositions() {
            // re-position cameras as modified by game controls (e.g. Kerbals moving about)
            switch (HighLogic.LoadedScene) {
                case GameScenes.FLIGHT:
                    if (FlightGlobals.ActiveVessel.isEVA) {
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

        protected void onGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> fromToAction) {
            Utils.Log("Game Scene Switch (" + fromToAction.from + " -> " + fromToAction.to + ") " + HighLogic.LoadedScene);
            SetVrCamerasEnabled(KerbalVR.Core.IsVrRunning);
            SetVrCameraPositions();
        }

        protected void OnIvaCameraChange(Kerbal kerbal) {
            SetVrCamerasEnabled(KerbalVR.Core.IsVrRunning);
            SetVrCameraPositions();
        }

        protected void OnCameraChange(CameraManager.CameraMode mode) {
            SetVrCamerasEnabled(KerbalVR.Core.IsVrRunning);
            SetVrCameraPositions();
        }

        protected void OnHmdStatusUpdated(bool isRunning) {
            // if enabling VR, ensure camera rig is built
            if (isRunning) {
                BuildVrCameraRig();
            }
            SetVrCamerasEnabled(isRunning);
            SetVrCameraPositions();
        }

        protected void OnNewPosesReady(TrackedDevicePose_t[] poses) {
            // get transforms to eye positions
            HmdMatrix34_t vrLeftEyeTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Left);
            HmdMatrix34_t vrRightEyeTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Right);

            // convert SteamVR poses to Unity coordinates
            var hmdTransform = new SteamVR_Utils.RigidTransform(poses[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking);
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
                            } else {
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

        protected void SetVrCameraPositions() {
            GameScenes scene = HighLogic.LoadedScene;
            Utils.Log("SetVrCameraPositions " + scene.ToString());

            // most scenese are seated tracking
            TrackingSpace = ETrackingUniverseOrigin.TrackingUniverseSeated;

            switch (scene) {
                case GameScenes.MAINMENU:
                    InitialPosition = Vector3.zero;
                    InitialRotation = Quaternion.identity;
                    break;

                case GameScenes.SPACECENTER:
                    InitialPosition = new Vector3(51.7f, -601.8f, 878.4f);
                    InitialRotation = Quaternion.identity;
                    break;

                case GameScenes.EDITOR:
                    TrackingSpace = ETrackingUniverseOrigin.TrackingUniverseStanding;
                    InitialPosition = new Vector3(0f, 0f, -5f);
                    InitialRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                    break;

                case GameScenes.FLIGHT:
                    if (CameraManager.Instance != null) {
                        Utils.Log("SetVrCameraPositions " + scene.ToString() + " " + CameraManager.Instance.currentCameraMode.ToString());
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
                                if (FlightGlobals.ActiveVessel.isEVA) {
                                    Utils.Log("SetVrCameraPositions EVA");
                                    Utils.PrintGameObjectTree(FlightGlobals.ActiveVessel.evaController.gameObject);
                                    Vector3 neckPos = FlightGlobals.ActiveVessel.evaController.helmetTransform.position;
                                    Quaternion neckRot = FlightGlobals.ActiveVessel.evaController.helmetTransform.rotation;
                                    // InitialPosition = FlightGlobals.ActiveVessel.transform.position;
                                    // InitialRotation = FlightGlobals.ActiveVessel.transform.rotation;
                                    InitialPosition = neckPos;
                                    InitialRotation = neckRot;
                                } else {
                                    InitialPosition = FlightCamera.fetch.GetCameraTransform().position;
                                    InitialRotation = FlightCamera.fetch.GetCameraTransform().rotation;
                                }
                                
                                break;

                            default:
                                Utils.LogWarning("unhandled flight scene");
                                break;
                        }
                    }
                    else {
                        Utils.Log("SetVrCameraPositions " + scene.ToString() + " other");
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
            // reset all cameras (VR cameras off, KSP cameras on)
            ResetCameraStates();

            // if we're enabling VR, get the cameras we care about.
            // otherwise, VR will be off after the ResetCameraStates,
            // so do nothing else.
            if (isVrEnabled) {
                string[] cameraNames = GetCameraNamesForCurrentScene();
                if (cameraNames == null) {
                    Utils.LogWarning("SetVrCamerasEnabled " + isVrEnabled + ": no camera names for current scene");
                    return;
                }

                foreach (string kspCameraName in cameraNames) {
                    foreach (var cameraSet in VRCameraSets) {
                        if (cameraSet.kspCameraName == kspCameraName) {
                            cameraSet.vrCameras[0].cameraComponent.enabled = true;
                            cameraSet.vrCameras[1].cameraComponent.enabled = true;

                            if (cameraSet.kspCameraComponent != null) {
                                cameraSet.kspCameraComponent.enabled = false;
                            }
                            else {
                                // if this component is suddenly a null reference,
                                // try to find the camera again
                                GameObject kspCameraGameObject = GameObject.Find(kspCameraName);
                                if (kspCameraGameObject == null) {
                                    Utils.LogWarning("SetVrCamerasEnabled " + isVrEnabled + ": Unexpected state, cannot find camera GameObject " + kspCameraName);
                                    continue;
                                }
                                cameraSet.kspCameraComponent = kspCameraGameObject.GetComponent<Camera>();
                                if (kspCameraGameObject == null) {
                                    Utils.LogWarning("SetVrCamerasEnabled " + isVrEnabled + ": Unexpected state, cannot find camera Component " + kspCameraName);
                                    continue;
                                }
                                cameraSet.kspCameraComponent.enabled = false;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Turns off all VR cameras, and re-enables all KSP cameras. If
        /// we lost some references to KSP cameras, try to find them again.
        /// </summary>
        protected void ResetCameraStates() {
            // turn off all the KSP cameras
            foreach (string kspCameraName in KSP_CAMERA_NAMES_ALL) {
                foreach (var cameraSet in VRCameraSets) {
                    if (cameraSet.kspCameraName == kspCameraName) {
                        if (cameraSet.kspCameraComponent != null) {
                            cameraSet.kspCameraComponent.enabled = false;
                        }
                        else {
                            // if we lost the reference to this camera object,
                            // then try to find it again
                            GameObject kspCameraGameObject = GameObject.Find(kspCameraName);
                            if (kspCameraGameObject == null) {
                                // Utils.LogWarning("ResetCameraStates: Unexpected state, cannot find camera GameObject " + kspCameraName);
                                // at this point, it doesn't exist, so there's nothing to turn off anyways
                                continue;
                            }
                            cameraSet.kspCameraComponent = kspCameraGameObject.GetComponent<Camera>();
                            if (kspCameraGameObject == null) {
                                Utils.LogWarning("ResetCameraStates: Unexpected state, cannot find camera Component " + kspCameraName);
                                continue;
                            }
                            cameraSet.kspCameraComponent.enabled = false;
                        }
                    }
                }
            }

            // turn off all VR cameras
            foreach (var cameraSet in VRCameraSets) {
                cameraSet.vrCameras[0].cameraComponent.enabled = false;
                cameraSet.vrCameras[1].cameraComponent.enabled = false;
            }

            // turn on only the KSP cameras needed in the current scene
            string[] activeKspCameraNames = GetCameraNamesForCurrentScene();
            if (activeKspCameraNames == null) {
                Utils.LogWarning("ResetCameraStates: No cameras for the current scene");
                return;
            }

            foreach (string kspCameraName in activeKspCameraNames) {
                Utils.Log("KSP Active Camera: " + kspCameraName);
                foreach (var cameraSet in VRCameraSets) {
                    if (cameraSet.kspCameraName == kspCameraName) {
                        if (cameraSet.kspCameraComponent != null) {
                            cameraSet.kspCameraComponent.enabled = true;
                        } else {
                            // if we didn't find this component before, we're not gonna find it here again
                            Utils.LogWarning("ResetCameraStates: Unexpected state, cannot find camera Component to re-activate " + kspCameraName);
                        }
                    }
                }
            }
        }

        protected string[] GetCameraNamesForCurrentScene() {
            string[] cameraNames = null;
            GameScenes scene = HighLogic.LoadedScene;
            Utils.Log("GetCameraNamesForCurrentScene " + scene.ToString());
            switch (HighLogic.LoadedScene) {
                case GameScenes.MAINMENU:
                    cameraNames = KSP_CAMERA_NAMES_MAINMENU;
                    break;

                case GameScenes.SPACECENTER:
                    cameraNames = KSP_CAMERA_NAMES_SPACECENTER;
                    break;

                case GameScenes.EDITOR:
                    cameraNames = KSP_CAMERA_NAMES_EDITOR;
                    break;

                case GameScenes.FLIGHT:
                    if (CameraManager.Instance != null) {
                        CameraManager.CameraMode mode = CameraManager.Instance.currentCameraMode;
                        Utils.Log("GetCameraNamesForCurrentScene " + scene.ToString() + " " + mode.ToString());
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
                        Utils.Log("GetCameraNamesForCurrentScene " + scene.ToString() + " other");
                        cameraNames = KSP_CAMERA_NAMES_FLIGHT;
                    }
                    break;
            }
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

        protected bool IsInIVA() {
            return (CameraManager.Instance != null) && (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA);
        }
    } // class Scene
} // namespace KerbalVR
