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

        public KerbalVR.Types.VREyeCameraRig[] VREyeCameraRigs { get; private set; } = new Types.VREyeCameraRig[2];
        public bool IsVrCamerasReady { get; private set; } = false;
        public bool IsVrCamerasEnabled { get; private set; } = false;
        #endregion


        #region Private Members
        #endregion


        protected void Awake() {
            // init some objects
            VREyeCameraRigs[0].cameras = new List<Types.VREyeCamera>(); // left eye
            VREyeCameraRigs[1].cameras = new List<Types.VREyeCamera>(); // right eye
        }

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
            // tear down existing cameras
            Utils.Log("Tearing down cameras for scene " + scene.ToString());
            for (int eyeIdx = 0; eyeIdx < 2; ++eyeIdx) {
                Utils.Log("Eye " + eyeIdx + " has " + VREyeCameraRigs[eyeIdx].cameras.Count + " cameras");
                for (int camIdx = 0; camIdx < VREyeCameraRigs[eyeIdx].cameras.Count; ++camIdx) {
                    Destroy(VREyeCameraRigs[eyeIdx].cameras[camIdx].cameraGameObject);
                }
                VREyeCameraRigs[eyeIdx].cameras.Clear();
            }
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
            for (int eyeIdx = 0; eyeIdx < 2; ++eyeIdx) {
                Vector3 eyeDisplacement = hmdTransform.rot * hmdEyeTransform[eyeIdx].pos;
                Vector3 updatedPosition = hmdTransform.pos + eyeDisplacement;
                Quaternion updatedRotation = hmdTransform.rot;
                for (int camIdx = 0; camIdx < VREyeCameraRigs[eyeIdx].cameras.Count; ++camIdx) {
                    Types.VREyeCamera camStruct = VREyeCameraRigs[eyeIdx].cameras[camIdx];
                    if (camStruct.kspCameraName == "GalaxyCamera") {
                        // galaxy camera gets special treatment. we place both eyes at
                        // zero (origin) so that the skybox appears infinitely distant.
                        // in reality this skybox is like a 1m x 1m x 1m box that encloses
                        // the player. for funsies, try setting position to `eyeDisplacement`
                        // and watch what happens ;)  #easteregg
                        camStruct.cameraGameObject.transform.position = Vector3.zero;
                        camStruct.cameraGameObject.transform.rotation = updatedRotation;
                    } else {
                        // everything else moves according to tracked device positions
                        camStruct.cameraGameObject.transform.position = updatedPosition;
                        camStruct.cameraGameObject.transform.rotation = updatedRotation;
                    }
                }
            }
        }

        protected void SetupCameras() {
            GameScenes scene = HighLogic.LoadedScene;

            // create new cameras
            switch (scene) {
                case GameScenes.MAINMENU:

                    // verify we can find the cameras we need
                    int numCameras = KSP_CAMERA_NAMES_MAINMENU.Length;
                    bool hasAllCameras = true;
                    for (int i = 0; i < numCameras; ++i) {
                        GameObject kspCameraGameObject = GameObject.Find(KSP_CAMERA_NAMES_MAINMENU[i]);
                        if (kspCameraGameObject == null) {
                            hasAllCameras = false;
                            break;
                        }
                    }if (!hasAllCameras) {
                        return;
                    }

                    // set the tracking space for this scene
                    TrackingSpace = ETrackingUniverseOrigin.TrackingUniverseSeated;
                    KerbalVR.Core.SetHmdTrackingSpace(TrackingSpace);

                    // create two sets of cameras (one for each eye)
                    for (int eyeIdx = 0; eyeIdx < 2; ++eyeIdx) {
                        EVREye eye = (EVREye)eyeIdx;
                        VREyeCameraRigs[eyeIdx].cameras = new List<Types.VREyeCamera>(numCameras);

                        for (int i = 0; i < numCameras; ++i) {
                            Types.VREyeCamera camStruct = new Types.VREyeCamera();

                            string kspCameraName = KSP_CAMERA_NAMES_MAINMENU[i];
                            GameObject kspCameraGameObject = GameObject.Find(KSP_CAMERA_NAMES_MAINMENU[i]);

                            string kvrCameraName = "KVR_Eye_Camera (" + eye.ToString() + ") (" + kspCameraName + ")";
                            GameObject kvrCameraGameObject = new GameObject(kvrCameraName);
                            Camera kvrCameraComponent = kvrCameraGameObject.AddComponent<Camera>();
                            kvrCameraComponent.enabled = false;

                            // copy camera settings
                            Camera kspCameraComponent = kspCameraGameObject.GetComponent<Camera>();
                            kvrCameraComponent.depth = kspCameraComponent.depth + eyeIdx;
                            kvrCameraComponent.clearFlags = kspCameraComponent.clearFlags; // CameraClearFlags.SolidColor;
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

                            // store references to objects
                            camStruct.cameraGameObject = kvrCameraGameObject;
                            camStruct.cameraComponent = kvrCameraComponent;
                            camStruct.kspCameraName = kspCameraName;
                            VREyeCameraRigs[eyeIdx].cameras.Add(camStruct);
                        }
                        Utils.Log("Set up " + scene.ToString() + " camera for eye " + eye.ToString());
                        IsVrCamerasReady = true;
                    }
                    break;
            }
        }

        protected void SetCamerasEnabled(bool isEnabled) {
            for (int eyeIdx = 0; eyeIdx < 2; ++eyeIdx) {
                foreach (var cameraStruct in VREyeCameraRigs[eyeIdx].cameras) {
                    cameraStruct.cameraComponent.enabled = isEnabled;
                }
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
