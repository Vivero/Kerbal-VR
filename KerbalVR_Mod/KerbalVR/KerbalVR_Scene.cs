using System;
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
        public ETrackingUniverseOrigin TrackingSpace { get; private set; }

        public KerbalVR.Types.VRCameraEyeRig[] VRCameraRigs { get; private set; } = new Types.VRCameraEyeRig[2];
        public bool IsVrCamerasReady { get; private set; } = false;
        public bool IsVrCamerasEnabled { get; private set; } = false;
        #endregion


        #region Private Members
        #endregion


        protected void Awake() {
            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);
            VRCameraRigs[0] = new Types.VRCameraEyeRig(); // left eye
            VRCameraRigs[1] = new Types.VRCameraEyeRig(); // right eye
        }


        protected void Update() {
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
                if (VRCameraRigs[eyeIdx].cameraGameObjects != null) {
                    Utils.Log("Eye " + eyeIdx + " has " + VRCameraRigs[eyeIdx].cameraGameObjects.Length + " cameras");
                    for (int camIdx = 0; camIdx < VRCameraRigs[eyeIdx].cameraGameObjects.Length; ++camIdx) {
                        Destroy(VRCameraRigs[eyeIdx].cameraGameObjects[camIdx]);
                        VRCameraRigs[eyeIdx].cameraGameObjects[camIdx] = null;
                        VRCameraRigs[eyeIdx].cameras[camIdx] = null;
                    }
                }
            }
            IsVrCamerasReady = false;
        }

        protected void SetupCameras() {
            GameScenes scene = HighLogic.LoadedScene;

            // create new cameras
            switch (scene) {
                case GameScenes.MAINMENU:

                    // string kspCameraName = "GalaxyCamera";
                    string kspCameraName = "Landscape Camera";
                    GameObject kspCameraGameObject = GameObject.Find(kspCameraName);
                    if (kspCameraGameObject == null) {
                        return;
                    }

                    for (int eyeIdx = 0; eyeIdx < 2; ++eyeIdx) {
                        EVREye eye = (EVREye)eyeIdx;

                        VRCameraRigs[eyeIdx].cameraGameObjects = new GameObject[1];
                        VRCameraRigs[eyeIdx].cameras = new Camera[1];
                        
                        string kvrCameraName = "KVR_Eye_Camera (" + eye.ToString() + ") (" + kspCameraName + ")";
                        GameObject kvrCameraGameObject = new GameObject(kvrCameraName);
                        Camera kvrCameraComponent = kvrCameraGameObject.AddComponent<Camera>();
                        kvrCameraComponent.enabled = false;

                        // copy camera settings
                        Camera kspCameraComponent = kspCameraGameObject.GetComponent<Camera>();
                        kvrCameraComponent.depth = kspCameraComponent.depth + eyeIdx;
                        kvrCameraComponent.clearFlags = CameraClearFlags.SolidColor;
                        kvrCameraComponent.backgroundColor = Color.red;
                        kvrCameraComponent.cullingMask = kspCameraComponent.cullingMask;
                        kvrCameraComponent.orthographic = kspCameraComponent.orthographic;
                        kvrCameraComponent.nearClipPlane = 0.01f;
                        kvrCameraComponent.farClipPlane = kspCameraComponent.farClipPlane;
                        kvrCameraComponent.depthTextureMode = kspCameraComponent.depthTextureMode;
                        kvrCameraComponent.targetTexture = KerbalVR.Core.HmdEyeRenderTexture[eyeIdx];

                        // set VR specific settings
                        HmdMatrix44_t projectionMatrix = OpenVR.System.GetProjectionMatrix(eye, kvrCameraComponent.nearClipPlane, kvrCameraComponent.farClipPlane);
                        kvrCameraComponent.projectionMatrix = MathUtils.Matrix4x4_OpenVr2UnityFormat(ref projectionMatrix);

                        // store references to objects
                        VRCameraRigs[eyeIdx].cameraGameObjects[0] = kvrCameraGameObject;
                        VRCameraRigs[eyeIdx].cameras[0] = kvrCameraComponent;

                        Utils.Log("Set up " + scene.ToString() + " camera for eye " + eye.ToString());
                        IsVrCamerasReady = true;
                    }
                    break;
            }
        }

        protected void SetCamerasEnabled(bool isEnabled) {
            for (int eyeIdx = 0; eyeIdx < 2; ++eyeIdx) {
                if (VRCameraRigs[eyeIdx].cameras != null) {
                    for (int camIdx = 0; camIdx < VRCameraRigs[eyeIdx].cameras.Length; ++camIdx) {
                        VRCameraRigs[eyeIdx].cameras[camIdx].enabled = isEnabled;
                    }
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
