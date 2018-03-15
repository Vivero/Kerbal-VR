using System;
using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    public class Scene
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


        #region Properties

        // The list of cameras to render for the current scene.
        public static Types.CameraData[] VRCameras { get; private set; }
        public static int NumVRCameras { get; private set; }

        // The initial world position of the cameras for the current scene. This
        // position corresponds to the origin in the real world physical device
        // coordinate system.
        public static Vector3 InitialPosition { get; private set; }
        public static Quaternion InitialRotation { get; private set; }

        // The current world position of the cameras for the current scene. This
        // position corresponds to the origin in the real world physical device
        // coordinate system.
        public static Vector3 CurrentPosition { get; set; }
        public static Quaternion CurrentRotation { get; set; }

        // The current position of the HMD
        public static Vector3 HmdPosition { get; private set; }
        public static Quaternion HmdRotation { get; private set; }

        // defines the tracking method to use
        public static ETrackingUniverseOrigin TrackingSpace { get; private set; }

        // defines what layer to render KerbalVR objects on
        public static int RenderLayer { get; private set; }

        #endregion


        /// <summary>
        /// Set up the list of cameras to render for this scene and the initial position
        /// corresponding to the origin in the real world device coordinate system.
        /// </summary>
        public static void SetupScene() {
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

        private static void SetupFlightScene() {
            // use seated mode during IVA flight
            TrackingSpace = ETrackingUniverseOrigin.TrackingUniverseSeated;

            // render KerbalVR objects on the InternalSpace layer
            RenderLayer = 20;

            // generate list of cameras to render
            PopulateCameraList(FLIGHT_SCENE_CAMERAS);

            // set inital scene position
            InitialPosition = InternalCamera.Instance.transform.position;
            InitialRotation = InternalCamera.Instance.transform.rotation;
        }

        private static void SetupEditorScene() {
            // use room-scale in editor
            TrackingSpace = ETrackingUniverseOrigin.TrackingUniverseStanding;

            // render KerbalVR objects on the default layer
            RenderLayer = 0;

            // generate list of cameras to render
            PopulateCameraList(EDITOR_SCENE_CAMERAS);

            // set inital scene position
            Vector3 forwardDir = EditorCamera.Instance.transform.rotation * Vector3.forward;
            forwardDir.y = 0f; // make the camera point straight forward

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
        public static void UpdateScene(
            SteamVR_Utils.RigidTransform hmdTransform,
            SteamVR_Utils.RigidTransform hmdEyeTransform) {

            switch (HighLogic.LoadedScene) {
                case GameScenes.FLIGHT:
                    UpdateFlightScene(hmdTransform, hmdEyeTransform);
                    break;

                case GameScenes.EDITOR:
                    UpdateEditorScene(hmdTransform, hmdEyeTransform);
                    break;

                default:
                    throw new Exception("Cannot setup VR scene, current scene \"" +
                        HighLogic.LoadedScene + "\" is invalid.");
            }

            HmdPosition = InitialPosition + InitialRotation * hmdTransform.pos;
            HmdRotation = InitialRotation * hmdTransform.rot;
        }

        private static void UpdateFlightScene(
            SteamVR_Utils.RigidTransform hmdTransform,
            SteamVR_Utils.RigidTransform hmdEyeTransform) {

            CurrentPosition = InitialPosition;
            CurrentRotation = InitialRotation;

            Vector3 positionToHmd = hmdTransform.pos;
            Vector3 positionToEye = hmdTransform.pos + hmdTransform.rot * hmdEyeTransform.pos;

            Vector3 updatedPosition = CurrentPosition + CurrentRotation * positionToEye;
            Quaternion updatedRotation = CurrentRotation * hmdTransform.rot;

            InternalCamera.Instance.transform.position = updatedPosition;
            InternalCamera.Instance.transform.rotation = updatedRotation;

            FlightCamera.fetch.transform.position = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
            FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);
        }

        private static void UpdateEditorScene(
            SteamVR_Utils.RigidTransform hmdTransform,
            SteamVR_Utils.RigidTransform hmdEyeTransform) {

            Vector3 positionToHmd = hmdTransform.pos;
            Vector3 positionToEye = hmdTransform.pos + hmdTransform.rot * hmdEyeTransform.pos;

            Vector3 updatedPosition = CurrentPosition + CurrentRotation * positionToEye;
            Quaternion updatedRotation = CurrentRotation * hmdTransform.rot;

            EditorCamera.Instance.transform.position = updatedPosition;
            EditorCamera.Instance.transform.rotation = updatedRotation;
        }

        /// <summary>
        /// Resets game cameras back to their original settings
        /// </summary>
        public static void CloseScene() {
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
        private static void PopulateCameraList(string[] cameraNames) {
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

        public static bool SceneAllowsVR() {
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
        public static Vector3 DevicePoseToWorld(Vector3 devicePosition) {
            return CurrentPosition + CurrentRotation * devicePosition;
        }

        /// <summary>
        /// Convert a device rotation to Unity world coordinates for this scene.
        /// </summary>
        /// <param name="deviceRotation">Device rotation in the device space coordinate system.</param>
        /// <returns>Unity world rotation corresponding to the device rotation.</returns>
        public static Quaternion DevicePoseToWorld(Quaternion deviceRotation) {
            return CurrentRotation * deviceRotation;
        }
    }
}
