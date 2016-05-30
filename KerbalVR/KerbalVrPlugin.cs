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


        /// <summary>
        /// Overrides the Start method for a MonoBehaviour plugin.
        /// </summary>
        void Start()
        {
            Debug.Log("[KerbalVR] KerbalVrPlugin started.");

            int sz = 512;

            rt1 = new RenderTexture(sz, sz, 16, RenderTextureFormat.ARGB32);
            rt1.Create();

            rt2 = new RenderTexture(sz, sz, 16, RenderTextureFormat.ARGB32);
            rt2.Create();

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
                vrSystem.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseSeated, 0.0f, vrDevicePoses);
                //HmdMatrix34_t hmdLeftEyeXform = vrSystem.GetEyeToHeadTransform(EVREye.Eye_Left);
                //HmdMatrix34_t hmdRightEyeXform = vrSystem.GetEyeToHeadTransform(EVREye.Eye_Right);
                vrCompositorError = vrCompositor.WaitGetPoses(vrRenderPoses, vrGamePoses);

                if (vrCompositorError != EVRCompositorError.None)
                {
                    Debug.Log("[KerbalVR] WaitGetPoses failed: " + (int)vrCompositorError);
                }

                var hmdTransform = new SteamVR_Utils.RigidTransform(vrDevicePoses[0].mDeviceToAbsoluteTracking);
                var hmdLTransform = new SteamVR_Utils.RigidTransform(vrSystem.GetEyeToHeadTransform(EVREye.Eye_Left));
                var hmdRTransform = new SteamVR_Utils.RigidTransform(vrSystem.GetEyeToHeadTransform(EVREye.Eye_Right));

                HmdMatrix44_t hmdLeftProjMatrix = vrSystem.GetProjectionMatrix(EVREye.Eye_Left, 0.1f, 1000.0f, EGraphicsAPIConvention.API_OpenGL);
                HmdMatrix44_t hmdRightProjMatrix = vrSystem.GetProjectionMatrix(EVREye.Eye_Right, 0.1f, 1000.0f, EGraphicsAPIConvention.API_OpenGL);


                //Vector3 hmdPosition = new Vector3();
                //Vector3 hmdRotation = new Vector3();
                //MathUtils.PoseMatrix2PositionAndRotation(ref vrDevicePoses[0].mDeviceToAbsoluteTracking, ref hmdPosition, ref hmdRotation);
                //Quaternion hmdQuat = new Quaternion();
                //MathUtils.PoseMatrix2PositionAndRotation(ref vrDevicePoses[0].mDeviceToAbsoluteTracking, ref hmdPosition, ref hmdQuat);

                /*
                Vector3 hmdLeftPosition = new Vector3();
                Quaternion hmdLeftQuat = new Quaternion();
                MathUtils.PoseMatrix2PositionAndRotation(ref hmdLeftEyeXform, ref hmdLeftPosition, ref hmdLeftQuat);

                Vector3 hmdRightPosition = new Vector3();
                Quaternion hmdRightQuat = new Quaternion();
                MathUtils.PoseMatrix2PositionAndRotation(ref hmdRightEyeXform, ref hmdRightPosition, ref hmdRightQuat);
                */

                // Transform camera orientation
                /*Vector3 camEulerAngles = InternalCamera.Instance.transform.localEulerAngles;
                camEulerAngles.x = camEulerAngles.x - hmdRotation.x * Mathf.Rad2Deg;
                camEulerAngles.y = camEulerAngles.y - hmdRotation.y * Mathf.Rad2Deg;
                camEulerAngles.z = camEulerAngles.z + hmdRotation.z * Mathf.Rad2Deg;
                InternalCamera.Instance.transform.localEulerAngles = camEulerAngles;*/
                //InternalCamera.Instance.transform.localRotation = Quaternion.Inverse(hmdQuat * hmdLeftQuat);


                // Transform camera position
                /*float posScale = 1f;
                Vector3 deltaPos = (hmdPosition + hmdLeftPosition) - hmdInitialPosition;
                Vector3 camPosition = new Vector3(deltaPos.x, deltaPos.y, -deltaPos.z);
                camPosition = camPosition * posScale;
                InternalCamera.Instance.transform.localPosition = camPosition;*/

                // adjust FOV
                //InternalCamera.Instance.SetFOV(90.0f);

                InternalCamera.Instance.transform.localPosition = (hmdTransform.pos + hmdLTransform.pos) - hmdInitialPosition;
                InternalCamera.Instance.transform.localRotation = hmdTransform.rot * hmdLTransform.rot;

                // adjust the flight camera as well, otherwise the outside world will not move accordingly
                FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);
                FlightCamera.fetch.transform.position = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
                FlightCamera.fetch.mainCamera.projectionMatrix = MathUtils.Matrix4x4_OpenVr2UnityFormat(ref hmdLeftProjMatrix);

                Camera cam = FlightCamera.fetch.mainCamera;
                cam.targetTexture = rt1;
                RenderTexture.active = rt1;
                cam.Render();
                tex1.ReadPixels(new Rect(0f, 0f, rt1.width, rt1.height), 0, 0);
                tex1.Apply(false);
                RenderTexture.active = null;
                cam.targetTexture = null;


                
                InternalCamera.Instance.transform.localPosition = (hmdTransform.pos + hmdRTransform.pos) - hmdInitialPosition;
                InternalCamera.Instance.transform.localRotation = hmdTransform.rot * hmdRTransform.rot;
                FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);
                FlightCamera.fetch.transform.position = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
                FlightCamera.fetch.mainCamera.projectionMatrix = MathUtils.Matrix4x4_OpenVr2UnityFormat(ref hmdRightProjMatrix);

                cam.targetTexture = rt2;
                cam.projectionMatrix = MathUtils.Matrix4x4_OpenVr2UnityFormat(ref hmdRightProjMatrix);
                RenderTexture.active = rt2;
                cam.Render();
                tex2.ReadPixels(new Rect(0f, 0f, rt2.width, rt2.height), 0, 0);
                tex2.Apply(false);
                RenderTexture.active = null;
                cam.targetTexture = null;



                //Debug.Log("[KerbalVR] About to submit frames.");

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

            hmdTex1.handle = tex1.GetNativeTexturePtr();
            hmdTex1.eType = EGraphicsAPIConvention.API_OpenGL;
            hmdTex1.eColorSpace = EColorSpace.Auto;

            hmdTex2.handle = tex2.GetNativeTexturePtr();
            hmdTex2.eType = EGraphicsAPIConvention.API_OpenGL;
            hmdTex2.eColorSpace = EColorSpace.Auto;

            bnds.uMin = 0.0f;
            bnds.uMax = 1.0f;
            bnds.vMin = 0.0f;
            bnds.vMax = 1.0f;

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
                vrSystem.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseSeated, 0.0f, vrDevicePoses);

                //hmdInitialPosition = MathUtils.PoseMatrix2Position(ref vrDevicePoses[0].mDeviceToAbsoluteTracking);
                var hmdTransform = new SteamVR_Utils.RigidTransform(vrDevicePoses[0].mDeviceToAbsoluteTracking);
                hmdInitialPosition = hmdTransform.pos;
            }
        }
    }

}
