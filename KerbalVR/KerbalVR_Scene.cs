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
        public static readonly string[] FLIGHT_SCENE_CAMERAS = {
            "GalaxyCamera",
            "Camera ScaledSpace",
            "Camera 01",
            "Camera 00",
            "InternalCamera",
        };

        public static readonly string[] SPACECENTER_SCENE_CAMERAS = {
            "GalaxyCamera",
            "Camera ScaledSpace",
            "Camera 01",
            "Camera 00",
        };

        public static readonly string[] EDITOR_SCENE_CAMERAS = {
            "GalaxyCamera",
            "sceneryCam",
            "Main Camera",
            "markerCam",
        };
        #endregion

        #region Singleton
        // this is a singleton class, and there must be one Scene in the scene
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

        // first-time initialization for this singleton class
        private void Initialize() {
            HmdEyePosition = new Vector3[2];
            HmdEyeRotation = new Quaternion[2];

            // initialize world scale values
            inverseWorldScale = new Dictionary<GameScenes, float>();
            inverseWorldScale.Add(GameScenes.MAINMENU, 1f);
            inverseWorldScale.Add(GameScenes.SPACECENTER, 1f);
            inverseWorldScale.Add(GameScenes.TRACKSTATION, 1f);
            inverseWorldScale.Add(GameScenes.FLIGHT, 1f);
            inverseWorldScale.Add(GameScenes.EDITOR, 1f);
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

        // defines what layer to render KerbalVR objects on
        public int RenderLayer { get; private set; }

        // defines the world scaling factor (store the inverse)
        public float WorldScale {
            get { return (1f / inverseWorldScale[HighLogic.LoadedScene]); }
            set { inverseWorldScale[HighLogic.LoadedScene] = (1f / value); }
        }
        #endregion


        #region Private Members
        private Dictionary<GameScenes, float> inverseWorldScale;
        private float editorMovementSpeed = 1f;
        #endregion


        void OnEnable() {
            Events.ManipulatorLeftUpdated.Listen(OnManipulatorLeftUpdated);
            Events.ManipulatorRightUpdated.Listen(OnManipulatorRightUpdated);
        }

        void OnDisable() {
            Events.ManipulatorLeftUpdated.Remove(OnManipulatorLeftUpdated);
            Events.ManipulatorRightUpdated.Remove(OnManipulatorRightUpdated);
        }


        /// <summary>
        /// Set up the list of cameras to render for this scene and the initial position
        /// corresponding to the origin in the real world device coordinate system.
        /// </summary>
        public void SetupScene() {
            switch (HighLogic.LoadedScene) {
                case GameScenes.FLIGHT:
                    SetupFlightScene();
                    break;

                case GameScenes.EDITOR:
                    SetupEditorScene();
                    break;

                default:
                    throw new Exception("Cannot setup VR scene, current scene \"" +
                        HighLogic.LoadedScene + "\" is invalid.");
            }

            CurrentPosition = InitialPosition;
            CurrentRotation = InitialRotation;
        }

        private void SetupFlightScene() {
            // use seated mode during IVA flight
            TrackingSpace = ETrackingUniverseOrigin.TrackingUniverseSeated;

            // render KerbalVR objects on the InternalSpace layer
            RenderLayer = 20;

            // generate list of cameras to render
            PopulateCameraList(FLIGHT_SCENE_CAMERAS);

            // set inital scene position
            InitialPosition = InternalCamera.Instance.transform.position;

            // set rotation to always point forward inside the cockpit
            // NOTE: actually this code doesn't work for certain capsules
            // with different internal origin orientations
            /*InitialRotation = Quaternion.LookRotation(
                InternalSpace.Instance.transform.rotation * Vector3.up,
                InternalSpace.Instance.transform.rotation * Vector3.back);*/

            InitialRotation = InternalCamera.Instance.transform.rotation;
        }

        private void SetupEditorScene() {
            // use room-scale in editor
            TrackingSpace = ETrackingUniverseOrigin.TrackingUniverseStanding;

            // render KerbalVR objects on the default layer
            RenderLayer = 0;

            // generate list of cameras to render
            PopulateCameraList(EDITOR_SCENE_CAMERAS);

            // set inital scene position

            //Vector3 forwardDir = EditorCamera.Instance.transform.rotation * Vector3.forward;
            //forwardDir.y = 0f; // make the camera point straight forward
            //Vector3 startingPos = EditorCamera.Instance.transform.position;
            //startingPos.y = 0f; // start at ground level

            Vector3 startingPos = new Vector3(0f, 0f, -5f);

            InitialPosition = startingPos;
            InitialRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
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

            switch (HighLogic.LoadedScene) {
                case GameScenes.FLIGHT:
                    UpdateFlightScene(eye, hmdTransform, hmdEyeTransform);
                    break;

                case GameScenes.EDITOR:
                    UpdateEditorScene(eye, hmdTransform, hmdEyeTransform);
                    break;

                default:
                    throw new Exception("Cannot setup VR scene, current scene \"" +
                        HighLogic.LoadedScene + "\" is invalid.");
            }

            HmdPosition = CurrentPosition + CurrentRotation * hmdTransform.pos;
            HmdRotation = CurrentRotation * hmdTransform.rot;
        }

        private void UpdateFlightScene(
            EVREye eye,
            SteamVR_Utils.RigidTransform hmdTransform,
            SteamVR_Utils.RigidTransform hmdEyeTransform) {

            // in flight, don't allow movement of the origin point
            CurrentPosition = InitialPosition;
            CurrentRotation = InitialRotation;

            // get position of your eyeball
            Vector3 positionToHmd = hmdTransform.pos;
            Vector3 positionToEye = hmdTransform.pos + hmdTransform.rot * hmdEyeTransform.pos;

            // translate device space to Unity space, with world scaling
            Vector3 updatedPosition = DevicePoseToWorld(positionToEye);
            Quaternion updatedRotation = DevicePoseToWorld(hmdTransform.rot);

            // in flight, update the internal and flight cameras
            InternalCamera.Instance.transform.position = updatedPosition;
            InternalCamera.Instance.transform.rotation = updatedRotation;

            FlightCamera.fetch.transform.position = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
            FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);

            // store the eyeball position
            HmdEyePosition[(int)eye] = updatedPosition;
            HmdEyeRotation[(int)eye] = updatedRotation;
        }

        private void UpdateEditorScene(
            EVREye eye,
            SteamVR_Utils.RigidTransform hmdTransform,
            SteamVR_Utils.RigidTransform hmdEyeTransform) {

            // get position of your eyeball
            Vector3 positionToHmd = hmdTransform.pos;
            Vector3 positionToEye = hmdTransform.pos + hmdTransform.rot * hmdEyeTransform.pos;

            // translate device space to Unity space, with world scaling
            Vector3 updatedPosition = DevicePoseToWorld(positionToEye);
            Quaternion updatedRotation = DevicePoseToWorld(hmdTransform.rot);

            // update the editor camera position
            EditorCamera.Instance.transform.position = updatedPosition;
            EditorCamera.Instance.transform.rotation = updatedRotation;

            // store the eyeball position
            HmdEyePosition[(int)eye] = updatedPosition;
            HmdEyeRotation[(int)eye] = updatedRotation;
        }

        /// <summary>
        /// Resets game cameras back to their original settings
        /// </summary>
        public void CloseScene() {
            // reset cameras to their original settings
            if (VRCameras != null) {
                for (int i = 0; i < VRCameras.Length; i++) {
                    VRCameras[i].camera.targetTexture = null;
                    VRCameras[i].camera.projectionMatrix = VRCameras[i].originalProjectionMatrix;
                    VRCameras[i].camera.enabled = true;
                }
            }
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
                    float nearClipPlane = (foundCamera.name.Equals("Camera 01")) ? 0.05f : foundCamera.nearClipPlane;
                    HmdMatrix44_t projectionMatrixL = OpenVR.System.GetProjectionMatrix(EVREye.Eye_Left, nearClipPlane, foundCamera.farClipPlane);
                    HmdMatrix44_t projectionMatrixR = OpenVR.System.GetProjectionMatrix(EVREye.Eye_Right, nearClipPlane, foundCamera.farClipPlane);

                    // store information about the camera
                    VRCameras[i].camera = foundCamera;
                    VRCameras[i].originalProjectionMatrix = foundCamera.projectionMatrix;
                    VRCameras[i].hmdProjectionMatrixL = MathUtils.Matrix4x4_OpenVr2UnityFormat(ref projectionMatrixL);
                    VRCameras[i].hmdProjectionMatrixR = MathUtils.Matrix4x4_OpenVr2UnityFormat(ref projectionMatrixR);

                    // disable the camera so we can call Render directly
                    foundCamera.enabled = false;
                }
            }
        }

        public bool SceneAllowsVR() {
            bool allowed;
            switch (HighLogic.LoadedScene) {
                case GameScenes.FLIGHT:
                    allowed = (CameraManager.Instance != null) &&
                        (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA);
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
            return CurrentPosition + CurrentRotation *
                (devicePosition * inverseWorldScale[HighLogic.LoadedScene]);
        }

        /// <summary>
        /// Convert a device rotation to Unity world coordinates for this scene.
        /// </summary>
        /// <param name="deviceRotation">Device rotation in the device space coordinate system.</param>
        /// <returns>Unity world rotation corresponding to the device rotation.</returns>
        public Quaternion DevicePoseToWorld(Quaternion deviceRotation) {
            return CurrentRotation * deviceRotation;
        }

        public void OnManipulatorLeftUpdated(SteamVR_Controller.Device state) {
            // left touchpad
            if (state.GetPress(EVRButtonId.k_EButton_SteamVR_Touchpad)) {
                Vector2 touchAxis = state.GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad);

                Vector3 upDisplacement = Vector3.up *
                    (editorMovementSpeed * inverseWorldScale[HighLogic.LoadedScene] * touchAxis.y) * Time.deltaTime;

                Vector3 newPosition = CurrentPosition + upDisplacement;
                if (newPosition.y < 0f) newPosition.y = 0f;

                CurrentPosition = newPosition;
            }

            // left menu button
            if (state.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu)) {
                Core.ResetInitialHmdPosition();
            }

            // simulate mouse touch events with the trigger
            if (state.GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger)) {
                foreach (var obj in DeviceManager.Instance.ManipulatorLeft.FingertipCollidedGameObjects) {
                    obj.SendMessage("OnMouseDown");
                }
            }

            if (state.GetPressUp(EVRButtonId.k_EButton_SteamVR_Trigger)) {
                foreach (var obj in DeviceManager.Instance.ManipulatorLeft.FingertipCollidedGameObjects) {
                    obj.SendMessage("OnMouseUp");
                }
            }
        }

        public void OnManipulatorRightUpdated(SteamVR_Controller.Device state) {
            // right touchpad
            if (state.GetPress(EVRButtonId.k_EButton_SteamVR_Touchpad)) {
                Vector2 touchAxis = state.GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad);

                Vector3 fwdDirection = HmdRotation * Vector3.forward;
                fwdDirection.y = 0f; // allow only planar movement
                Vector3 fwdDisplacement = fwdDirection.normalized *
                    (editorMovementSpeed * inverseWorldScale[HighLogic.LoadedScene] * touchAxis.y) * Time.deltaTime;

                Vector3 rightDirection = HmdRotation * Vector3.right;
                rightDirection.y = 0f; // allow only planar movement
                Vector3 rightDisplacement = rightDirection.normalized *
                    (editorMovementSpeed * inverseWorldScale[HighLogic.LoadedScene] * touchAxis.x) * Time.deltaTime;

                CurrentPosition += fwdDisplacement + rightDisplacement;
            }

            // right menu button
            if (state.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu)) {
                Core.ResetInitialHmdPosition();
            }

            // simulate mouse touch events with the trigger
            if (state.GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger)) {
                foreach (var obj in DeviceManager.Instance.ManipulatorRight.FingertipCollidedGameObjects) {
                    obj.SendMessage("OnMouseDown");
                }
            }

            if (state.GetPressUp(EVRButtonId.k_EButton_SteamVR_Trigger)) {
                foreach (var obj in DeviceManager.Instance.ManipulatorRight.FingertipCollidedGameObjects) {
                    obj.SendMessage("OnMouseUp");
                }
            }
        }
    } // class Scene
} // namespace KerbalVR
