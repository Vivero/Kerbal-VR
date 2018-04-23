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

        private Manipulator attachedManipulator;
        private bool isManipulatorLeftInsideCollider;
        private bool isManipulatorRightInsideCollider;
        private bool isCommandingControl; // stick is allowed to control the vessel

        private ConfigNode moduleConfigNode;

        private GameObject[] emissiveObjects;
        private int numEmissiveObjects;
        private Color emissiveColorDefault = Color.white;
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
                        Utils.LogWarning("KVR_Throttle (" + gameObject.name + ") has no emissive transform \"" + emissiveObjectNames[i] + "\"");
                    }
                }

                emissiveColor = Color.white;
                bool success = emissiveConfigNode.TryGetValue("color", ref emissiveColor);
            }

            // define the active vessel to control
            FlightGlobals.ActiveVessel.OnFlyByWire += VesselControl;

            HandleAxis = 0f;
            isManipulatorLeftInsideCollider = false;
            isManipulatorRightInsideCollider = false;
            isCommandingControl = false;
            attachedManipulator = null;

            SetEmissiveColor(emissiveColorDefault);
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
            //isCommandingControl = false;

            if (attachedManipulator != null) {
                // calculate the delta position between the manipulator and the joystick
                Vector3 handleToManipulatorPos =
                    attachedManipulator.GripPosition - handleInitialPosition;

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

                    SetEmissiveColor(Color.white);
                } else {
                    isCommandingControl = true;
                    HandleAxis = (xAngle - handleAxisMin) / (handleAxisMax - handleAxisMin);
                    
                    SetEmissiveColor(Color.Lerp(emissiveColorDefault, emissiveColor, HandleAxis));
                }
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
                state.mainThrottle = HandleAxis;
            }
        }

        private IEnumerator ButtonCooldown() {
            yield return new WaitForSeconds(buttonCooldownTime);
            isInteractable = true;
        }
    }
}
