using System.Collections;
using UnityEngine;
using Valve.VR;
using KerbalVR.Components;

namespace KerbalVR.Modules
{
    public class KVR_Throttle : InternalModule, IActionableCollider
    {
        // TODO: naming scheme needs some revision

        #region KSP Config Fields
        [KSPField]
        public string transformHandleCollider = string.Empty;
        [KSPField]
        public string transformHandle = string.Empty;
        [KSPField]
        public float transformHandleAngleOffset = 0f;
        #endregion

        public float HandleAxis { get; private set; }

        private float handleAxisMin = 0f;
        private float handleAxisMax = 70f;
        private float handleDeadZoneAngle = 5f;

        private GameObject handleTransformGameObject;
        private Vector3 handleInitialPosition;
        private Quaternion handleInitialRotation;

        private Transform handleColliderTransform;
        private GameObject handleColliderGameObject;

        private bool isManipulatorInsideHandleCollider;
        private bool isUnderControl; // stick is being operated by manipulator
        private bool isCommandingControl; // stick is allowed to control the vessel

        // implement a button de-bounce
        private bool isInteractable = true;
        private float buttonCooldownTime = 0.3f;

        // event listeners
        Events.Action onManipulatorLeftUpdatedAction;
        Events.Action onManipulatorRightUpdatedAction;

        void Awake() {
            // define events to listen
            onManipulatorLeftUpdatedAction = KerbalVR.Events.ManipulatorLeftUpdatedAction(OnManipulatorLeftUpdated);
            onManipulatorRightUpdatedAction = KerbalVR.Events.ManipulatorRightUpdatedAction(OnManipulatorRightUpdated);
        }

        void Start() {
            HandleAxis = 0f;

            // obtain the collider
            handleColliderTransform = internalProp.FindModelTransform(transformHandleCollider);
            if (handleColliderTransform != null) {
                handleColliderGameObject = handleColliderTransform.gameObject;
                handleColliderGameObject.AddComponent<KVR_ActionableCollider>().module = this;
            } else {
                Utils.LogWarning("KVR_Throttle (" + gameObject.name + ") has no collider \"" + transformHandleCollider + "\"");
            }

            // obtain the transform for the actual control stick object
            Transform handleTransform = internalProp.FindModelTransform(transformHandle);
            if (handleTransform != null) {
                handleTransformGameObject = handleTransform.gameObject;
                handleInitialPosition = handleTransformGameObject.transform.position;
                handleInitialRotation = handleTransformGameObject.transform.rotation;
            } else {
                Utils.LogWarning("KVR_Throttle (" + gameObject.name + ") has no handle transform \"" + transformHandle + "\"");
            }

            // define the active vessel to control
            FlightGlobals.ActiveVessel.OnFlyByWire += VesselControl;

            isManipulatorInsideHandleCollider = false;
            isUnderControl = false;
            isCommandingControl = false;
        }

        void OnDestroy() {
            FlightGlobals.ActiveVessel.OnFlyByWire -= VesselControl;
        }

        void OnEnable() {
            onManipulatorLeftUpdatedAction.enabled = true;
            onManipulatorRightUpdatedAction.enabled = true;
        }

        void OnDisable() {
            onManipulatorLeftUpdatedAction.enabled = false;
            onManipulatorRightUpdatedAction.enabled = false;
        }

        void OnManipulatorLeftUpdated(SteamVR_Controller.Device state) {
            OnManipulatorUpdated(state);
        }

        void OnManipulatorRightUpdated(SteamVR_Controller.Device state) {
            OnManipulatorUpdated(state);
        }

        void OnManipulatorUpdated(SteamVR_Controller.Device state) {
            if (isInteractable && state.GetPressDown(EVRButtonId.k_EButton_Grip)) {

                if (isManipulatorInsideHandleCollider && !isUnderControl) {
                    isUnderControl = true;

                    // cool-down for button de-bounce
                    isInteractable = false;
                    StartCoroutine(ButtonCooldown());

                } else if (isUnderControl) {
                    isUnderControl = false;
                }

            }
        }

        void Update() {
            // keep track if we're actually sending commands
            //isCommandingControl = false;

            if (isUnderControl) {
                // calculate the delta position between the manipulator and the joystick
                Vector3 handleToManipulatorPos =
                    DeviceManager.Instance.ManipulatorRight.transform.position -
                    handleInitialPosition;

                // calculate the joystick X-axis angle
                Vector3 handleToManipulatorDeltaPos = handleInitialRotation * handleToManipulatorPos;
                float xAngle = Mathf.Atan2(handleToManipulatorDeltaPos.z, -handleToManipulatorDeltaPos.y);
                xAngle *= Mathf.Rad2Deg;
                xAngle -= transformHandleAngleOffset;
                xAngle *= -1f;
                xAngle = Mathf.Clamp(xAngle, handleAxisMin, handleAxisMax);
                Quaternion xRot = Quaternion.Euler(xAngle, 0f, 0f);

                // rotate the joystick into position
                Quaternion handleRotation = handleInitialRotation * xRot;
                handleTransformGameObject.transform.rotation = handleRotation;

                // perform vessel flight controls
                if (Mathf.Abs(xAngle) < handleDeadZoneAngle) {
                    isCommandingControl = false;
                    HandleAxis = 0f;
                } else {
                    isCommandingControl = true;
                    HandleAxis = (xAngle - handleAxisMin) / (handleAxisMax - handleAxisMin);
                }
            }
        }

        public void OnColliderEntered(Collider thisObject, Collider otherObject) {
            if (DeviceManager.IsManipulator(otherObject.gameObject)) {
                isManipulatorInsideHandleCollider = true;
            }
        }

        public void OnColliderExited(Collider thisObject, Collider otherObject) {
            if (thisObject.gameObject == handleColliderGameObject &&
                ((DeviceManager.Instance.ManipulatorRight != null &&
                otherObject.gameObject == DeviceManager.Instance.ManipulatorRight.gameObject) ||
                (DeviceManager.Instance.ManipulatorLeft != null &&
                otherObject.gameObject == DeviceManager.Instance.ManipulatorLeft.gameObject))) {

                isManipulatorInsideHandleCollider = false;
            }
        }

        private void VesselControl(FlightCtrlState state) {
            if (isCommandingControl) {
                state.mainThrottle = HandleAxis;
            }
        }

        private IEnumerator ButtonCooldown() {
            yield return new WaitForSeconds(buttonCooldownTime);
            isInteractable = true;
        }
    }
}
