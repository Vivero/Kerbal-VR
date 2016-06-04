using System;
using System.Collections.Generic;
using System.Reflection;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;
using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    // Start plugin on entering the Flight scene
    //
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KerbalVrPlugin : MonoBehaviour
    {
        private bool hmdIsInitialized = false;
        private bool hmdIsActive = false;
        private bool hmdIsActive_prev = false;
        private bool hmdIsRenderingLeft = true;

        private CVRSystem vrSystem;
        private CVRCompositor vrCompositor;
        private TrackedDevicePose_t[] vrDevicePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private TrackedDevicePose_t[] vrRenderPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private TrackedDevicePose_t[] vrGamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        
        private Texture_t hmdLeftEyeTexture, hmdRightEyeTexture;
        private VRTextureBounds_t hmdTextureBounds;
        private RenderTexture hmdLeftEyeRenderTexture, hmdRightEyeRenderTexture;
        
        // list of all cameras in the game
        private string[] cameraNames = new string[7]
        {
            "GalaxyCamera",
            "Camera ScaledSpace",
            "Camera 01",
            "Camera 00",
            "InternalCamera",
            "UIMainCamera",
            "UIVectorCamera",
        };

        // list of cameras to render (string names), defined on Start()
        private List<string> cameraNamesToRender;

        // struct to keep track of Camera properties
        private struct CameraProperties
        {
            public Camera camera;
            public Matrix4x4 originalProjMatrix;
            public Matrix4x4 hmdLeftProjMatrix;
            public Matrix4x4 hmdRightProjMatrix;

            public CameraProperties(Camera camera, Matrix4x4 originalProjMatrix, Matrix4x4 hmdLeftProjMatrix, Matrix4x4 hmdRightProjMatrix)
            {
                this.camera = camera;
                this.originalProjMatrix = originalProjMatrix;
                this.hmdLeftProjMatrix = hmdLeftProjMatrix;
                this.hmdRightProjMatrix = hmdRightProjMatrix;
            }
        }

        // list of cameras to render (Camera objects)
        private List<CameraProperties> camerasToRender;


        //*** debug
        private float counter = 0f;
        private int cameraNameSelected = 0;
        //*** debug



        /// <summary>
        /// Overrides the Start method for a MonoBehaviour plugin.
        /// </summary>
        void Start()
        {
            Debug.Log("[KerbalVR] KerbalVrPlugin started.");
            
            // define what cameras to render to HMD
            cameraNamesToRender = new List<string>();
            cameraNamesToRender.Add(cameraNames[0]); // renders the galaxy
            cameraNamesToRender.Add(cameraNames[1]); // renders space/planets?
            cameraNamesToRender.Add(cameraNames[2]); // renders things far away (like out to the horizon)
            cameraNamesToRender.Add(cameraNames[3]); // renders things close to you
            cameraNamesToRender.Add(cameraNames[4]); // renders the IVA view (cockpit)
            //cameraNamesToRender.Add(cameraNames[5]); // don't render UI, it looks shitty
            //cameraNamesToRender.Add(cameraNames[6]); // don't render UI, it looks shitty

            camerasToRender = new List<CameraProperties>(cameraNamesToRender.Count);
        }

        /// <summary>
        /// Overrides the Update method, called every frame.
        /// </summary>
        void Update()
        {
            // do nothing unless we are in IVA
            if (CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA)
            {
                hmdIsActive = false;
            }
            else
            {
                hmdIsActive = true;
            }

            // start HMD using the N key
            if (Input.GetKeyDown(KeyCode.N) && hmdIsActive)
            {
                if (!hmdIsInitialized)
                {
                    Debug.Log("[KerbalVR] Initializing HMD...");
                    bool retVal = InitHMD();
                    if (retVal)
                    {
                        Debug.Log("[KerbalVR] HMD initialized.");
                    }
                }
                else
                {
                    ResetInitialHmdPosition();
                }
            }

            // perform regular updates if HMD is initialized
            if (hmdIsActive && hmdIsInitialized)
            {
                EVRCompositorError vrCompositorError = EVRCompositorError.None;

                // get latest HMD pose
                //--------------------------------------------------------------
                vrSystem.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseSeated, 0.0f, vrDevicePoses);
                HmdMatrix34_t vrLeftEyeTransform = vrSystem.GetEyeToHeadTransform(EVREye.Eye_Left);
                HmdMatrix34_t vrRightEyeTransform = vrSystem.GetEyeToHeadTransform(EVREye.Eye_Right);
                vrCompositorError = vrCompositor.WaitGetPoses(vrRenderPoses, vrGamePoses);

                if (vrCompositorError != EVRCompositorError.None)
                {
                    Debug.Log("[KerbalVR] WaitGetPoses failed: " + (int)vrCompositorError);
                    return;
                }

                // convert SteamVR poses to Unity coordinates
                var hmdTransform = new SteamVR_Utils.RigidTransform(vrDevicePoses[0].mDeviceToAbsoluteTracking);
                var hmdLeftEyeTransform = new SteamVR_Utils.RigidTransform(vrLeftEyeTransform);
                var hmdRightEyeTransform = new SteamVR_Utils.RigidTransform(vrRightEyeTransform);

                // Render the LEFT eye
                //--------------------------------------------------------------
                // rotate camera according to the HMD orientation
                InternalCamera.Instance.transform.localRotation = hmdTransform.rot;

                // translate the camera to match the position of the left eye, from origin
                InternalCamera.Instance.transform.localPosition = new Vector3(0f, 0f, 0f);
                InternalCamera.Instance.transform.Translate(hmdLeftEyeTransform.pos);

                // translate the camera to match the position of the HMD
                InternalCamera.Instance.transform.localPosition += hmdTransform.pos;

                // move the FlightCamera to match the position of the InternalCamera (so the outside world moves accordingly)
                FlightCamera.fetch.transform.position = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
                FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);

                // render the set of cameras
                foreach (CameraProperties camStruct in camerasToRender)
                {
                    // set projection matrix
                    camStruct.camera.projectionMatrix = camStruct.hmdLeftProjMatrix;

                    // set texture to render to
                    camStruct.camera.targetTexture = hmdLeftEyeRenderTexture;
                    RenderTexture.active = hmdLeftEyeRenderTexture;

                    // render camera
                    camStruct.camera.Render();

                    // reset texture buffer
                    //camStruct.camera.targetTexture = null;
                    //RenderTexture.active = null;

                    // reset camera projection back to original
                    //camStruct.camera.projectionMatrix = camStruct.originalProjMatrix;
                }


                // Render the RIGHT eye (see previous comments)
                //--------------------------------------------------------------
                InternalCamera.Instance.transform.localRotation = hmdTransform.rot;
                InternalCamera.Instance.transform.localPosition = new Vector3(0f, 0f, 0f);
                InternalCamera.Instance.transform.Translate(hmdRightEyeTransform.pos);
                InternalCamera.Instance.transform.localPosition += hmdTransform.pos;
                FlightCamera.fetch.transform.position = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
                FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);

                foreach (CameraProperties camStruct in camerasToRender)
                {
                    camStruct.camera.projectionMatrix = camStruct.hmdRightProjMatrix;
                    camStruct.camera.targetTexture = hmdRightEyeRenderTexture;
                    RenderTexture.active = hmdRightEyeRenderTexture;
                    camStruct.camera.Render();
                    //camStruct.camera.targetTexture = null;
                    //RenderTexture.active = null;
                    //camStruct.camera.projectionMatrix = camStruct.originalProjMatrix;
                }

                // Set camera position to an HMD-centered position (for regular screen rendering)
                //--------------------------------------------------------------
                /*InternalCamera.Instance.transform.localRotation = hmdTransform.rot;
                InternalCamera.Instance.transform.localPosition = hmdTransform.pos;
                FlightCamera.fetch.transform.position = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
                FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);*/


                /* debug
                InternalCamera.Instance.transform.localRotation = hmdTransform.rot;
                InternalCamera.Instance.transform.localPosition = new Vector3(0f, 0f, 0f);
                InternalCamera.Instance.transform.Translate(counter, 0f, 0f);
                FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);
                FlightCamera.fetch.transform.position = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
                */


                // Submit frames to HMD
                //--------------------------------------------------------------
                vrCompositorError = vrCompositor.Submit(EVREye.Eye_Left, ref hmdLeftEyeTexture, ref hmdTextureBounds, EVRSubmitFlags.Submit_Default);
                if (vrCompositorError != EVRCompositorError.None)
                {
                    Debug.Log("[KerbalVR] Submit (Eye_Left) failed: " + (int)vrCompositorError);
                }


                vrCompositorError = vrCompositor.Submit(EVREye.Eye_Right, ref hmdRightEyeTexture, ref hmdTextureBounds, EVRSubmitFlags.Submit_Default);
                if (vrCompositorError != EVRCompositorError.None)
                {
                    Debug.Log("[KerbalVR] Submit (Eye_Right) failed: " + (int)vrCompositorError);
                }

                // disable highlighting of parts due to mouse
                Part hoveredPart = Mouse.HoveredPart;
                if (hoveredPart != null)
                {
                    hoveredPart.HighlightActive = false;
                    hoveredPart.highlightColor = new Color(0f, 0f, 0f, 0f);
                    //Debug.Log("[KerbalVR] hovered part: " + hoveredPart.name);
                }


                // DEBUG
                if (Input.GetKeyDown(KeyCode.H))
                {
                    Debug.Log("[KerbalVR] POSITION hmdTransform : " + hmdTransform.pos.x + ", " + hmdTransform.pos.y + ", " + hmdTransform.pos.z);
                    Debug.Log("[KerbalVR] POSITION hmdLTransform : " + hmdLeftEyeTransform.pos.x + ", " + hmdLeftEyeTransform.pos.y + ", " + hmdLeftEyeTransform.pos.z);
                    Debug.Log("[KerbalVR] POSITION hmdRTransform : " + hmdRightEyeTransform.pos.x + ", " + hmdRightEyeTransform.pos.y + ", " + hmdRightEyeTransform.pos.z);

                    foreach (Camera c in Camera.allCameras)
                    {
                        Debug.Log("[KerbalVR] Camera: " + c.name);
                    }
                    Debug.Log("[KerbalVR] FlightCamera: " + FlightCamera.fetch.mainCamera.name);

                    cameraNameSelected += 1;
                    cameraNameSelected = (cameraNameSelected >= cameraNames.Length) ? 0 : cameraNameSelected;
                    Debug.Log("[KerbalVR] Rendering camera: " + cameraNames[cameraNameSelected]);
                }

                if (Input.GetKeyDown(KeyCode.I))
                {
                    counter += 0.2f;
                    Debug.Log("[KerbalVR] Counter = " + counter);
                }

                    if (Input.GetKeyDown(KeyCode.K))
                {
                    counter -= 0.2f;
                    Debug.Log("[KerbalVR] Counter = " + counter);
                }
            }

            // if we are exiting VR, restore the cameras
            if (!hmdIsActive && hmdIsActive_prev)
            {
                foreach (CameraProperties camStruct in camerasToRender)
                {
                    camStruct.camera.projectionMatrix = camStruct.originalProjMatrix;
                    camStruct.camera.targetTexture = null;
                    RenderTexture.active = null;
                }
            }

            hmdIsActive_prev = hmdIsActive;
        }

        /// <summary>
        /// Overrides the OnDestroy method, called when plugin is destroyed (leaving Flight scene).
        /// </summary>
        void OnDestroy()
        {
            Debug.Log("[KerbalVR] KerbalVrPlugin OnDestroy");
            OpenVR.Shutdown();
            hmdIsInitialized = false;
        }

        /// <summary>
        /// Initialize HMD using OpenVR API calls.
        /// </summary>
        /// <returns>True on success, false otherwise. Errors logged.</returns>
        bool InitHMD()
        {
            bool retVal = false;

            // return if HMD has already been initialized
            if (hmdIsInitialized)
            {
                return true;
            }

            // check if HMD is connected on the system
            retVal = OpenVR.IsHmdPresent();
            if (!retVal)
            {
                Debug.Log("[KerbalVR] HMD not found on this system.");
                return retVal;
            }

            // check if SteamVR runtime is installed.
            // For this plugin, MAKE SURE IT IS ALREADY RUNNING.
            retVal = OpenVR.IsRuntimeInstalled();
            if (!retVal)
            {
                Debug.Log("[KerbalVR] SteamVR runtime not found on this system.");
                return retVal;
            }

            // initialize HMD
            EVRInitError hmdInitErrorCode = EVRInitError.None;
            vrSystem = OpenVR.Init(ref hmdInitErrorCode, EVRApplicationType.VRApplication_Scene);

            // return if failure
            retVal = (hmdInitErrorCode == EVRInitError.None);
            if (!retVal)
            {
                Debug.Log("[KerbalVR] Failed to initialize HMD. Init returned: " + OpenVR.GetStringForHmdError(hmdInitErrorCode));
                return retVal;
            }
            else
            {
                Debug.Log("[KerbalVR] OpenVR.Init passed.");
            }
            
            // reset "seated position" and capture initial position. this means you should hold the HMD in
            // the position you would like to consider "seated", before running this code.
            hmdIsInitialized = true;
            ResetInitialHmdPosition();

            // initialize Compositor
            vrCompositor = OpenVR.Compositor;

            // initialize render textures (for displaying on HMD)
            uint renderTextureWidth = 0;
            uint renderTextureHeight = 0;
            vrSystem.GetRecommendedRenderTargetSize(ref renderTextureWidth, ref renderTextureHeight);
            //renderTextureWidth /= 2;
            //renderTextureHeight /= 2;

            //Debug.Log("[KerbalVR] Render Texture size: " + renderTextureWidth + " x " + renderTextureHeight);

            hmdLeftEyeRenderTexture = new RenderTexture((int)renderTextureWidth, (int)renderTextureHeight, 24, RenderTextureFormat.ARGB32);
            hmdLeftEyeRenderTexture.Create();

            hmdRightEyeRenderTexture = new RenderTexture((int)renderTextureWidth, (int)renderTextureHeight, 24, RenderTextureFormat.ARGB32);
            hmdRightEyeRenderTexture.Create();

            hmdLeftEyeTexture.handle = hmdLeftEyeRenderTexture.GetNativeTexturePtr();
            hmdLeftEyeTexture.eType = EGraphicsAPIConvention.API_OpenGL;
            //hmdLeftEyeTexture.eType = EGraphicsAPIConvention.API_DirectX; // this doesn't seem to work
            hmdLeftEyeTexture.eColorSpace = EColorSpace.Auto;

            hmdRightEyeTexture.handle = hmdRightEyeRenderTexture.GetNativeTexturePtr();
            hmdRightEyeTexture.eType = EGraphicsAPIConvention.API_OpenGL;
            //hmdRightEyeTexture.eType = EGraphicsAPIConvention.API_DirectX; // this doesn't seem to work
            hmdRightEyeTexture.eColorSpace = EColorSpace.Auto;

            // Set rendering bounds on texture to render?
            // I assume min=0.0 and max=1.0 renders to the full extent of the texture
            hmdTextureBounds.uMin = 0.0f;
            hmdTextureBounds.uMax = 1.0f;
            hmdTextureBounds.vMin = 0.0f;
            hmdTextureBounds.vMax = 1.0f;

            // search for camera objects to render
            foreach (string cameraName in cameraNamesToRender)
            {
                foreach (Camera camera in Camera.allCameras)
                {
                    if (cameraName.Equals(camera.name))
                    {
                        HmdMatrix44_t projLeft = vrSystem.GetProjectionMatrix(EVREye.Eye_Left, camera.nearClipPlane, camera.farClipPlane, EGraphicsAPIConvention.API_OpenGL);
                        HmdMatrix44_t projRight = vrSystem.GetProjectionMatrix(EVREye.Eye_Right, camera.nearClipPlane, camera.farClipPlane, EGraphicsAPIConvention.API_OpenGL);
                        //HmdMatrix44_t projLeft = vrSystem.GetProjectionMatrix(EVREye.Eye_Left, camera.nearClipPlane, camera.farClipPlane, EGraphicsAPIConvention.API_DirectX); // this doesn't seem to work
                        //HmdMatrix44_t projRight = vrSystem.GetProjectionMatrix(EVREye.Eye_Right, camera.nearClipPlane, camera.farClipPlane, EGraphicsAPIConvention.API_DirectX); // this doesn't seem to work
                        camerasToRender.Add(new CameraProperties(camera, camera.projectionMatrix, MathUtils.Matrix4x4_OpenVr2UnityFormat(ref projLeft), MathUtils.Matrix4x4_OpenVr2UnityFormat(ref projRight)));
                        break;
                    }
                }
            }

            //*** debug
            /*foreach (Camera cam in camerasToRender)
            {
                Debug.Log("[KerbalVR] Found camera: " + cam.name + ", clip near: " + cam.nearClipPlane + ", clip far: " + cam.farClipPlane);
            }*/
            //*** debug

            return retVal;
        }

        /// <summary>
        /// Sets the current real-world position of the HMD as the seated origin in IVA.
        /// </summary>
        void ResetInitialHmdPosition()
        {
            if (hmdIsInitialized)
            {
                vrSystem.ResetSeatedZeroPose();
                Debug.Log("[KerbalVR] Seated pose reset!");
            }
        }
    }

}
