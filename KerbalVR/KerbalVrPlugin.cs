using System;
using System.Collections.Generic;
using System.Linq;
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
        private TrackedDevicePose_t[] vrDevicePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];


        /// <summary>
        /// Overrides the Start method for a MonoBehaviour plugin.
        /// </summary>
        void Start()
        {
            Debug.Log("[KerbalVR] KerbalVrPlugin started.");
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
                }
                else
                {
                    ResetInitialHmdPosition();
                }
            }

            // perform regular updates if HMD is initialized
            if (hmdIsInitialized)
            {
                // get latest HMD pose
                vrSystem.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseSeated, 0.0f, vrDevicePoses);

                Vector3 hmdPosition = new Vector3();
                Vector3 hmdRotation = new Vector3();
                MathUtils.PoseMatrix2PositionAndRotation(ref vrDevicePoses[0].mDeviceToAbsoluteTracking, ref hmdPosition, ref hmdRotation);
                //Quaternion hmdQuat = new Quaternion();
                //MathUtils.PoseMatrix2PositionAndRotation(ref vrDevicePoses[0].mDeviceToAbsoluteTracking, ref hmdPosition, ref hmdQuat);

                // Transform camera orientation
                Vector3 camEulerAngles = InternalCamera.Instance.transform.localEulerAngles;
                camEulerAngles.x = camEulerAngles.x - hmdRotation.x * Mathf.Rad2Deg;
                camEulerAngles.y = camEulerAngles.y - hmdRotation.y * Mathf.Rad2Deg;
                camEulerAngles.z = camEulerAngles.z + hmdRotation.z * Mathf.Rad2Deg;
                InternalCamera.Instance.transform.localEulerAngles = camEulerAngles;
                //InternalCamera.Instance.transform.localRotation = Quaternion.Inverse(hmdQuat);
                

                // Transform camera position
                float posScale = 0.8f;
                Vector3 deltaPos = hmdPosition - hmdInitialPosition;
                Vector3 camPosition = new Vector3(deltaPos.x, deltaPos.y, -deltaPos.z);
                camPosition = camPosition * posScale;

                InternalCamera.Instance.transform.localPosition = camPosition;

                // adjust FOV
                InternalCamera.Instance.SetFOV(120.0f);

                // adjust the flight camera as well, otherwise the outside world will not move accordingly
                FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);
                FlightCamera.fetch.transform.position = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
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
            vrSystem = OpenVR.Init(ref hmdInitErrorCode, EVRApplicationType.VRApplication_Background);

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

                hmdInitialPosition = MathUtils.PoseMatrix2Position(ref vrDevicePoses[0].mDeviceToAbsoluteTracking);
            }
        }
    }

}
