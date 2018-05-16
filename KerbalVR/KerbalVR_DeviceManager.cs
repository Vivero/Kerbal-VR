using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    /// <summary>
    /// DeviceManager handles events from the OpenVR dispatcher to handle
    /// changes and state updates to tracked devices. It keeps convenient
    /// references to the two controller "hands" (the so-named Manipulators).
    /// </summary>
    public class DeviceManager : MonoBehaviour
    {
        #region Constants
        public readonly Vector3 GLOVE_POSITION = new Vector3(0f, 0.02f, -0.1f);
        public readonly Vector3 GLOVE_ROTATION = new Vector3(-45f, 0f, 90f);
        #endregion


        #region Properties
        // Manipulator objects
        public Manipulator ManipulatorLeft { get; private set; }
        public Manipulator ManipulatorRight { get; private set; }

        // Manipulator object properties
        private float _manipulatorSize = 0.45f;
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
        

        /// <summary>
        /// When this GameObject is enabled, listen to OpenVR events.
        /// </summary>
        protected void OnEnable() {
            SteamVR_Events.NewPoses.Listen(OnDevicePosesReady);
            SteamVR_Events.DeviceConnected.Listen(OnDeviceConnected);
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceRoleChanged).Listen(OnTrackedDeviceRoleChanged);
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceUpdated).Listen(OnTrackedDeviceRoleChanged);

        }

        /// <summary>
        /// When this GameObject is disabled, stop listening to OpenVR events.
        /// </summary>
        protected void OnDisable() {
            SteamVR_Events.NewPoses.Remove(OnDevicePosesReady);
            SteamVR_Events.DeviceConnected.Remove(OnDeviceConnected);
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceRoleChanged).Remove(OnTrackedDeviceRoleChanged);
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceUpdated).Remove(OnTrackedDeviceRoleChanged);
        }

        /// <summary>
        /// Callback for NewPoses event. Check whether devices have (dis)connected,
        /// update their state, and manage the Manipulator objects.
        /// </summary>
        /// <param name="devicePoses">The data structure containing the current state of all devices.</param>
        protected void OnDevicePosesReady(TrackedDevicePose_t[] devicePoses) {
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

                if (controllerState != null) {
                    // state is stored in Manipulator object
                    ManipulatorLeft.UpdateState(controllerPose, controllerState);

                    // notify listeners
                    Events.ManipulatorLeftUpdated.Send(controllerState);
                }
            }

            if (DeviceIndexIsValid(ControllerIndexRight)) {
                SteamVR_Utils.RigidTransform controllerPose = new SteamVR_Utils.RigidTransform(
                    devicePoses[ControllerIndexRight].mDeviceToAbsoluteTracking);
                SteamVR_Controller.Device controllerState =
                    SteamVR_Controller.Input((int)ControllerIndexRight);

                if (controllerState != null) {
                    // state is stored in Manipulator object
                    ManipulatorRight.UpdateState(controllerPose, controllerState);

                    // notify listeners
                    Events.ManipulatorRightUpdated.Send(controllerState);
                }
            }
        }

        /// <summary>
        /// Callback for the VREvent_TrackedDeviceRoleChanged and 
        /// VREvent_TrackedDeviceUpdated events. Checks whether the "left"
        /// and "right" controllers have changed.
        /// </summary>
        /// <param name="vrEvent">The OpenVR event data.</param>
        protected void OnTrackedDeviceRoleChanged(VREvent_t vrEvent) {
            OnTrackedDeviceRoleChanged();
        }

        private void OnTrackedDeviceRoleChanged() {
            // re-check controller indices
            ControllerIndexLeft = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
            ControllerIndexRight = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);

            ManageManipulators();
        }

        /// <summary>
        /// Callback for the DeviceConnected event.
        /// </summary>
        /// <param name="deviceIndex">The OpenVR-assigned index of this device.</param>
        /// <param name="isConnected">True if the device is connected, false otherwise.</param>
        protected void OnDeviceConnected(int deviceIndex, bool isConnected) {
            Utils.Log("Device " + deviceIndex + " (" +
                OpenVR.System.GetTrackedDeviceClass((uint)deviceIndex) +
                ") is " + (isConnected ? "connected" : "disconnected"));
        }

        /// <summary>
        /// Checks whether a given device index is a valid index number.
        /// </summary>
        /// <param name="deviceIndex">The OpenVR device index number.</param>
        /// <returns>True if index is valid, false otherwise.</returns>
        public static bool DeviceIndexIsValid(uint deviceIndex) {
            return deviceIndex != OpenVR.k_unTrackedDeviceIndexInvalid;
        }

        /// <summary>
        /// Creates a GameObject that represents the left or right device controllers.
        /// These are the "VR hands".
        /// </summary>
        /// <param name="role">The controller device role (left or right).</param>
        /// <returns>The GameObject for the "VR hand".</returns>
        protected GameObject CreateManipulator(ETrackedControllerRole role) {
            // create new GameObject
            GameObject manipulator = new GameObject("KVR_Manipulator_" + role.ToString());
            DontDestroyOnLoad(manipulator);

            // define the render model
            GameObject glovePrefab = AssetLoader.Instance.GetGameObject("GlovePrefab");
            if (glovePrefab == null) {
                Utils.LogError("GameObject \"GlovePrefab\" was not found!");
                return manipulator;
            }
            GameObject gloveObject = Instantiate(glovePrefab);
            gloveObject.transform.SetParent(manipulator.transform);
            Vector3 gloveObjectScale = Vector3.one * ManipulatorSize;
            if (role == ETrackedControllerRole.RightHand) {
                gloveObjectScale.y *= -1f;
            }
            gloveObject.transform.localPosition = GLOVE_POSITION;
            gloveObject.transform.localRotation = Quaternion.Euler(GLOVE_ROTATION);
            gloveObject.transform.localScale = gloveObjectScale;
            Utils.SetLayer(gloveObject, 20);

            // define the colliders
            Transform colliderObject = gloveObject.transform.Find("HandDummy/Arm Bone L/Wrist Bone L/Finger Index Bone L1/Finger Index Bone L2/Finger Index Bone L3/Finger Index Bone L4");
            if (colliderObject == null) {
                Utils.LogWarning("Manipulator is missing fingertip collider child object");
                return manipulator;
            }
            SphereCollider fingertipCollider = colliderObject.GetComponent<SphereCollider>();

            colliderObject = gloveObject.transform.Find("HandDummy/Arm Bone L/Wrist Bone L");
            if (colliderObject == null) {
                Utils.LogWarning("Manipulator is missing grip collider child object");
                return manipulator;
            }
            CapsuleCollider gripCollider = colliderObject.GetComponent<CapsuleCollider>();
            

            // retrieve the animator
            Animator manipulatorAnimator = gloveObject.GetComponent<Animator>();

            // define the Manipulator component
            Manipulator manipulatorComponent = manipulator.AddComponent<Manipulator>();
            manipulatorComponent.role = role;
            manipulatorComponent.fingertipCollider = fingertipCollider;
            manipulatorComponent.gripCollider = gripCollider;
            manipulatorComponent.manipulatorAnimator = manipulatorAnimator;

#if DEBUG
            GameObject manipulatorGizmo = Utils.CreateGizmo();
            manipulatorGizmo.transform.SetParent(manipulator.transform);
            manipulatorGizmo.transform.localPosition = Vector3.zero;
            manipulatorGizmo.transform.localRotation = Quaternion.identity;
            manipulatorGizmo.transform.localScale = Vector3.one * 0.5f;
#endif

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

        /// <summary>
        /// Sets the size of each "VR hand".
        /// </summary>
        /// <param name="size">Size of the hand, in meters.</param>
        protected void SetManipulatorSize(float size) {
            if (manipulatorLeft != null) {
                manipulatorLeft.transform.localScale = Vector3.one * ManipulatorSize;
            }
            if (manipulatorRight != null) {
                manipulatorRight.transform.localScale = Vector3.one * ManipulatorSize;
            }
        }

        /// <summary>
        /// Checks whether the given Collider is the left Manipulator's fingertip.
        /// </summary>
        /// <param name="collider">The Collider to check.</param>
        /// <returns>True if this is the left Manipulator's fingertip collider, false otherwise.</returns>
        public static bool IsManipulatorFingertipLeft(Collider collider) {
            return Instance.manipulatorLeft != null && collider == Instance.ManipulatorLeft.fingertipCollider;
        }

        /// <summary>
        /// Checks whether the given Collider is the left Manipulator's grip.
        /// </summary>
        /// <param name="collider">The Collider to check.</param>
        /// <returns>True if this is the left Manipulator's grip collider, false otherwise.</returns>
        public static bool IsManipulatorGripLeft(Collider collider) {
            return Instance.manipulatorLeft != null && collider == Instance.ManipulatorLeft.gripCollider;
        }

        /// <summary>
        /// Checks whether the given Manipulator component is the left Manipulator.
        /// </summary>
        /// <param name="obj">The Manipulator component to check.</param>
        /// <returns>True if this is the left Manipulator, false otherwise.</returns>
        public static bool IsManipulatorLeft(Manipulator obj) {
            return Instance.ManipulatorLeft != null && obj == Instance.ManipulatorLeft;
        }

        /// <summary>
        /// Checks whether the given Collider is the right Manipulator's fingertip.
        /// </summary>
        /// <param name="collider">The Collider to check.</param>
        /// <returns>True if this is the right Manipulator's fingertip collider, false otherwise.</returns>
        public static bool IsManipulatorFingertipRight(Collider collider) {
            return Instance.manipulatorRight != null && collider == Instance.ManipulatorRight.fingertipCollider;
        }

        /// <summary>
        /// Checks whether the given Collider is the right Manipulator's grip.
        /// </summary>
        /// <param name="collider">The Collider to check.</param>
        /// <returns>True if this is the right Manipulator's grip collider, false otherwise.</returns>
        public static bool IsManipulatorGripRight(Collider collider) {
            return Instance.manipulatorRight != null && collider == Instance.ManipulatorRight.gripCollider;
        }

        /// <summary>
        /// Checks whether the given Manipulator component is the right Manipulator.
        /// </summary>
        /// <param name="obj">The Manipulator component to check.</param>
        /// <returns>True if this is the right Manipulator, false otherwise.</returns>
        public static bool IsManipulatorRight(Manipulator obj) {
            return Instance.ManipulatorRight != null && obj == Instance.ManipulatorRight;
        }

        /// <summary>
        /// Checks whether the given Collider is a Manipulator fingertip collider.
        /// </summary>
        /// <param name="collider">The Collider to check.</param>
        /// <returns>True if this is a Manipulator fingertip collider, false otherwise.</returns>
        public static bool IsManipulatorFingertip(Collider collider) {
            return (Instance.manipulatorLeft != null && collider == Instance.ManipulatorLeft.fingertipCollider) ||
                (Instance.manipulatorRight != null && collider == Instance.ManipulatorRight.fingertipCollider);
        }

        /// <summary>
        /// Checks whether the given Collider is a Manipulator grip collider.
        /// </summary>
        /// <param name="collider">The Collider to check.</param>
        /// <returns>True if this is a Manipulator grip collider, false otherwise.</returns>
        public static bool IsManipulatorGrip(Collider collider) {
            return (Instance.manipulatorLeft != null && collider == Instance.ManipulatorLeft.gripCollider) ||
                (Instance.manipulatorRight != null && collider == Instance.ManipulatorRight.gripCollider);
        }

        public static SteamVR_Controller.Device GetManipulatorLeftState() {
            return (Instance.ManipulatorLeft != null) ? Instance.ManipulatorLeft.State : null;
        }

        public static SteamVR_Controller.Device GetManipulatorRightState() {
            return (Instance.ManipulatorRight != null) ? Instance.ManipulatorRight.State : null;
        }

    } // class DeviceManager
} // namespace KerbalVR
