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
        private GameObject controllerObjR;

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
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceRoleChanged).Listen(OnTrackedDeviceRoleChanged);
        }

        void OnDisable() {
            SteamVR_Events.NewPoses.Remove(OnDevicePosesReady);
            SteamVR_Events.DeviceConnected.Remove(OnDeviceConnected);
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceRoleChanged).Remove(OnTrackedDeviceRoleChanged);
        }

        void Update() {

        }

        private void OnDevicePosesReady(TrackedDevicePose_t[] devicePoses) {

            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++) {
                bool isConnected = devicePoses[i].bDeviceIsConnected;
                if (isDeviceConnected[i] != isConnected) {
                    SteamVR_Events.DeviceConnected.Send((int)i, isConnected);
                }
                isDeviceConnected[i] = isConnected;
            }

            SteamVR_Controller.Update();


            if (controllerObjL == null) {
                controllerObjL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                controllerObjL.name = "VR_ControllerL";
                controllerObjL.transform.localScale = Vector3.one * 0.1f;
                controllerObjL.GetComponent<MeshRenderer>().material.color = Color.red;
            }
            controllerObjL.layer = Scene.RenderLayer;

            if (controllerObjR == null) {
                controllerObjR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                controllerObjR.name = "VR_ControllerR";
                controllerObjR.transform.localScale = Vector3.one * 0.1f;
                controllerObjR.GetComponent<MeshRenderer>().material.color = Color.green;
            }
            controllerObjR.layer = Scene.RenderLayer;

            
            if (DeviceIndexIsValid(controllerIndexL)) {
                SteamVR_Utils.RigidTransform pose = new SteamVR_Utils.RigidTransform(devicePoses[controllerIndexL].mDeviceToAbsoluteTracking);
                controllerObjL.transform.position = Scene.DevicePoseToWorld(pose.pos);
                controllerObjL.transform.rotation = Scene.DevicePoseToWorld(pose.rot);

                SteamVR_Controller.Device controllerL = SteamVR_Controller.Input((int)controllerIndexL);

                if (controllerL.GetTouch(EVRButtonId.k_EButton_SteamVR_Touchpad)) {
                    Vector2 touchAxis = SteamVR_Controller.Input((int)controllerIndexL).GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad);
                    Vector3 sceneForwardDir = Scene.InitialRotation * Vector3.forward;
                    Vector3 sceneRightDir = Scene.InitialRotation * Vector3.right;
                    sceneForwardDir.y = 0f;
                    sceneRightDir.y = 0f;
                    Scene.InitialPosition += sceneForwardDir.normalized * 2e-2f * touchAxis.y;
                    Scene.InitialPosition += sceneRightDir.normalized * 2e-2f * touchAxis.x;
                }
            }

            if (DeviceIndexIsValid(controllerIndexR)) {
                SteamVR_Utils.RigidTransform pose = new SteamVR_Utils.RigidTransform(devicePoses[controllerIndexR].mDeviceToAbsoluteTracking);
                controllerObjR.transform.position = Scene.DevicePoseToWorld(pose.pos);
                controllerObjR.transform.rotation = Scene.DevicePoseToWorld(pose.rot);

                SteamVR_Controller.Device controllerR = SteamVR_Controller.Input((int)controllerIndexR);

                if (controllerR.GetTouch(EVRButtonId.k_EButton_SteamVR_Touchpad)) {
                    Vector2 touchAxis = SteamVR_Controller.Input((int)controllerIndexR).GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad);
                    Scene.InitialPosition += Vector3.up * 2e-2f * touchAxis.y;
                }
            }

        }

        private void OnTrackedDeviceRoleChanged(VREvent_t vrEvent) {
            // re-check controller indices
            controllerIndexL = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
            controllerIndexR = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);

            Utils.LogInfo("controllerIndexL = " + controllerIndexL);
            Utils.LogInfo("controllerIndexR = " + controllerIndexR);
        }

        private void OnDeviceConnected(int deviceIndex, bool isConnected) {
            Utils.LogInfo("Device " + deviceIndex + " (" +
                OpenVR.System.GetTrackedDeviceClass((uint)deviceIndex) +
                ") is " + (isConnected ? "connected" : "disconnected"));
        }

        private bool DeviceIndexIsValid(uint deviceIndex) {
            return deviceIndex < OpenVR.k_unMaxTrackedDeviceCount;
        }
    }
}
