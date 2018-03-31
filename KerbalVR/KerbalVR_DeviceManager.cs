using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    public class DeviceManager : MonoBehaviour
    {
        #region Properties
        // Manipulator objects
        public Manipulator ManipulatorLeft { get; private set; }
        public Manipulator ManipulatorRight { get; private set; }

        // Manipulator object properties
        private float _manipulatorSize = 0.02f;
        public float ManipulatorSize {
            get {
                return _manipulatorSize;
            }
            set {
                _manipulatorSize = value;
                SetManipulatorSize(_manipulatorSize);
            }
        }

        // keep aliases of controller indices
        public uint ControllerIndexLeft { get; private set; }
        public uint ControllerIndexRight { get; private set; }
        #endregion


        #region Private Members
        // keep track of devices that are connected
        private bool[] isDeviceConnected = new bool[OpenVR.k_unMaxTrackedDeviceCount];

        // Manipulator Game Objects
        private GameObject manipulatorLeft;
        private GameObject manipulatorRight;
        #endregion


        #region Singleton
        // this is a singleton class, and there must be one DeviceManager in the scene
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

            ControllerIndexLeft = OpenVR.k_unTrackedDeviceIndexInvalid;
            ControllerIndexRight = OpenVR.k_unTrackedDeviceIndexInvalid;
        }
        #endregion

        void OnEnable() {
            SteamVR_Events.NewPoses.Listen(OnDevicePosesReady);
            SteamVR_Events.DeviceConnected.Listen(OnDeviceConnected);
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceRoleChanged).Listen(OnTrackedDeviceRoleChanged);
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceUpdated).Listen(OnTrackedDeviceRoleChanged);

        }

        void OnDisable() {
            SteamVR_Events.NewPoses.Remove(OnDevicePosesReady);
            SteamVR_Events.DeviceConnected.Remove(OnDeviceConnected);
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceRoleChanged).Remove(OnTrackedDeviceRoleChanged);
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceUpdated).Remove(OnTrackedDeviceRoleChanged);
        }

        private void OnDevicePosesReady(TrackedDevicePose_t[] devicePoses) {
            // detect devices that have (dis)connected
            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++) {
                bool isConnected = devicePoses[i].bDeviceIsConnected;
                if (isDeviceConnected[i] != isConnected) {
                    SteamVR_Events.DeviceConnected.Send((int)i, isConnected);
                }
                isDeviceConnected[i] = isConnected;
            }

            OnTrackedDeviceRoleChanged(); //the events are NOT trustworthy!

            // update poses for tracked devices
            SteamVR_Controller.Update();

            // update Manipulator objects' state
            if (DeviceIndexIsValid(ControllerIndexLeft)) {
                SteamVR_Utils.RigidTransform controllerPose = new SteamVR_Utils.RigidTransform(
                    devicePoses[ControllerIndexLeft].mDeviceToAbsoluteTracking);
                SteamVR_Controller.Device controllerState =
                    SteamVR_Controller.Input((int)ControllerIndexLeft);

                // state is stored in Manipulator object
                ManipulatorLeft.UpdateState(controllerPose, controllerState);

                // notify listeners
                Events.ManipulatorLeftUpdated.Send(controllerState);
            }

            if (DeviceIndexIsValid(ControllerIndexRight)) {
                SteamVR_Utils.RigidTransform controllerPose = new SteamVR_Utils.RigidTransform(
                    devicePoses[ControllerIndexRight].mDeviceToAbsoluteTracking);
                SteamVR_Controller.Device controllerState =
                    SteamVR_Controller.Input((int)ControllerIndexRight);

                // state is stored in Manipulator object
                ManipulatorRight.UpdateState(controllerPose, controllerState);

                // notify listeners
                Events.ManipulatorRightUpdated.Send(controllerState);
            }
        }

        private void OnTrackedDeviceRoleChanged(VREvent_t vrEvent) {
            OnTrackedDeviceRoleChanged();
        }

        private void OnTrackedDeviceRoleChanged() {
            // re-check controller indices
            ControllerIndexLeft = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
            ControllerIndexRight = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);

            ManageManipulators();
        }


        private void OnDeviceConnected(int deviceIndex, bool isConnected) {
            Utils.Log("Device " + deviceIndex + " (" +
                OpenVR.System.GetTrackedDeviceClass((uint)deviceIndex) +
                ") is " + (isConnected ? "connected" : "disconnected"));
        }

        private bool DeviceIndexIsValid(uint deviceIndex) {
            // return deviceIndex < OpenVR.k_unMaxTrackedDeviceCount;
            return deviceIndex != OpenVR.k_unTrackedDeviceIndexInvalid;
        }

        private GameObject CreateManipulator(ETrackedControllerRole role) {
            // create new GameObject
            GameObject manipulator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            manipulator.name = "KVR_Manipulator_" + role.ToString();
            DontDestroyOnLoad(manipulator);

            // define the render model
            manipulator.transform.localScale = Vector3.one * ManipulatorSize;
            Color manipulatorColor = (role == ETrackedControllerRole.RightHand) ? Color.green : Color.red;
            MeshRenderer manipulatorRenderer = manipulator.GetComponent<MeshRenderer>();
            manipulatorRenderer.material.color = manipulatorColor;

            // define the collider
            Rigidbody manipulatorRigidbody = manipulator.AddComponent<Rigidbody>();
            manipulatorRigidbody.isKinematic = true;
            SphereCollider manipulatorCollider = manipulator.GetComponent<SphereCollider>();
            manipulatorCollider.isTrigger = true;

            // define the Manipulator component
            Manipulator manipulatorComponent = manipulator.AddComponent<Manipulator>();
            manipulatorComponent.role = role;
            manipulatorComponent.defaultColor = manipulatorColor;
            manipulatorComponent.activeColor = Color.yellow;
            
            manipulatorRenderer.enabled = false;

            return manipulator;
        }

        private void ManageManipulators() {
            if (DeviceIndexIsValid(ControllerIndexLeft) && manipulatorLeft == null) {
                manipulatorLeft = CreateManipulator(ETrackedControllerRole.LeftHand);
                ManipulatorLeft = manipulatorLeft.GetComponent<Manipulator>();
            } else if (!DeviceIndexIsValid(ControllerIndexLeft) && manipulatorLeft != null) {
                Destroy(manipulatorLeft);
            }

            if (DeviceIndexIsValid(ControllerIndexRight) && manipulatorRight == null) {
                manipulatorRight = CreateManipulator(ETrackedControllerRole.RightHand);
                ManipulatorRight = manipulatorRight.GetComponent<Manipulator>();
            } else if (!DeviceIndexIsValid(ControllerIndexRight) && manipulatorRight != null) {
                Destroy(manipulatorRight);
            }
        }

        private void SetManipulatorSize(float size) {
            if (manipulatorLeft != null) {
                manipulatorLeft.transform.localScale = Vector3.one * ManipulatorSize;
            }
            if (manipulatorRight != null) {
                manipulatorRight.transform.localScale = Vector3.one * ManipulatorSize;
            }
        }

        public static bool IsManipulatorLeft(GameObject obj) {
            return Instance.manipulatorLeft != null && obj == Instance.manipulatorLeft;
        }

        public static bool IsManipulatorRight(GameObject obj) {
            return Instance.manipulatorRight != null && obj == Instance.manipulatorRight;
        }

        public static bool IsManipulator(GameObject obj) {
            return (Instance.manipulatorLeft != null && obj == Instance.manipulatorLeft) ||
                (Instance.manipulatorRight != null && obj == Instance.manipulatorRight);
        }
    } // class DeviceManager
} // namespace KerbalVR
