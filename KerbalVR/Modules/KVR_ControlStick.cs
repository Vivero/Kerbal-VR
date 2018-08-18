using System;
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
        public float TwistAxis { get; private set; }

        private float stickAxisMaxX = 45f;
        private float stickAxisMaxY = 45f;
        private float stickDeadZoneAngle = 10f;
        private float rollDeadZoneRange = 0.1f;

        private GameObject stickTransformGameObject;
        private Vector3 stickInitialPosition;
        private Quaternion stickInitialRotation;

        private Transform stickColliderTransform;
        private GameObject stickColliderGameObject;

        private Manipulator attachedManipulator;
        private bool isManipulatorLeftInsideCollider;
        private bool isManipulatorRightInsideCollider;
        private bool isCommandingControl; // stick is allowed to control the vessel

        private ConfigNode moduleConfigNode;

        private GameObject[] emissiveObjects;
        private int numEmissiveObjects;
        private Color emissiveColor;

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
            // no setup needed in editor mode
            if (HighLogic.LoadedScene == GameScenes.EDITOR) return;

            // obtain module configuration
            moduleConfigNode = ConfigUtils.GetModuleConfigNode(internalProp.name, moduleID);

            // Utils.PrintGameObjectTree(gameObject);
            StickAxisX = 0f;
            StickAxisY = 0f;
            TwistAxis = 0f;

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

            // special effects
            ConfigNode emissiveConfigNode = moduleConfigNode.GetNode("KVR_EMISSIVE");
            if (emissiveConfigNode != null) {
                string[] emissiveObjectNames = emissiveConfigNode.GetValues("objectName");
                numEmissiveObjects = emissiveObjectNames.Length;
                emissiveObjects = new GameObject[numEmissiveObjects];
                for (int i = 0; i < numEmissiveObjects; i++) {
                    Transform emissiveTransform = internalProp.FindModelTransform(emissiveObjectNames[i]);
                    if (emissiveTransform != null) {
                        emissiveObjects[i] = emissiveTransform.gameObject;
                    } else {
                        Utils.LogWarning("KVR_ControlStick (" + gameObject.name + ") has no emissive transform \"" + emissiveObjectNames[i] + "\"");
                    }
                }

                emissiveColor = Color.white;
                bool success = emissiveConfigNode.TryGetValue("color", ref emissiveColor);
            }

            // define the active vessel to control
            FlightGlobals.ActiveVessel.OnFlyByWire += VesselControl;

            isManipulatorLeftInsideCollider = false;
            isManipulatorRightInsideCollider = false;
            isCommandingControl = false;
            attachedManipulator = null;

            SetEmissiveColor(emissiveColor);
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
            if (state.GetPressDown(EVRButtonId.k_EButton_Grip)) {
                /*Utils.Log("OnManipulatorLeftUpdated: " +
                    "attached = " + (attachedManipulator == null ? "null" : attachedManipulator.ToString()) +
                    ", isManipulatorLeftInsideStickCollider = " + (isManipulatorLeftInsideCollider ? "yes" : "no") +
                    ", state.GetPressDown = " + (state.GetPressDown(EVRButtonId.k_EButton_Grip) ? "yes" : "no"));*/

                if (isInteractable && (attachedManipulator == null) &&
                    isManipulatorLeftInsideCollider) {

                    attachedManipulator = DeviceManager.Instance.ManipulatorLeft;
                    DeviceManager.Instance.ManipulatorLeft.isGripping = true;

                    // cool-down for button de-bounce
                    isInteractable = false;
                    StartCoroutine(ButtonCooldown());

                } else if (DeviceManager.IsManipulatorLeft(attachedManipulator)) {
                    attachedManipulator = null;
                    DeviceManager.Instance.ManipulatorLeft.isGripping = false;
                }
            }
        }

        void OnManipulatorRightUpdated(SteamVR_Controller.Device state) {
            if (state.GetPressDown(EVRButtonId.k_EButton_Grip)) {
                /*Utils.Log("OnManipulatorRightUpdated: " +
                    "attached = " + (attachedManipulator == null ? "null" : attachedManipulator.ToString()) +
                    ", isManipulatorRightInsideStickCollider = " + (isManipulatorRightInsideCollider ? "yes" : "no") +
                    ", state.GetPressDown = " + (state.GetPressDown(EVRButtonId.k_EButton_Grip) ? "yes" : "no"));*/

                if (isInteractable && (attachedManipulator == null) &&
                    isManipulatorRightInsideCollider) {

                    attachedManipulator = DeviceManager.Instance.ManipulatorRight;
                    DeviceManager.Instance.ManipulatorRight.isGripping = true;

                    // cool-down for button de-bounce
                    isInteractable = false;
                    StartCoroutine(ButtonCooldown());

                } else if (DeviceManager.IsManipulatorRight(attachedManipulator)) {
                    attachedManipulator = null;
                    DeviceManager.Instance.ManipulatorRight.isGripping = false;
                }
            }
        }

        void Update() {
            // keep track if we're actually sending commands
            isCommandingControl = false;

            if (attachedManipulator != null) {
                // calculate the delta position between the manipulator and the joystick
                Vector3 stickToManipulatorPos =
                    attachedManipulator.GripPosition - stickTransformGameObject.transform.position;

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

                // detect roll control
                if (attachedManipulator.State != null) {
                    if (attachedManipulator.State.GetPress(EVRButtonId.k_EButton_SteamVR_Touchpad)) {
                        Vector2 touchAxis = attachedManipulator.State.GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad);
                        float xTouchAxis = touchAxis.x;

                        if (Mathf.Abs(xTouchAxis) < rollDeadZoneRange) {
                            TwistAxis = 0f;
                        } else {
                            isCommandingControl = true;
                            TwistAxis = xTouchAxis;
                        }
                    } else {
                        TwistAxis = 0f;
                    }
                } else {
                    TwistAxis = 0f;
                }

            } else {
                stickTransformGameObject.transform.rotation = stickInitialRotation;
                StickAxisX = 0f;
                StickAxisY = 0f;
                TwistAxis = 0f;
            }
        }

        private void SetEmissiveColor(Color emissiveColor) {
            for (int i = 0; i < numEmissiveObjects; i++) {
                MeshRenderer emissiveRenderer = emissiveObjects[i].GetComponent<MeshRenderer>();
                emissiveRenderer.material.SetColor("_EmissiveColor", emissiveColor);
            }
        }

        public void OnColliderEntered(Collider thisObject, Collider otherObject) {
            if (DeviceManager.IsManipulatorGripLeft(otherObject)) {
                isManipulatorLeftInsideCollider = true;
            }

            if (DeviceManager.IsManipulatorGripRight(otherObject)) {
                isManipulatorRightInsideCollider = true;
            }
        }

        public void OnColliderExited(Collider thisObject, Collider otherObject) {
            if (DeviceManager.IsManipulatorGripLeft(otherObject)) {
                isManipulatorLeftInsideCollider = false;
            }

            if (DeviceManager.IsManipulatorGripRight(otherObject)) {
                isManipulatorRightInsideCollider = false;
            }
        }

        private void VesselControl(FlightCtrlState state) {
            if (isCommandingControl) {
                state.yaw = TwistAxis;
                state.pitch = -StickAxisY;
                state.roll = StickAxisX;
            }
        }

        private IEnumerator ButtonCooldown() {
            yield return new WaitForSeconds(buttonCooldownTime);
            isInteractable = true;
        }
    }
}
