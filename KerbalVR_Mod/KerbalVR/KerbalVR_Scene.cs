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
        public static readonly string[] KSP_CAMERA_NAMES_FLIGHT = {
            "GalaxyCamera",
            "Camera ScaledSpace",
            "Camera 00",
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
        public bool IsVrCamerasReady { get; private set; } = false;
        public bool IsVrCamerasEnabled { get; private set; } = false;
        #endregion


        #region Private Members
        #endregion


        protected void Awake() { }

        protected void OnEnable() {
            // setup callback functions for events
            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);
            KerbalVR.Events.HmdStatusUpdated.Listen(OnHmdStatusUpdated);
            SteamVR_Events.NewPoses.Listen(OnNewPosesReady);
        }

        protected void OnDisable() {
            // remove callback functions
            GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequested);
            KerbalVR.Events.HmdStatusUpdated.Remove(OnHmdStatusUpdated);
            SteamVR_Events.NewPoses.Remove(OnNewPosesReady);
        }


        protected void LateUpdate() {
            // set up the cameras
            if (!IsVrCamerasReady) {
                SetupCameras();
                return;
            }
        }


        protected void OnGameSceneLoadRequested(GameScenes scene) {
            // disable the cameras
            SetCamerasEnabled(false);

            // tear down existing camera sets
            int numCameraSets = VRCameraSets.Count;
            Utils.Log("Tearing down cameras for scene " + scene.ToString() + " (" + numCameraSets + " sets)");
            for (int camIdx = 0; camIdx < numCameraSets; ++camIdx) {
                for (int eyeIdx = 0; eyeIdx < 2; ++eyeIdx) {
                    VRCameraSets[camIdx].vrCameras[eyeIdx].cameraComponent.targetTexture = null;
                    Destroy(VRCameraSets[camIdx].vrCameras[eyeIdx].cameraComponent);
                    Destroy(VRCameraSets[camIdx].vrCameras[eyeIdx].cameraGameObject);
                }
            }
            VRCameraSets.Clear();
            IsVrCamerasReady = false;
        }

        protected void OnHmdStatusUpdated(bool isRunning) {
            SetCamerasEnabled(isRunning);
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
                        // galaxy camera gets special treatment. we place both eyes at
                        // zero (origin) so that the skybox appears infinitely distant.
                        // in reality this skybox is like a 1m x 1m x 1m box that encloses
                        // the player. for funsies, try setting position to `eyeDisplacement[eyeIdx]`
                        // and watch what happens ;)  #easteregg
                        camStruct.cameraGameObject.transform.position = Vector3.zero;
                        camStruct.cameraGameObject.transform.rotation = DevicePoseToWorld(updatedRotation);
                    }
                    else {
                        // everything else moves according to tracked device positions
                        camStruct.cameraGameObject.transform.position = DevicePoseToWorld(updatedPositions[eyeIdx]);
                        camStruct.cameraGameObject.transform.rotation = DevicePoseToWorld(updatedRotation);
                    }
                }
            }
        }

        protected void SetupCameras() {
            GameScenes scene = HighLogic.LoadedScene;

            // create new cameras
            string[] cameraNames;
            switch (scene) {
                case GameScenes.MAINMENU:
                    cameraNames = KSP_CAMERA_NAMES_MAINMENU;
                    InitialPosition = Vector3.zero;
                    InitialRotation = Quaternion.identity;
                    break;

                case GameScenes.SPACECENTER:
                    cameraNames = KSP_CAMERA_NAMES_SPACECENTER;
                    InitialPosition = new Vector3(51.7f, -601.8f, 878.4f);
                    // InitialRotation = new Quaternion(0.5f, -0.8f, -0.3f, -0.2f); // tilted a bit downwards
                    // InitialRotation = Quaternion.Euler(0f, 0f, 90f); // looking towards mountains in back, but tilted upward
                    // InitialRotation = Quaternion.Euler(0f, 90f, 0f); // looking down to earth
                    InitialRotation = Quaternion.Euler(90f, 0f, 0f);
                    break;

                case GameScenes.FLIGHT:
                    cameraNames = KSP_CAMERA_NAMES_FLIGHT;
                    InitialPosition = new Vector3(0.6f, -28.1f, 10.1f);
                    InitialRotation = new Quaternion(-0.4f, 0.7f, 0.5f, 0.3f);
                    break;

                default:
                    return;
            }
            CurrentPosition = InitialPosition;
            CurrentRotation = InitialRotation;

            // verify we can find the cameras we need
            int numCameras = cameraNames.Length;
            bool hasAllCameras = true;
            for (int i = 0; i < numCameras; ++i) {
                GameObject kspCameraGameObject = GameObject.Find(cameraNames[i]);
                if (kspCameraGameObject == null) {
                    hasAllCameras = false;
                    break;
                }
            }
            if (!hasAllCameras) {
                return;
            }

            // set the tracking space for this scene
            TrackingSpace = ETrackingUniverseOrigin.TrackingUniverseSeated;
            KerbalVR.Core.SetHmdTrackingSpace(TrackingSpace);

            // create two sets of cameras (one for each eye)
            for (int camIdx = 0; camIdx < numCameras; ++camIdx) {
                Types.VRCameraSet cameraSet = new Types.VRCameraSet();
                string kspCameraName = cameraNames[camIdx];
                GameObject kspCameraGameObject = GameObject.Find(kspCameraName);
                Camera kspCameraComponent = kspCameraGameObject.GetComponent<Camera>();

                // create a camera for each eye
                cameraSet.vrCameras = new Types.VREyeCamera[2];
                for (int eyeIdx = 0; eyeIdx < 2; ++eyeIdx) {
                    EVREye eye = (EVREye)eyeIdx;

                    string kvrCameraName = "KVR_Eye_Camera (" + eye.ToString() + ") (" + kspCameraName + ")";
                    GameObject kvrCameraGameObject = new GameObject(kvrCameraName);
                    Camera kvrCameraComponent = kvrCameraGameObject.AddComponent<Camera>();
                    kvrCameraComponent.enabled = false;

                    // copy camera settings
                    kvrCameraComponent.depth = kspCameraComponent.depth + eyeIdx;
                    kvrCameraComponent.clearFlags = kspCameraComponent.clearFlags;
                    kvrCameraComponent.backgroundColor = kspCameraComponent.backgroundColor;
                    kvrCameraComponent.cullingMask = kspCameraComponent.cullingMask;
                    kvrCameraComponent.orthographic = kspCameraComponent.orthographic;
                    kvrCameraComponent.nearClipPlane = kspCameraComponent.nearClipPlane;
                    kvrCameraComponent.farClipPlane = kspCameraComponent.farClipPlane;
                    kvrCameraComponent.depthTextureMode = kspCameraComponent.depthTextureMode;
                    kvrCameraComponent.targetTexture = KerbalVR.Core.HmdEyeRenderTexture[eyeIdx];

                    // camera settings overrides
                    if (kspCameraName == "Landscape Camera") {
                        kvrCameraComponent.nearClipPlane = 0.01f;
                    }

                    // set VR specific settings
                    HmdMatrix44_t projectionMatrix = OpenVR.System.GetProjectionMatrix(eye, kvrCameraComponent.nearClipPlane, kvrCameraComponent.farClipPlane);
                    kvrCameraComponent.projectionMatrix = MathUtils.Matrix4x4_OpenVr2UnityFormat(ref projectionMatrix);

                    cameraSet.vrCameras[eyeIdx].cameraGameObject = kvrCameraGameObject;
                    cameraSet.vrCameras[eyeIdx].cameraComponent = kvrCameraComponent;

                    Utils.Log("Set up " + scene.ToString() + " camera (" + kspCameraName + ") for eye " + eye.ToString());
                }

                cameraSet.kspCameraName = kspCameraName;
                cameraSet.kspCameraComponent = kspCameraComponent;
                cameraSet.kspCameraState = new Types.CameraState();
                cameraSet.kspCameraState.enabled = kspCameraComponent.enabled;
                VRCameraSets.Add(cameraSet);
            }
            IsVrCamerasReady = true;
        }

        protected void SetCamerasEnabled(bool isEnabled) {
            for (int camIdx = 0; camIdx < VRCameraSets.Count; ++camIdx) {
                // turn on/off all the VR cameras
                for (int eyeIdx = 0; eyeIdx < 2; ++eyeIdx) {
                    VRCameraSets[camIdx].vrCameras[eyeIdx].cameraComponent.enabled = true;
                }

                Utils.Log("KSP Camera State Before (VR? " + isEnabled + ") " + VRCameraSets[camIdx].kspCameraName + " " + VRCameraSets[camIdx].kspCameraComponent.enabled);

                if (isEnabled) {
                    // if enabling VR cameras, disable the KSP cameras but save their state first
                    VRCameraSets[camIdx].kspCameraState.enabled = VRCameraSets[camIdx].kspCameraComponent.enabled;
                    VRCameraSets[camIdx].kspCameraComponent.enabled = false;
                } else {
                    // if disabling VR cameras, reset KSP cameras to previous state
                    VRCameraSets[camIdx].kspCameraComponent.enabled = VRCameraSets[camIdx].kspCameraState.enabled;
                }

                Utils.Log("KSP Camera State After (VR? " + isEnabled + ") " + VRCameraSets[camIdx].kspCameraName + " " + VRCameraSets[camIdx].kspCameraComponent.enabled);
            }
            IsVrCamerasEnabled = isEnabled;
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
    } // class Scene
} // namespace KerbalVR
