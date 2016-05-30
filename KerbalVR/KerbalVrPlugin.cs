using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
        private Vector3 hmdInitialPosition = new Vector3();

        private CVRSystem vrSystem;
        private CVRCompositor vrCompositor;
        private TrackedDevicePose_t[] vrDevicePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private TrackedDevicePose_t[] vrRenderPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private TrackedDevicePose_t[] vrGamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        
        private Texture_t hmdTex1, hmdTex2;
        private VRTextureBounds_t bnds;
        private RenderTexture rt1, rt2;
        private Texture2D tex1, tex2;

        private float counter = 0f;
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
        private int cameraNameSelected = 0;

        private string[] camerasToRender = new string[3]
        {
            //"Camera ScaledSpace",
            "Camera 01",
            "Camera 00",
            "InternalCamera",
        };


        /// <summary>
        /// Overrides the Start method for a MonoBehaviour plugin.
        /// </summary>
        void Start()
        {
            Debug.Log("[KerbalVR] KerbalVrPlugin started.");

            int sz = 512;

            /*
            rt1 = new RenderTexture(sz, sz, 16, RenderTextureFormat.ARGB32);
            rt1.Create();

            rt2 = new RenderTexture(sz, sz, 16, RenderTextureFormat.ARGB32);
            rt2.Create();
            */

            tex1 = new Texture2D(sz, sz);
            //tex2 = Texture2D.whiteTexture;
            tex2 = new Texture2D(sz, sz);
        }

        /// <summary>
        /// Overrides the Update method, called every frame.
        /// </summary>
        void Update()
        {
            // do nothing unless we are in IVA
            if (CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA)
            {
                return;
            }

            // start HMD using the N key
            if (Input.GetKeyDown(KeyCode.N))
            {
                if (!hmdIsInitialized)
                {
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
            if (hmdIsInitialized)
            {
                EVRCompositorError vrCompositorError = EVRCompositorError.None;

                // get latest HMD pose
                //--------------------------------------------------------------
                vrSystem.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseSeated, 0.0f, vrDevicePoses);
                HmdMatrix34_t hmdLeftEyeXform = vrSystem.GetEyeToHeadTransform(EVREye.Eye_Left);
                HmdMatrix34_t hmdRightEyeXform = vrSystem.GetEyeToHeadTransform(EVREye.Eye_Right);
                vrCompositorError = vrCompositor.WaitGetPoses(vrRenderPoses, vrGamePoses);

                if (vrCompositorError != EVRCompositorError.None)
                {
                    Debug.Log("[KerbalVR] WaitGetPoses failed: " + (int)vrCompositorError);
                    return;
                }

                // convert SteamVR poses to Unity coordinates
                var hmdTransform = new SteamVR_Utils.RigidTransform(vrDevicePoses[0].mDeviceToAbsoluteTracking);
                var hmdLTransform = new SteamVR_Utils.RigidTransform(vrSystem.GetEyeToHeadTransform(EVREye.Eye_Left));
                var hmdRTransform = new SteamVR_Utils.RigidTransform(vrSystem.GetEyeToHeadTransform(EVREye.Eye_Right));

                // get projection matrices from HMD eyes
                float nearClip = 0.05f;// FlightCamera.fetch.mainCamera.nearClipPlane;
                float farClip = FlightCamera.fetch.mainCamera.farClipPlane;
                HmdMatrix44_t hmdLeftProjMatrix = vrSystem.GetProjectionMatrix(EVREye.Eye_Left, nearClip, farClip, EGraphicsAPIConvention.API_OpenGL);
                HmdMatrix44_t hmdRightProjMatrix = vrSystem.GetProjectionMatrix(EVREye.Eye_Right, nearClip, farClip, EGraphicsAPIConvention.API_OpenGL);

                // Select the camera to render to the HMD
                //--------------------------------------------------------------
                /*string galaxyCamera = "GalaxyCamera";
                string scaledCamera = "Camera ScaledSpace";
                string camera01 = "Camera 01";
                string camera00 = "Camera 00";
                string internalCamera = "InternalCamera";
                string uiMainCamera = "UIMainCamera";
                string uiVectorCamera = "UIVectorCamera";*/

                InternalCamera.Instance.transform.localRotation = hmdTransform.rot;
                InternalCamera.Instance.transform.localPosition = new Vector3(0f, 0f, 0f);
                InternalCamera.Instance.transform.Translate(hmdLTransform.pos);
                InternalCamera.Instance.transform.localPosition += hmdTransform.pos;

                Vector3 camLPosition = InternalCamera.Instance.transform.localPosition;
                Quaternion camLRotation = InternalCamera.Instance.transform.localRotation;

                InternalCamera.Instance.transform.localRotation = hmdTransform.rot;
                InternalCamera.Instance.transform.localPosition = new Vector3(0f, 0f, 0f);
                InternalCamera.Instance.transform.Translate(hmdRTransform.pos);
                InternalCamera.Instance.transform.localPosition += hmdTransform.pos;

                Vector3 camRPosition = InternalCamera.Instance.transform.localPosition;
                Quaternion camRRotation = InternalCamera.Instance.transform.localRotation;

                foreach (string camToRender in camerasToRender)
                {
                    Camera cam = FlightCamera.fetch.mainCamera;

                    foreach (Camera c in Camera.allCameras)
                    {
                        if (c.name.Equals(camToRender, StringComparison.Ordinal))
                        {
                            cam = c;
                            break;
                        }
                    }

                    Matrix4x4 camOriginalProjection = cam.projectionMatrix;
                    Vector3 camOriginalPosition = cam.transform.localPosition;
                    Quaternion camOriginalRotation = cam.transform.localRotation;

                    // Render the LEFT eye
                    //--------------------------------------------------------------
                    cam.transform.localRotation = hmdTransform.rot;
                    cam.transform.localPosition = new Vector3(0f, 0f, 0f);
                    cam.transform.Translate(hmdLTransform.pos);
                    cam.transform.localPosition += hmdTransform.pos;
                    cam.projectionMatrix = MathUtils.Matrix4x4_OpenVr2UnityFormat(ref hmdLeftProjMatrix);

                    // render
                    cam.targetTexture = rt1;
                    RenderTexture.active = rt1;
                    cam.Render();

                    // Render the RIGHT eye
                    //--------------------------------------------------------------
                    cam.transform.localRotation = hmdTransform.rot;
                    cam.transform.localPosition = new Vector3(0f, 0f, 0f);
                    cam.transform.Translate(hmdRTransform.pos);
                    cam.transform.localPosition += hmdTransform.pos;
                    cam.projectionMatrix = MathUtils.Matrix4x4_OpenVr2UnityFormat(ref hmdRightProjMatrix);

                    // render
                    cam.targetTexture = rt2;
                    RenderTexture.active = rt2;
                    cam.Render();
                    RenderTexture.active = null;
                    cam.targetTexture = null;

                    // Reset camera position to an HMD-centered position (for regular screen rendering)
                    //--------------------------------------------------------------
                    cam.transform.localPosition = hmdTransform.pos;
                    cam.transform.localRotation = hmdTransform.rot;
                }




                /* debug
                InternalCamera.Instance.transform.localRotation = hmdTransform.rot;
                InternalCamera.Instance.transform.localPosition = new Vector3(0f, 0f, 0f);
                InternalCamera.Instance.transform.Translate(counter, 0f, 0f);
                FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);
                FlightCamera.fetch.transform.position = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
                */






                // Submit frames to HMD
                //--------------------------------------------------------------
                vrCompositorError = vrCompositor.Submit(EVREye.Eye_Left, ref hmdTex1, ref bnds, EVRSubmitFlags.Submit_Default);
                if (vrCompositorError != EVRCompositorError.None)
                {
                    Debug.Log("[KerbalVR] Submit (Eye_Left) failed: " + (int)vrCompositorError);
                }


                vrCompositorError = vrCompositor.Submit(EVREye.Eye_Right, ref hmdTex2, ref bnds, EVRSubmitFlags.Submit_Default);
                if (vrCompositorError != EVRCompositorError.None)
                {
                    Debug.Log("[KerbalVR] Submit (Eye_Right) failed: " + (int)vrCompositorError);
                }


                // DEBUG
                if (Input.GetKeyDown(KeyCode.H))
                {
                    Debug.Log("[KerbalVR] POSITION hmdTransform : " + hmdTransform.pos.x + ", " + hmdTransform.pos.y + ", " + hmdTransform.pos.z);
                    Debug.Log("[KerbalVR] POSITION hmdLTransform : " + hmdLTransform.pos.x + ", " + hmdLTransform.pos.y + ", " + hmdLTransform.pos.z);
                    Debug.Log("[KerbalVR] POSITION hmdRTransform : " + hmdRTransform.pos.x + ", " + hmdRTransform.pos.y + ", " + hmdRTransform.pos.z);

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

            Debug.Log("[KerbalVR] Render Texture size: " + renderTextureWidth + " x " + renderTextureHeight);

            rt1 = new RenderTexture((int)renderTextureWidth, (int)renderTextureHeight, 24, RenderTextureFormat.ARGB32);
            rt1.Create();

            rt2 = new RenderTexture((int)renderTextureWidth, (int)renderTextureHeight, 24, RenderTextureFormat.ARGB32);
            rt2.Create();

            hmdTex1.handle = rt1.GetNativeTexturePtr();
            hmdTex1.eType = EGraphicsAPIConvention.API_OpenGL;
            hmdTex1.eColorSpace = EColorSpace.Auto;

            hmdTex2.handle = rt2.GetNativeTexturePtr();
            hmdTex2.eType = EGraphicsAPIConvention.API_OpenGL;
            hmdTex2.eColorSpace = EColorSpace.Auto;

            bnds.uMin = 0.0f;
            bnds.uMax = 1.0f;
            bnds.vMin = 0.0f;
            bnds.vMax = 1.0f;

            Vector3 intCamPos = InternalCamera.Instance.transform.localPosition;
            Debug.Log("[KerbalVR] InternalCamera position: " + intCamPos.x + ", " + intCamPos.y + ", " + intCamPos.z);

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
            }
        }
    }

}
