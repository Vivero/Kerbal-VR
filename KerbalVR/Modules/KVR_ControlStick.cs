using System.Collections;
using UnityEngine;
using Valve.VR;
using KerbalVR.Components;

namespace KerbalVR.Modules
{
    public class KVR_ControlStick : InternalModule, IActionableCollider
    {
        // TODO: naming scheme needs some revision

        #region KSP Config Fields
        [KSPField]
        public string transformStickCollider = string.Empty;
        [KSPField]
        public string transformStick = string.Empty;
        #endregion

        public float StickAxisX { get; private set; }
        public float StickAxisY { get; private set; }

        private float stickAxisMaxX = 45f;
        private float stickAxisMaxY = 45f;
        private float stickDeadZoneAngle = 10f;

        private GameObject stickTransformGameObject;
        private Vector3 stickInitialPosition;
        private Quaternion stickInitialRotation;

        private Transform stickColliderTransform;
        private GameObject stickColliderGameObject;

        private Manipulator attachedManipulator;
        private bool isManipulatorLeftInsideStickCollider;
        private bool isManipulatorRightInsideStickCollider;
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
            // Utils.PrintGameObjectTree(gameObject);
            StickAxisX = 0f;
            StickAxisY = 0f;

            // obtain the collider
            stickColliderTransform = internalProp.FindModelTransform(transformStickCollider);
            if (stickColliderTransform != null) {
                stickColliderGameObject = stickColliderTransform.gameObject;
                stickColliderGameObject.AddComponent<KVR_ActionableCollider>().module = this;
            } else {
                Utils.LogWarning("KVR_ControlStick (" + gameObject.name + ") has no collider \"" + transformStickCollider + "\"");
            }

            // obtain the transform for the actual control stick object
            Transform stickTransform = internalProp.FindModelTransform(transformStick);
            if (stickTransform != null) {
                stickTransformGameObject = stickTransform.gameObject;
                stickInitialPosition = stickTransformGameObject.transform.position;
                stickInitialRotation = stickTransformGameObject.transform.rotation;
            } else {
                Utils.LogWarning("KVR_ControlStick (" + gameObject.name + ") has no stick transform \"" + transformStick + "\"");
            }

            // define the active vessel to control
            FlightGlobals.ActiveVessel.OnFlyByWire += VesselControl;

            isManipulatorLeftInsideStickCollider = false;
            isManipulatorRightInsideStickCollider = false;
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
            if (isInteractable && isManipulatorLeftInsideStickCollider &&
                state.GetPressDown(EVRButtonId.k_EButton_Grip)) {
                if (!isUnderControl) {
                    attachedManipulator = DeviceManager.Instance.ManipulatorLeft;
                    isUnderControl = true;

                    // cool-down for button de-bounce
                    isInteractable = false;
                    StartCoroutine(ButtonCooldown());

                } else if (isUnderControl) {
                    attachedManipulator = null;
                    isUnderControl = false;
                }
            }
        }

        void OnManipulatorRightUpdated(SteamVR_Controller.Device state) {
            if (isInteractable && isManipulatorRightInsideStickCollider &&
                state.GetPressDown(EVRButtonId.k_EButton_Grip)) {
                if (!isUnderControl) {
                    attachedManipulator = DeviceManager.Instance.ManipulatorRight;
                    isUnderControl = true;

                    // cool-down for button de-bounce
                    isInteractable = false;
                    StartCoroutine(ButtonCooldown());

                } else if (isUnderControl) {
                    attachedManipulator = null;
                    isUnderControl = false;
                }
            }
        }

        void Update() {
            // keep track if we're actually sending commands
            isCommandingControl = false;

            if (isUnderControl && attachedManipulator != null) {
                // calculate the delta position between the manipulator and the joystick
                Vector3 stickToManipulatorPos =
                    attachedManipulator.transform.position - stickTransformGameObject.transform.position;

                // calculate the joystick X-axis angle
                Vector3 stickToManipulatorDeltaPos = stickInitialRotation * stickToManipulatorPos;
                float xAngle = Mathf.Atan2(stickToManipulatorDeltaPos.x, -stickToManipulatorDeltaPos.y);
                xAngle *= Mathf.Rad2Deg;
                xAngle = Mathf.Clamp(xAngle, -stickAxisMaxX, stickAxisMaxX);
                Quaternion xRot = Quaternion.Euler(0f, 0f, -xAngle);

                // calculate the joystick Y-axis angle
                float yAngle = Mathf.Atan2(stickToManipulatorDeltaPos.z, -stickToManipulatorDeltaPos.y);
                yAngle *= -Mathf.Rad2Deg;
                yAngle = Mathf.Clamp(yAngle, -stickAxisMaxY, stickAxisMaxY);
                Quaternion yRot = Quaternion.Euler(yAngle, 0f, 0f);

                // rotate the joystick into position
                Quaternion stickRotation = stickInitialRotation * yRot * xRot;
                stickTransformGameObject.transform.rotation = stickRotation;

                // perform vessel flight controls
                if (Mathf.Abs(xAngle) < stickDeadZoneAngle) {
                    StickAxisX = 0f;
                } else {
                    isCommandingControl = true;
                    StickAxisX = xAngle / stickAxisMaxX;
                }
                if (Mathf.Abs(yAngle) < stickDeadZoneAngle) {
                    StickAxisY = 0f;
                } else {
                    isCommandingControl = true;
                    StickAxisY = yAngle / stickAxisMaxY;
                }

            } else {
                stickTransformGameObject.transform.rotation = stickInitialRotation;
                StickAxisX = 0f;
                StickAxisY = 0f;
            }
        }

        public void OnColliderEntered(Collider thisObject, Collider otherObject) {
            if (thisObject.gameObject == stickColliderGameObject) {
                isManipulatorLeftInsideStickCollider = DeviceManager.IsManipulatorLeft(otherObject.gameObject);
                isManipulatorRightInsideStickCollider = DeviceManager.IsManipulatorRight(otherObject.gameObject);
            }
        }

        public void OnColliderExited(Collider thisObject, Collider otherObject) {
            if (thisObject.gameObject == stickColliderGameObject) {
                isManipulatorLeftInsideStickCollider = DeviceManager.IsManipulatorLeft(otherObject.gameObject);
                isManipulatorRightInsideStickCollider = DeviceManager.IsManipulatorRight(otherObject.gameObject);
            }
        }

        private void VesselControl(FlightCtrlState state) {
            if (isCommandingControl) {
                state.yaw = StickAxisX * 0.3f;
                state.pitch = -StickAxisY * 0.3f;
            }
        }

        private IEnumerator ButtonCooldown() {
            yield return new WaitForSeconds(buttonCooldownTime);
            isInteractable = true;
        }
    }
}
