using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    public class DeviceManager : MonoBehaviour
    {
        private GameObject controllerObjL;

        private bool[] isDeviceConnected = new bool[OpenVR.k_unMaxTrackedDeviceCount];
        private uint leftControllerIndex;
        private uint rightControllerIndex;

        void Awake() {
            Utils.LogInfo("DeviceManager starting...");
        }

        void OnEnable() {
            EventManager.StartListening(EventManager.EVENT_DEVICE_POSES_READY, OnDevicePosesReady);
        }

        void OnDisable() {
            EventManager.StopListening(EventManager.EVENT_DEVICE_POSES_READY, OnDevicePosesReady);
        }

        void Update() {

        }

        private void OnDevicePosesReady() {
            
            if (controllerObjL == null) {
                controllerObjL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                controllerObjL.name = "VR_ControllerL";
                controllerObjL.layer = 20;
                controllerObjL.transform.localScale = Vector3.one * 0.1f;
            }

            leftControllerIndex = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
            rightControllerIndex = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);

            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++) {
                bool isConnected = KerbalVR.IsDeviceConnected(i);
                if (isDeviceConnected[i] != isConnected) {
                    // EventManager.TriggerEvent(EventManager.EVENT_DEVICE_CONNECTED);
                }
                isDeviceConnected[i] = isConnected;
            }

            if (leftControllerIndex < OpenVR.k_unMaxTrackedDeviceCount) {
                SteamVR_Utils.RigidTransform pose = KerbalVR.GetDevicePose(leftControllerIndex);
                controllerObjL.transform.position = KerbalVR.DevicePoseToWorld(pose.pos);
                controllerObjL.transform.rotation = KerbalVR.DevicePoseToWorld(pose.rot);
            }
        }
    }
}
