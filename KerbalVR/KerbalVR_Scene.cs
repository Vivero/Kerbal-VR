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
        public static readonly string[] FLIGHT_SCENE_IVA_CAMERAS = {
            "GalaxyCamera",
            "Camera ScaledSpace",
            "Camera 00",
            "InternalCamera",
        };

        public static readonly string[] FLIGHT_SCENE_EVA_CAMERAS = {
            "GalaxyCamera",
            "Camera ScaledSpace",
            "Camera 00",
        };

        public static readonly string[] SPACECENTER_SCENE_CAMERAS = {
            "GalaxyCamera",
            "Camera ScaledSpace",
            "Camera 00",
        };

        public static readonly string[] EDITOR_SCENE_CAMERAS = {
            "GalaxyCamera",
            "sceneryCam",
            "Main Camera",
            "markerCam",
        };

        public static readonly string[] MAINMENU_SCENE_CAMERAS = {
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
        private void Initialize() {
            HmdEyePosition = new Vector3[2];
            HmdEyeRotation = new Quaternion[2];
        }
        #endregion


        #region Properties
        // The list of cameras to render for the current scene.
        public Types.CameraData[] VRCameras { get; private set; }
        public int NumVRCameras { get; private set; }

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

        /// <summary>
        /// The current position of the HMD in Unity world coordinates
        /// </summary>
        public Vector3 HmdPosition { get; private set; }
        /// <summary>
        /// The current rotation of the HMD in Unity world coordinates
        /// </summary>
        public Quaternion HmdRotation { get; private set; }

        /// <summary>
        /// The current position of the HMD eye in Unity world coordinates,
        /// indexed by EVREye value.
        /// </summary>
        public Vector3[] HmdEyePosition { get; private set; }
        /// <summary>
        /// The current rotation of the HMD left eye in Unity world coordinates,
        /// indexed by EVREye value.
        /// </summary>
        public Quaternion[] HmdEyeRotation { get; private set; }

        // defines the tracking method to use
        public ETrackingUniverseOrigin TrackingSpace { get; private set; }

        public KerbalVR.Types.VRCameraEyeRig[] VRCameraRigs { get; private set; } = new Types.VRCameraEyeRig[2];
        public bool IsVrCamerasReady { get; private set; } = false;
        public bool IsVrCamerasEnabled { get; private set; } = false;
        #endregion


        #region Private Members
        private GameObject galaxyCamera = null;
        private GameObject landscapeCamera = null;
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

            // enable the cameras if VR is enabled
            if (KerbalVR.Core.HmdIsRunning && !IsVrCamerasEnabled) {
                SetCamerasEnabled(true);
            }

            // disabled the cameras if VR is off
            if (!KerbalVR.Core.HmdIsRunning && IsVrCamerasEnabled) {
                SetCamerasEnabled(false);
            }

            switch (HighLogic.LoadedScene) {
                case GameScenes.MAINMENU:
                    break;
            }
        }


        protected void OnGameSceneLoadRequested(GameScenes scene) {
            Utils.Log("Setting up scene for " + scene.ToString());

            // tear down existing cameras
            Utils.Log("Tearing down cameras");
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
                        VREyeRenderer kvrCameraRenderer = kvrCameraGameObject.AddComponent<VREyeRenderer>();
                        kvrCameraRenderer.eye = eye;
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
                        kvrCameraComponent.targetTexture = KerbalVR.Core.hmdEyeRenderTexture[eyeIdx];

                        // set VR specific settings
                        HmdMatrix44_t projectionMatrix = OpenVR.System.GetProjectionMatrix(eye, kvrCameraComponent.nearClipPlane, kvrCameraComponent.farClipPlane);
                        kvrCameraComponent.projectionMatrix = MathUtils.Matrix4x4_OpenVr2UnityFormat(ref projectionMatrix);

                        // store references to objects
                        VRCameraRigs[eyeIdx].cameraGameObjects[0] = kvrCameraGameObject;
                        VRCameraRigs[eyeIdx].cameras[0] = kvrCameraComponent;

                        Utils.Log("Set up camera for eye " + eye.ToString());
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
        /// Updates the game cameras to the correct position, according to the given HMD eye pose.
        /// </summary>
        /// <param name="eyePosition">Position of the HMD eye, in the device space coordinate system</param>
        /// <param name="eyeRotation">Rotation of the HMD eye, in the device space coordinate system</param>
        public void UpdateScene(
            EVREye eye,
            SteamVR_Utils.RigidTransform hmdTransform,
            SteamVR_Utils.RigidTransform hmdEyeTransform) {
        }

        /// <summary>
        /// Populates the list of cameras according to the cameras that should be used for
        /// the current game scene.
        /// </summary>
        /// <param name="cameraNames">An array of camera names to use for this VR scene.</param>
        private void PopulateCameraList(string[] cameraNames) {
            // search for the cameras to render
            NumVRCameras = cameraNames.Length;
            VRCameras = new Types.CameraData[NumVRCameras];
            for (int i = 0; i < NumVRCameras; i++) {
                Camera foundCamera = Array.Find(Camera.allCameras, cam => cam.name.Equals(cameraNames[i]));
                if (foundCamera == null) {
                    Utils.LogError("Could not find camera \"" + cameraNames[i] + "\" in the scene!");

                } else {
                    // determine clip plane and new projection matrices
                    HmdMatrix44_t projectionMatrixL = OpenVR.System.GetProjectionMatrix(EVREye.Eye_Left, foundCamera.nearClipPlane, foundCamera.farClipPlane);
                    HmdMatrix44_t projectionMatrixR = OpenVR.System.GetProjectionMatrix(EVREye.Eye_Right, foundCamera.nearClipPlane, foundCamera.farClipPlane);

                    // store information about the camera
                    VRCameras[i].camera = foundCamera;
                    VRCameras[i].originalProjectionMatrix = foundCamera.projectionMatrix;
                    VRCameras[i].hmdProjectionMatrixL = MathUtils.Matrix4x4_OpenVr2UnityFormat(ref projectionMatrixL);
                    VRCameras[i].hmdProjectionMatrixR = MathUtils.Matrix4x4_OpenVr2UnityFormat(ref projectionMatrixR);

                    // disable the camera so we can call Render directly
                    foundCamera.enabled = false;

                    // cache the galaxy camera object, we'll need to call on it directly during eyeball positioning
                    if (foundCamera.name == "GalaxyCamera") {
                        galaxyCamera = foundCamera.gameObject;
                    } else if (foundCamera.name == "Landscape Camera") {
                        landscapeCamera = foundCamera.gameObject;
                    }
                }
            }
        }

        public bool SceneAllowsVR() {
            bool allowed = false;
            switch (HighLogic.LoadedScene) {
                case GameScenes.MAINMENU:
                    allowed = true;
                    break;

                case GameScenes.FLIGHT:
                    allowed = ((CameraManager.Instance != null) && (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA)) ||
                        ((FlightGlobals.ActiveVessel != null) && (FlightGlobals.ActiveVessel.isEVA));
                    break;

                case GameScenes.EDITOR:
                    allowed = true;
                    break;

                default:
                    allowed = false;
                    break;
            }
            return allowed;
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


    public class VREyeRenderer : MonoBehaviour {
        public EVREye eye { get; set; }

        protected Camera camera;
        protected VRTextureBounds_t hmdTextureBounds;

        protected void Awake() {
            camera = this.gameObject.GetComponent<Camera>();
            hmdTextureBounds = new VRTextureBounds_t();
            hmdTextureBounds.uMin = 0.0f;
            hmdTextureBounds.uMax = 1.0f;
            hmdTextureBounds.vMin = 1.0f; // flip the vertical coordinate for some reason
            hmdTextureBounds.vMax = 0.0f;
        }

        protected void OnPostRender() {
            Utils.Log("OnPostRender Eye: " + eye);

            // Submit frames to HMD
            Texture_t hmdEyeTexture = new Texture_t {
                handle = KerbalVR.Core.hmdEyeRenderTexture[(int)eye].GetNativeTexturePtr(),
                eColorSpace = EColorSpace.Auto,
                eType = ETextureType.DirectX
            };
            EVRCompositorError vrCompositorError = OpenVR.Compositor.Submit(eye, ref hmdEyeTexture, ref hmdTextureBounds, EVRSubmitFlags.Submit_Default);
            if (vrCompositorError != EVRCompositorError.None) {
                Utils.Log("Submit (" + eye + ") failed: (" + (int)vrCompositorError + ") " + vrCompositorError.ToString());
            }

            /*if (eye == EVREye.Eye_Right) {
                OpenVR.Compositor.PostPresentHandoff();
            }*/
        }
    }
} // namespace KerbalVR
