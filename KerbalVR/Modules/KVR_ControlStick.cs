using UnityEngine;
using Valve.VR;

namespace KerbalVR.Modules
{
    public class KVR_ControlStick : InternalModule
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
        private bool isUnderControl; // stick is being operated by manipulator
        private bool isCommandingControl; // stick is allowed to control the vessel

        void Start() {
            // Utils.PrintGameObjectTree(gameObject);
            StickAxisX = 0f;
            StickAxisY = 0f;

            // obtain the collider
            stickColliderTransform = internalProp.FindModelTransform(transformStickCollider);
            if (stickColliderTransform != null) {
                stickColliderGameObject = stickColliderTransform.gameObject;
                stickColliderGameObject.AddComponent<KVR_ControlStickCollider>().controlStickComponent = this;
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

            isUnderControl = false;
            isCommandingControl = false;
        }

        void OnDestroy() {
            FlightGlobals.ActiveVessel.OnFlyByWire -= VesselControl;
        }

        void Update() {
            isCommandingControl = false;

            if (isUnderControl) {
                // calculate the delta position between the manipulator and the joystick
                Vector3 stickToManipulatorPos =
                    DeviceManager.Instance.ManipulatorRight.transform.position -
                    stickTransformGameObject.transform.position;

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

#if DEBUG
                if (DeviceManager.Instance.ManipulatorRight.State.GetPressDown(EVRButtonId.k_EButton_SteamVR_Touchpad)) {
                    Utils.Log("Delta Pos = " + stickToManipulatorDeltaPos.ToString("F3"));
                    Utils.Log("xAngle = " + xAngle.ToString("F3"));
                    Utils.Log("yAngle = " + yAngle.ToString("F3"));
                }
#endif


            } else {
                stickTransformGameObject.transform.rotation = stickInitialRotation;
                StickAxisX = 0f;
                StickAxisY = 0f;
            }

            /*if (DeviceManager.Instance.ManipulatorRight != null &&
                DeviceManager.Instance.ManipulatorRight.State.GetPressDown(EVRButtonId.k_EButton_SteamVR_Touchpad)) {
                Utils.CreateGizmoAtPosition(stickTransformGameObject.transform.position, stickTransformGameObject.transform.rotation);

                Utils.Log("StickAxisX = " + StickAxisX.ToString("F3"));
                Utils.Log("StickAxisY = " + StickAxisY.ToString("F3"));
            }*/
        }

        private void UpdateStick() {
            // detect when the Grip button has been pressed (this "grabs" the stick)
            if (DeviceManager.Instance.ManipulatorRight != null &&
                DeviceManager.Instance.ManipulatorRight.State.GetPressDown(EVRButtonId.k_EButton_Grip)) {
                Utils.Log("GRIP while inside stick collider!");

                isUnderControl = !isUnderControl;
            }
        }

        public void StickColliderStayed(GameObject colliderObject) {
            // Utils.Log("KVR_ControlStick inside " + colliderObject.name);

            if (colliderObject == stickColliderGameObject) {
                UpdateStick();
            }
        }

        private void VesselControl(FlightCtrlState state) {
            if (isCommandingControl) {
                state.roll = StickAxisX * 0.6f;
                state.pitch = -StickAxisY * 0.6f;
            }
        }
    }
}
