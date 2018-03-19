using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    // Start plugin on entering the Flight scene
    //
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class KerbalVR_Plugin : MonoBehaviour
    {
        private uint controllerDeviceIndexL = 0;
        private uint controllerDeviceIndexR = 0;
        private VRControllerState_t controllerDeviceStateL = new VRControllerState_t();
        private VRControllerState_t controllerDeviceStateR = new VRControllerState_t();

        // this function allows importing DLLs from a given path
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);

        private bool hmdIsInitialized = false;
        private bool hmdIsRunning = false;
        private bool hmdIsRunningPrev = false;

        // enable and disable VR functionality
        private bool _hmdIsEnabled;
        public bool HmdIsEnabled {
            get { return _hmdIsEnabled; }
            set {
                _hmdIsEnabled = value;

                if (_hmdIsEnabled) {
                    InitVRScene();
                    ResetInitialHmdPosition();
                }
            }
        }

        // check if VR can be enabled
        public bool HmdIsAllowed { get; private set; }

        private CVRSystem vrSystem;
        private CVRCompositor vrCompositor;
        private TrackedDevicePose_t[] vrDevicePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private TrackedDevicePose_t[] vrRenderPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private TrackedDevicePose_t[] vrGamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

        private uint _renderTargetSizeW;
        public uint RenderTargetSizeW {
            get { return _renderTargetSizeW; }
            private set { _renderTargetSizeW = value; }
        }

        private uint _renderTargetSizeH;
        public uint RenderTargetSizeH {
            get { return _renderTargetSizeH; }
            private set { _renderTargetSizeH = value; }
        }

        private float renderTargetAspect;

        private Texture_t hmdLeftEyeTexture, hmdRightEyeTexture;
        private VRTextureBounds_t hmdTextureBounds;
        private RenderTexture hmdLeftEyeRenderTexture, hmdRightEyeRenderTexture;

        private KerbalVR_GUI gui;

        private Vector3 ivaInitialPosition;
        private Quaternion ivaInitialRotation;

        // create a camera for the sole purpose of rendering the Hidden Area Masks
        private GameObject hamCameraGameObject;
        private Camera hamCamera;
        private static readonly string HAM_CAMERA_NAME = "VR_HAM_Camera";
        private static readonly float HAM_CAMERA_NEAR_Z = 0.01f;
        private static readonly float HAM_CAMERA_FAR_Z = 0.02f;
        private static readonly int HAM_LAYER = 27;
        // layer 27 is "WheelColliders". use it to render the hidden area masks.

        // these arrays each hold one object for the corresponding eye, where
        // index 0 = Left_Eye, index 1 = Right_Eye

        // Hidden Area Mask objects. These objects mask off a portion of the
        // render targets to increase VR performance.
        private GameObject[] hamGameObject = new GameObject[2];
        private Mesh[] hamMesh = new Mesh[2];
        
        // DEBUG
        RenderTexture hamCameraRenderTexture;
        GameObject ctrlGizmoL, hamGizmo;
        GameObject previewWindow;
        Texture2D previewWindowTexture;


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

        // list of cameras to render (Camera objects)
        private Utils.CameraData[] camerasToRender;
        private int numCamerasToRender;

        public void Awake() {
            Utils.LogInfo("KerbalVrPlugin started.");

            // init objects
            gui = new KerbalVR_GUI(this);
            _hmdIsEnabled = false;
            HmdIsAllowed = false;
        }

        /// <summary>
        /// Overrides the Start method for a MonoBehaviour plugin.
        /// </summary>
        public void Start() {
            // add an event triggered when game scene changes, to handle
            // shutting off the HMD outside of Flight scene
            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);

            // initialize the OpenVR API
            bool success = InitHMD();
            if (!success) {
                Utils.LogError("Unable to initialize VR headset!");
            }

            // don't destroy this object when switching scenes
            DontDestroyOnLoad(this);

            // when ready for a GUI, load it
            GameEvents.onGUIApplicationLauncherReady.Add(gui.OnAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(gui.OnAppLauncherDestroyed);
        }

        /// <summary>
        /// Overrides the LateUpdate method, called every frame.
        /// </summary>
        public void LateUpdate() {
            // do nothing unless we are in IVA
            HmdIsAllowed = HighLogic.LoadedSceneIsFlight &&
                (CameraManager.Instance != null) &&
                (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA);

            // check if we are running the HMD
            hmdIsRunning = HmdIsAllowed && hmdIsInitialized && HmdIsEnabled;

            // perform regular updates if HMD is initialized
            if (hmdIsRunning) {
                EVRCompositorError vrCompositorError = EVRCompositorError.None;

                try {
                    // TODO: investigate if we should really be capturing poses in LateUpdate

                    // detect controllers
                    //--------------------------------------------------------------
                    for (uint idx = 0; idx < OpenVR.k_unMaxTrackedDeviceCount; idx++) {
                        if ((controllerDeviceIndexL == 0) && (vrSystem.GetTrackedDeviceClass(idx) == ETrackedDeviceClass.Controller)) {
                            controllerDeviceIndexL = idx;
                        } else if ((controllerDeviceIndexR == 0) && (vrSystem.GetTrackedDeviceClass(idx) == ETrackedDeviceClass.Controller)) {
                            controllerDeviceIndexR = idx;
                        }
                    }

                    // get latest HMD pose
                    //--------------------------------------------------------------
                    float secondsToPhotons = CalculatePredictedSecondsToPhotons();
                    vrSystem.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseSeated, secondsToPhotons, vrDevicePoses);
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

                    if (controllerDeviceIndexL > 0) {
                        SteamVR_Utils.RigidTransform ctrlPoseLeft = new SteamVR_Utils.RigidTransform(vrDevicePoses[controllerDeviceIndexL].mDeviceToAbsoluteTracking);

                        // Controller Gizmo
                        if (ctrlGizmoL != null) {
                            ctrlGizmoL.transform.position = ivaInitialPosition + ivaInitialRotation * ctrlPoseLeft.pos;
                            ctrlGizmoL.transform.rotation = ivaInitialRotation * ctrlPoseLeft.rot;
                        }

                        // VR Mask Camera
                        if (hamCameraGameObject != null) {
                            hamCameraGameObject.transform.position = ivaInitialPosition + ivaInitialRotation * ctrlPoseLeft.pos;
                            hamCameraGameObject.transform.rotation = ivaInitialRotation * ctrlPoseLeft.rot;
                        }

                        // Preview Window
                        if (previewWindow != null) {
                            Vector3 previewWindowPos = ctrlPoseLeft.pos + ctrlPoseLeft.rot * new Vector3(0f, 0.15f, 0.2f);
                            previewWindow.transform.position = ivaInitialPosition + ivaInitialRotation * previewWindowPos;
                            previewWindow.transform.rotation = ivaInitialRotation * ctrlPoseLeft.rot;
                        }
                    }

                    // TODO: render HAM
                    //hamGameObject[0].SetActive(true);
                    hamCamera.targetTexture = hamCameraRenderTexture;
                    hamCamera.Render();
                    //hamGameObject[0].SetActive(false);

                    
                    hamGizmo.transform.position = hamGameObject[0].transform.position;
                    hamGizmo.transform.rotation = hamGameObject[0].transform.rotation;


                    // Render the LEFT eye
                    //--------------------------------------------------------------
                    RenderHmdCameras(
                        EVREye.Eye_Left,
                        hmdTransform,
                        hmdLeftEyeTransform,
                        hmdLeftEyeRenderTexture,
                        hmdLeftEyeTexture);

                    // Render the RIGHT eye
                    //--------------------------------------------------------------
                    RenderHmdCameras(
                        EVREye.Eye_Right,
                        hmdTransform,
                        hmdRightEyeTransform,
                        hmdRightEyeRenderTexture,
                        hmdRightEyeTexture);
                    
                    vrCompositor.PostPresentHandoff();



                    // DEBUG
                    RenderTexture.active = hamCameraRenderTexture;
                    previewWindowTexture.ReadPixels(
                        new Rect(0, 0, hamCameraRenderTexture.width, hamCameraRenderTexture.height),
                        0, 0);
                    previewWindowTexture.Apply();



                } catch (Exception e) {
                    Utils.LogError(e);
                    HmdIsEnabled = false;
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
                CloseVRScene();
            }
            

            hmdIsRunningPrev = hmdIsRunning;
        }

        private void RenderHmdCameras(
            EVREye eye,
            SteamVR_Utils.RigidTransform hmdTransform,
            SteamVR_Utils.RigidTransform hmdEyeTransform,
            RenderTexture hmdEyeRenderTexture,
            Texture_t hmdEyeTexture) {

            /**
             * hmdEyeTransform is in a coordinate system that follows the headset, where
             * the origin is the headset device position. Therefore the eyes are at a constant
             * offset from the device. hmdEyeTransform does not change (per eye).
             *      hmdEyeTransform.x+  towards the right of the headset
             *      hmdEyeTransform.y+  towards the top the headset
             *      hmdEyeTransform.z+  towards the front of the headset
             *
             * hmdTransform is in a coordinate system set in physical space, where the
             * origin is the initial seated position.
             *      hmdTransform.x+     towards the right
             *      hmdTransform.y+     towards the sky
             *      hmdTransform.z+     towards the front
             *
             *  ivaInitialPosition and ivaInitialRotation are the Unity world coordinates where
             *  we initialize the VR scene, i.e. the origin of a coordinate system that maps
             *  1-to-1 with physical space.
             *
             *  1. Calculate the position of the eye in the physical coordinate system.
             *  2. Transform the calculated position into Unity world coordinates, offset from
             *     ivaInitialPosition and ivaInitialRotation.
             */

            // position of the eye in the VR reference frame
            Vector3 positionToEye = hmdTransform.pos + hmdTransform.rot * hmdEyeTransform.pos;

            // position of the eye in the IVA reference frame
            InternalCamera.Instance.transform.position = ivaInitialPosition + ivaInitialRotation * positionToEye;
            InternalCamera.Instance.transform.rotation = ivaInitialRotation * hmdTransform.rot;

            // move the FlightCamera to match the position of the InternalCamera (so the outside world moves accordingly)
            FlightCamera.fetch.transform.position = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
            FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);

            // render the set of cameras
            for (int i = 0; i < numCamerasToRender; i++) {
                Utils.CameraData camData = camerasToRender[i];

                // set projection matrix
                camData.camera.projectionMatrix = (eye == EVREye.Eye_Left) ?
                    camData.hmdLeftProjMatrix : camData.hmdRightProjMatrix;

                // set texture to render to, then render
                camData.camera.targetTexture = hmdEyeRenderTexture;
                camData.camera.Render();


                if (eye == EVREye.Eye_Left && i == 4) {
                    camData.camera.targetTexture = hamCameraRenderTexture;
                    camData.camera.Render();
                }
            }

            // Submit frames to HMD
            EVRCompositorError vrCompositorError = vrCompositor.Submit(eye, ref hmdEyeTexture, ref hmdTextureBounds, EVRSubmitFlags.Submit_Default);
            if (vrCompositorError != EVRCompositorError.None) {
                throw new Exception("Submit (" + eye + ") failed: (" + (int)vrCompositorError + ") " + vrCompositorError.ToString());
            }
        }

        /// <summary>
        /// Overrides the OnDestroy method, called when plugin is destroyed (leaving Flight scene).
        /// </summary>
        public void OnDestroy() {
            Utils.LogInfo("KerbalVrPlugin OnDestroy");
            CloseHMD();
        }

        /// <summary>
        /// An event called when the game is switching scenes. The VR headset should be disabled.
        /// </summary>
        /// <param name="scene">The scene being switched into.</param>
        public void OnGameSceneLoadRequested(GameScenes scene) {
            HmdIsEnabled = false;
        }

        public void OnGUI() {
            gui.OnGUI();
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

            // check if SteamVR runtime is installed
            retVal = OpenVR.IsRuntimeInstalled();
            if (!retVal) {
                Utils.LogError("SteamVR runtime not found on this system.");
                return retVal;
            }

            // initialize HMD
            EVRInitError hmdInitErrorCode = EVRInitError.None;
            vrSystem = OpenVR.Init(ref hmdInitErrorCode, EVRApplicationType.VRApplication_Scene);
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
            vrSystem.GetRecommendedRenderTargetSize(ref _renderTargetSizeW, ref _renderTargetSizeH);
            renderTargetAspect = (float)_renderTargetSizeW / _renderTargetSizeH;
            Utils.LogInfo("Render Target Size: " + _renderTargetSizeW + " x " + _renderTargetSizeH);

            hmdLeftEyeRenderTexture = new RenderTexture((int)RenderTargetSizeW, (int)RenderTargetSizeH, 24, RenderTextureFormat.ARGB32);
            hmdLeftEyeRenderTexture.Create();

            hmdRightEyeRenderTexture = new RenderTexture((int)RenderTargetSizeW, (int)RenderTargetSizeH, 24, RenderTextureFormat.ARGB32);
            hmdRightEyeRenderTexture.Create();

            hmdLeftEyeTexture.handle = hmdLeftEyeRenderTexture.GetNativeTexturePtr();
            hmdLeftEyeTexture.eColorSpace = EColorSpace.Auto;

            hmdRightEyeTexture.handle = hmdRightEyeRenderTexture.GetNativeTexturePtr();
            hmdRightEyeTexture.eColorSpace = EColorSpace.Auto;

            // at the moment, only Direct3D12 is working with Kerbal Space Program
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

            // set rendering bounds on texture to render
            hmdTextureBounds.uMin = 0.0f;
            hmdTextureBounds.uMax = 1.0f;
            hmdTextureBounds.vMin = 1.0f; // flip the vertical coordinate for some reason
            hmdTextureBounds.vMax = 0.0f;

            // create hidden area mask meshes
            for (int i = 0; i < 2; i++) {
                EVREye eye = (EVREye)i;
                HiddenAreaMesh_t hiddenAreaMesh = OpenVR.System.GetHiddenAreaMesh(
                    eye, EHiddenAreaMeshType.k_eHiddenAreaMesh_Standard);
                hamMesh[i] = Utils.CreateHiddenAreaMesh(hiddenAreaMesh, hmdTextureBounds, eye == EVREye.Eye_Right);
            }

            hmdIsInitialized = true;

            // DEBUG
            for (int i = 0; i < 32; i++) {
                Utils.LogInfo("Layer " + i + ": " + LayerMask.LayerToName(i));
            }

            return retVal;
        }

        private void InitVRScene() {
            /*foreach (Camera camera in Camera.allCameras) {
                Utils.LogInfo("KSP Camera: " + camera.name);
            }*/

            // create the hidden area mask camera
            if (hamCameraGameObject == null) {
                hamCameraGameObject = new GameObject(HAM_CAMERA_NAME);
                hamCamera = hamCameraGameObject.AddComponent<Camera>();

                // DEBUG
                hamCameraRenderTexture = new RenderTexture((int)RenderTargetSizeW, (int)RenderTargetSizeH, 8, RenderTextureFormat.ARGB32);

                hamCamera.clearFlags = CameraClearFlags.SolidColor;
                hamCamera.backgroundColor = Color.red;
                hamCamera.cullingMask = 1 << HAM_LAYER;
                hamCamera.orthographic = true;
                hamCamera.orthographicSize = 1f;
                hamCamera.nearClipPlane = HAM_CAMERA_NEAR_Z;
                hamCamera.farClipPlane = HAM_CAMERA_FAR_Z;
                hamCamera.depth = 10f;
                hamCamera.enabled = false;
                
                /*Utils.CreateGizmoAtPosition(
                    InternalSpace.InternalToWorld(Vector3.zero),
                    InternalSpace.InternalToWorld(Quaternion.identity));*/
            }

            // create the hidden area mask objects
            for (int i = 0; i < 2; i++) {
                if (hamGameObject[i] == null) {
                    hamGameObject[i] = CreateHiddenAreaMaskGameObject((EVREye)i);

                    hamGameObject[i].transform.SetParent(hamCameraGameObject.transform);
                    hamGameObject[i].transform.localPosition = new Vector3(
                        0f, 0f, (HAM_CAMERA_NEAR_Z + HAM_CAMERA_FAR_Z) * 0.5f);
                    hamGameObject[i].SetActive(false);
                }
            }

            // DEBUG
            hamGameObject[0].SetActive(true);
            hamGizmo = CreateMeshGizmo(hamGameObject[0]);

            // search for the cameras to render
            numCamerasToRender = cameraNames.Length;
            camerasToRender = new Utils.CameraData[numCamerasToRender];
            for (int i = 0; i < cameraNames.Length; i++) {

                Camera foundCamera = Array.Find(Camera.allCameras, cam => cam.name.Equals(cameraNames[i]));
                if (foundCamera == null) {
                    Utils.LogError("Could not find camera \"" + cameraNames[i] + "\" in the scene!");

                } else {
                    // determine clip plane and new projection matrices
                    float nearClipPlane = (foundCamera.name.Equals("Camera 01")) ? 0.05f : foundCamera.nearClipPlane;
                    HmdMatrix44_t projLeft = OpenVR.System.GetProjectionMatrix(EVREye.Eye_Left, nearClipPlane, foundCamera.farClipPlane);
                    HmdMatrix44_t projRight = OpenVR.System.GetProjectionMatrix(EVREye.Eye_Right, nearClipPlane, foundCamera.farClipPlane);

                    // store information about the camera
                    camerasToRender[i] = new Utils.CameraData(
                        foundCamera,
                        foundCamera.projectionMatrix,
                        MathUtils.Matrix4x4_OpenVr2UnityFormat(ref projLeft),
                        MathUtils.Matrix4x4_OpenVr2UnityFormat(ref projRight));

                    // disable the camera so we can call Render directly
                    foundCamera.enabled = false;

                    Utils.LogInfo("Camera \"" + foundCamera.name + "\" depth: " + foundCamera.depth);
                }
                
            }

            // capture the initial viewpoint position
            ivaInitialPosition = InternalCamera.Instance.transform.position;
            ivaInitialRotation = InternalCamera.Instance.transform.rotation;
            
            

            if (ctrlGizmoL == null) {
                ctrlGizmoL = Utils.CreateGizmo();
            }

            // DEBUG
            if (previewWindow == null) {
                previewWindow = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(previewWindow.GetComponent<MeshCollider>());
                previewWindow.transform.localScale = Vector3.one * 0.3f;
                previewWindow.layer = 20;
                Shader testWindowShader = ShaderDatabase.GetShader("KerbalVR/UnlitTexture");
                if (testWindowShader == null) {
                    Utils.LogInfo("Missing shader Unlit/Texture!");
                    testWindowShader = Shader.Find("Standard");
                }
                Material testWindowMaterial = new Material(testWindowShader);
                previewWindowTexture = new Texture2D((int)RenderTargetSizeW, (int)RenderTargetSizeH);
                previewWindowTexture.wrapMode = TextureWrapMode.Clamp;
                testWindowMaterial.mainTexture = previewWindowTexture;
                previewWindow.GetComponent<MeshRenderer>().sharedMaterial = testWindowMaterial;
            }
            
        }

        public void CloseVRScene() {
            Utils.LogInfo("HMD is now off, closing VR scene...");

            // reset cameras to original settings
            foreach (Utils.CameraData camData in camerasToRender) {
                camData.camera.targetTexture = null;
                camData.camera.projectionMatrix = camData.originalProjMatrix;
                camData.camera.enabled = true;
            }
        }

        /// <summary>
        /// Sets the current real-world position of the HMD as the seated origin in IVA.
        /// </summary>
        public void ResetInitialHmdPosition() {
            if (hmdIsInitialized) {
                vrSystem.ResetSeatedZeroPose();
            }
        }

        public void CloseHMD() {
            HmdIsEnabled = false;
            OpenVR.Shutdown();
            hmdIsInitialized = false;
        }

        public float CalculatePredictedSecondsToPhotons() {
            ETrackedPropertyError propertyError = ETrackedPropertyError.TrackedProp_Success;

            float secondsSinceLastVsync = 0f;
            ulong frameCounter = 0;
            vrSystem.GetTimeSinceLastVsync(ref secondsSinceLastVsync, ref frameCounter);

            float displayFrequency = vrSystem.GetFloatTrackedDeviceProperty(
                OpenVR.k_unTrackedDeviceIndex_Hmd,
                ETrackedDeviceProperty.Prop_DisplayFrequency_Float,
                ref propertyError);
            if (propertyError != ETrackedPropertyError.TrackedProp_Success) {
                throw new Exception("Failed to obtain Prop_DisplayFrequency_Float: (" +
                    (int)propertyError + ") " + propertyError.ToString());
            }

            float vsyncToPhotons = vrSystem.GetFloatTrackedDeviceProperty(
                OpenVR.k_unTrackedDeviceIndex_Hmd,
                ETrackedDeviceProperty.Prop_SecondsFromVsyncToPhotons_Float,
                ref propertyError);
            if (propertyError != ETrackedPropertyError.TrackedProp_Success) {
                throw new Exception("Failed to obtain Prop_SecondsFromVsyncToPhotons_Float: (" +
                    (int)propertyError + ") " + propertyError.ToString());
            }

            float frameDuration = 1f / displayFrequency;
            return frameDuration - secondsSinceLastVsync + vsyncToPhotons;
        }

        private GameObject CreateHiddenAreaMaskGameObject(EVREye eye) {
            // try to find existing object
            string hamGameObjectName = "VR_HAM_" + eye;
            GameObject hamGameObject = GameObject.Find(hamGameObjectName);

            if (hamGameObject == null) {
                // create new object
                hamGameObject = new GameObject(hamGameObjectName);
                hamGameObject.transform.localScale = new Vector3(renderTargetAspect, 1f, 1f);
                hamGameObject.layer = HAM_LAYER;

                // add mesh components
                MeshFilter hamMeshFilter = hamGameObject.AddComponent<MeshFilter>();
                MeshRenderer hamMeshRenderer = hamGameObject.AddComponent<MeshRenderer>();

                hamMeshFilter.sharedMesh = hamMesh[(int)eye];
                //hamMeshRenderer.sharedMaterial = new Material(Shader.Find("Unlit/Color"));
                //hamMeshRenderer.sharedMaterial.color = Color.black;
                hamMeshRenderer.sharedMaterial = new Material(ShaderDatabase.GetShader("KerbalVR/Mask"));

            }

            return hamGameObject;
        }


        private GameObject CreateMeshGizmo(GameObject obj) {
            /*GameObject meshGizmo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            meshGizmo.name = "mesh_gizmo " + obj.name;
            meshGizmo.layer = 20;
            meshGizmo.transform.localScale = Vector3.one * 0.1f;
            Destroy(meshGizmo.GetComponent<SphereCollider>());*/

            GameObject meshGizmo = new GameObject("mesh_gizmo " + obj.name);
            meshGizmo.transform.localScale = obj.transform.localScale;
            meshGizmo.layer = 20;

            Mesh objMesh = obj.GetComponent<MeshFilter>().sharedMesh;

            int numVertices = objMesh.vertices.Length;
            for (int i = 0; i < numVertices; i++) {
                GameObject vertexObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                vertexObj.layer = 20;
                vertexObj.transform.SetParent(meshGizmo.transform);
                vertexObj.transform.localScale = Vector3.one * 0.03f;
                vertexObj.transform.localPosition = objMesh.vertices[i];
                Material vertexObjMat = new Material(ShaderDatabase.GetShader("KerbalVR/Mask"));
                vertexObj.GetComponent<MeshRenderer>().material = vertexObjMat;
                Destroy(vertexObj.GetComponent<SphereCollider>());
            }
            
            return meshGizmo;
        }

    } // class KerbalVR_Plugin
} // namespace KerbalVR
