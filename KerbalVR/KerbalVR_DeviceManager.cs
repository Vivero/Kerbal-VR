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
        private uint controllerIndexL;
        private uint controllerIndexR;

        // this is a singleton class, and there must be one EventManager in the scene
        private static DeviceManager _instance;
        public static DeviceManager Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<DeviceManager>();
                    if (_instance == null) {
                        Utils.LogError("The scene needs to have one active GameObject with a DeviceManager script attached!");
                    } else {
                        _instance.Initialize();
                    }
                }
                return _instance;
            }
        }

        // first-time initialization for this singleton class
        private void Initialize() {
            if (isDeviceConnected == null) {
                isDeviceConnected = new bool[OpenVR.k_unMaxTrackedDeviceCount];
            }
        }

        void OnEnable() {
            SteamVR_Events.NewPoses.Listen(OnDevicePosesReady);
            SteamVR_Events.DeviceConnected.Listen(OnDeviceConnected);
        }

        void OnDisable() {
            SteamVR_Events.NewPoses.Remove(OnDevicePosesReady);
            SteamVR_Events.DeviceConnected.Remove(OnDeviceConnected);
        }

        void Update() {

        }

        private void OnDevicePosesReady(TrackedDevicePose_t[] devicePoses) {
            
            if (controllerObjL == null) {
                controllerObjL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                controllerObjL.name = "VR_ControllerL";
                controllerObjL.layer = 20;
                controllerObjL.transform.localScale = Vector3.one * 0.1f;
            }


            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++) {
                bool isConnected = devicePoses[i].bDeviceIsConnected;
                if (isDeviceConnected[i] != isConnected) {
                    SteamVR_Events.DeviceConnected.Send((int)i, isConnected);
                }
                isDeviceConnected[i] = isConnected;
            }

            if (controllerIndexL < OpenVR.k_unMaxTrackedDeviceCount) {
                SteamVR_Utils.RigidTransform pose = new SteamVR_Utils.RigidTransform(devicePoses[controllerIndexL].mDeviceToAbsoluteTracking);
                controllerObjL.transform.position = Scene.DevicePoseToWorld(pose.pos);
                controllerObjL.transform.rotation = Scene.DevicePoseToWorld(pose.rot);
            }
        }

        private void OnDeviceConnected(int deviceIndex, bool isConnected) {
            Utils.LogInfo("Device " + deviceIndex + " is " + (isConnected ? "connected" : "disconnected"));

            // re-check controller indices
            controllerIndexL = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
            controllerIndexR = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
        }
    }
}
