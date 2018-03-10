using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    // Start plugin on entering the Flight scene
    //
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KerbalVR_Plugin : MonoBehaviour
    {
        // this function allows importing DLLs from a given path
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);

        private bool hmdIsInitialized = false;
        private bool hmdIsAllowed = false;
        private bool hmdIsEnabled = false;
        private bool hmdIsRunning = false;
        private bool hmdIsRunningPrev = false;

        private CVRSystem vrSystem;
        private CVRCompositor vrCompositor;
        private TrackedDevicePose_t[] vrDevicePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private TrackedDevicePose_t[] vrRenderPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private TrackedDevicePose_t[] vrGamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        
        private Texture_t hmdLeftEyeTexture, hmdRightEyeTexture;
        private VRTextureBounds_t hmdTextureBounds;
        private RenderTexture hmdLeftEyeRenderTexture, hmdRightEyeRenderTexture;
        

        // list of all cameras in the game
        //--------------------------------------------------------------
        private string[] cameraNames = 
        {
            "GalaxyCamera",
            "Camera ScaledSpace",
            "Camera 01",
            "Camera 00",
            "InternalCamera",
            //"Canvas Camera",
            //"FXCamera",
            //"UIMainCamera",
            //"UIVectorCamera",
            //"Camera",
            //"velocity camera",
        };
        
        // struct to keep track of Camera properties
        private struct CameraProperties
        {
            public Camera camera;
            public Matrix4x4 originalProjMatrix;
            public Matrix4x4 hmdLeftProjMatrix;
            public Matrix4x4 hmdRightProjMatrix;

            public CameraProperties(Camera camera, Matrix4x4 originalProjMatrix, Matrix4x4 hmdLeftProjMatrix, Matrix4x4 hmdRightProjMatrix) {
                this.camera = camera;
                this.originalProjMatrix = originalProjMatrix;
                this.hmdLeftProjMatrix = hmdLeftProjMatrix;
                this.hmdRightProjMatrix = hmdRightProjMatrix;
            }
        }

        // list of cameras to render (Camera objects)
        private List<CameraProperties> camerasToRender;

        /// <summary>
        /// Overrides the Start method for a MonoBehaviour plugin.
        /// </summary>
        public void Start() {
            Utils.LogInfo("KerbalVrPlugin started.");

            // add an event triggered when game scene changes, to handle
            // shutting off the HMD outside of Flight scene
            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);

            // initialize the OpenVR API
            InitHMD();

            // don't destroy this object when switching scenes
            // TODO: investigate if we really want this behavior
            DontDestroyOnLoad(this);
        }

        /// <summary>
        /// Overrides the Update method, called every frame.
        /// </summary>
        public void LateUpdate() {

            // start HMD using the Y key
            if (Input.GetKeyDown(KeyCode.Y)) {
                hmdIsEnabled = !hmdIsEnabled;
                Utils.LogInfo("HMD enabled: " + hmdIsEnabled);

                if (hmdIsEnabled) {
                    InitVRScene();
                    ResetInitialHmdPosition();
                }
            }

            // do nothing unless we are in IVA
            hmdIsAllowed = HighLogic.LoadedSceneIsFlight &&
                (CameraManager.Instance != null) &&
                (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA);

            // check we are running the HMD
            hmdIsRunning = hmdIsAllowed && hmdIsInitialized && hmdIsEnabled;

            // perform regular updates if HMD is initialized
            if (hmdIsRunning) {
                EVRCompositorError vrCompositorError = EVRCompositorError.None;

                try {

                    // TODO: investigate if we should really be capturing poses in LateUpdate

                    // get latest HMD pose
                    //--------------------------------------------------------------
                    vrSystem.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseSeated, 0.0f, vrDevicePoses);
                    HmdMatrix34_t vrLeftEyeTransform = vrSystem.GetEyeToHeadTransform(EVREye.Eye_Left);
                    HmdMatrix34_t vrRightEyeTransform = vrSystem.GetEyeToHeadTransform(EVREye.Eye_Right);
                    vrCompositorError = vrCompositor.WaitGetPoses(vrRenderPoses, vrGamePoses);

                    if (vrCompositorError != EVRCompositorError.None) {
                        throw new Exception("WaitGetPoses failed: (" + (int)vrCompositorError + ") " + vrCompositorError.ToString());
                    }

                    // convert SteamVR poses to Unity coordinates
                    var hmdTransform = new SteamVR_Utils.RigidTransform(vrDevicePoses[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking);
                    var hmdLeftEyeTransform = new SteamVR_Utils.RigidTransform(vrLeftEyeTransform);
                    var hmdRightEyeTransform = new SteamVR_Utils.RigidTransform(vrRightEyeTransform);

                    // Render the LEFT eye
                    RenderHmdCameras(
                        EVREye.Eye_Left,
                        hmdTransform,
                        hmdLeftEyeTransform,
                        hmdLeftEyeRenderTexture,
                        hmdLeftEyeTexture);

                    // Render the RIGHT eye
                    RenderHmdCameras(
                        EVREye.Eye_Right,
                        hmdTransform,
                        hmdRightEyeTransform,
                        hmdRightEyeRenderTexture,
                        hmdRightEyeTexture);

                    vrCompositor.PostPresentHandoff();

                } catch (Exception e) {
                    Utils.LogError(e);
                    hmdIsEnabled = false;
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
                foreach (CameraProperties camStruct in camerasToRender) {
                    camStruct.camera.targetTexture = null;
                    camStruct.camera.projectionMatrix = camStruct.originalProjMatrix;
                }
            }

            hmdIsRunningPrev = hmdIsRunning;
        }
        
        /// <summary>
        /// Overrides the OnDestroy method, called when plugin is destroyed (leaving Flight scene).
        /// </summary>
        public void OnDestroy() {
            Utils.LogInfo("KerbalVrPlugin OnDestroy");
            CloseHMD();
        }

        /// <summary>
        /// An event called when the game is switching scenes.
        /// </summary>
        /// <param name="data">The scene being switched into.</param>
        public void OnGameSceneLoadRequested(GameScenes data) {
            CloseHMD();
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
            SetDllDirectory(Utils.OpenVRDllPath);

            // check if HMD is connected on the system
            retVal = OpenVR.IsHmdPresent();
            if (!retVal) {
                Utils.LogError("HMD not found on this system.");
                return retVal;
            }

            // check if SteamVR runtime is installed.
            // For this plugin, MAKE SURE IT IS ALREADY RUNNING.
            retVal = OpenVR.IsRuntimeInstalled();
            if (!retVal) {
                Utils.LogError("SteamVR runtime not found on this system.");
                return retVal;
            }

            // initialize HMD
            EVRInitError hmdInitErrorCode = EVRInitError.None;
            vrSystem = OpenVR.Init(ref hmdInitErrorCode, EVRApplicationType.VRApplication_Scene);

            // return if failure
            retVal = (hmdInitErrorCode == EVRInitError.None);
            if (!retVal) {
                Utils.LogError("Failed to initialize HMD. Init returned: " + OpenVR.GetStringForHmdError(hmdInitErrorCode));
                return retVal;
            }

            // reset "seated position" and capture initial position. this means you should hold the HMD in
            // the position you would like to consider "seated", before running this code.

            ResetInitialHmdPosition();

            // initialize Compositor
            vrCompositor = OpenVR.Compositor;

            // initialize render textures (for displaying on HMD)
            uint renderTextureWidth = 0;
            uint renderTextureHeight = 0;
            vrSystem.GetRecommendedRenderTargetSize(ref renderTextureWidth, ref renderTextureHeight);

            hmdLeftEyeRenderTexture = new RenderTexture((int)renderTextureWidth, (int)renderTextureHeight, 24, RenderTextureFormat.ARGB32);
            hmdLeftEyeRenderTexture.Create();

            hmdRightEyeRenderTexture = new RenderTexture((int)renderTextureWidth, (int)renderTextureHeight, 24, RenderTextureFormat.ARGB32);
            hmdRightEyeRenderTexture.Create();

            hmdLeftEyeTexture.handle = hmdLeftEyeRenderTexture.GetNativeTexturePtr();
            hmdLeftEyeTexture.eColorSpace = EColorSpace.Auto;

            hmdRightEyeTexture.handle = hmdRightEyeRenderTexture.GetNativeTexturePtr();
            hmdRightEyeTexture.eColorSpace = EColorSpace.Auto;


            switch (SystemInfo.graphicsDeviceType) {
                case UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore:
                case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2:
                case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3:
                    hmdLeftEyeTexture.eType = ETextureType.OpenGL;
                    hmdRightEyeTexture.eType = ETextureType.OpenGL;
                    break; // doesn't work
                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D9:
                    throw (new Exception("DirectX 9 not supported"));
                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D11:
                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D12:
                    hmdLeftEyeTexture.eType = ETextureType.DirectX;
                    hmdRightEyeTexture.eType = ETextureType.DirectX;
                    break;
                default:
                    throw (new Exception(SystemInfo.graphicsDeviceType.ToString() + " not supported"));
            }

            // Set rendering bounds on texture to render?
            // I assume min=0.0 and max=1.0 renders to the full extent of the texture
            hmdTextureBounds.uMin = 0.0f;
            hmdTextureBounds.uMax = 1.0f;
            hmdTextureBounds.vMin = 1.0f; // flip the vertical coordinate for some reason
            hmdTextureBounds.vMax = 0.0f;

            // TODO: Need to understand better how to create render targets and incorporate hidden area mask mesh
            
            hmdIsInitialized = true;

            return retVal;
        }

        private void InitVRScene() {
            /*foreach (Camera camera in Camera.allCameras) {
                Utils.LogInfo("KSP Camera: " + camera.name);
            }*/

            // search for camera objects to render
            camerasToRender = new List<CameraProperties>(cameraNames.Length);
            foreach (string cameraName in cameraNames) {
                foreach (Camera camera in Camera.allCameras) {
                    if (cameraName.Equals(camera.name)) {
                        float nearClipPlane = (camera.name.Equals("Camera 01")) ? 0.05f : camera.nearClipPlane;

                        HmdMatrix44_t projLeft = vrSystem.GetProjectionMatrix(EVREye.Eye_Left, nearClipPlane, camera.farClipPlane);
                        HmdMatrix44_t projRight = vrSystem.GetProjectionMatrix(EVREye.Eye_Right, nearClipPlane, camera.farClipPlane);
                        camerasToRender.Add(new CameraProperties(camera, camera.projectionMatrix, MathUtils.Matrix4x4_OpenVr2UnityFormat(ref projLeft), MathUtils.Matrix4x4_OpenVr2UnityFormat(ref projRight)));
                        break;
                    }
                }
            }
        }

        private void RenderHmdCameras(
            EVREye eye,
            SteamVR_Utils.RigidTransform hmdTransform,
            SteamVR_Utils.RigidTransform hmdEyeTransform,
            RenderTexture hmdEyeRenderTexture,
            Texture_t hmdEyeTexture) {

            // rotate camera according to the HMD orientation
            InternalCamera.Instance.transform.localRotation = hmdTransform.rot;

            // translate the camera to match the position of the left eye, from origin
            InternalCamera.Instance.transform.localPosition = new Vector3(0f, 0f, 0f);
            InternalCamera.Instance.transform.Translate(hmdEyeTransform.pos);

            // translate the camera to match the position of the HMD
            InternalCamera.Instance.transform.localPosition += hmdTransform.pos;

            // move the FlightCamera to match the position of the InternalCamera (so the outside world moves accordingly)
            FlightCamera.fetch.transform.position = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
            FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);

            // render the set of cameras
            foreach (CameraProperties camStruct in camerasToRender) {
                // set projection matrix
                camStruct.camera.projectionMatrix = (eye == EVREye.Eye_Left) ?
                    camStruct.hmdLeftProjMatrix : camStruct.hmdRightProjMatrix;

                // set texture to render to
                camStruct.camera.targetTexture = hmdEyeRenderTexture;

                // render camera
                camStruct.camera.Render();
            }

            // Submit frames to HMD
            EVRCompositorError vrCompositorError = vrCompositor.Submit(eye, ref hmdEyeTexture, ref hmdTextureBounds, EVRSubmitFlags.Submit_Default);
            if (vrCompositorError != EVRCompositorError.None) {
                throw new Exception("Submit (" + eye + ") failed: (" + (int)vrCompositorError + ") " + vrCompositorError.ToString());
            }
        }

        /// <summary>
        /// Sets the current real-world position of the HMD as the seated origin in IVA.
        /// </summary>
        private void ResetInitialHmdPosition() {
            if (hmdIsInitialized) {
                vrSystem.ResetSeatedZeroPose();
            }
        }

        private void CloseHMD() {
            hmdIsEnabled = false;
            OpenVR.Shutdown();
            hmdIsInitialized = false;
        }

    } // class KerbalVR_Plugin
} // namespace KerbalVR
