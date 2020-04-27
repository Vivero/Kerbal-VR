using System.Collections;
using UnityEngine;

namespace KerbalVR.Components
{
    // start plugin at startup
    //
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KVR_AvionicsComputer : MonoBehaviour
    {
        private Coroutine outputSignalsCoroutine;

        private Events.Action stageUpdatedAction;
        private Events.Action sasUpdatedAction;
        private Events.Action precisionModeUpdatedAction;

        void Awake() {
            // start emitting signals to components
            outputSignalsCoroutine = StartCoroutine(OutputSignals());

            stageUpdatedAction = KerbalVR.Events.AvionicsIntAction("stage", OnStageInput);
            sasUpdatedAction = KerbalVR.Events.AvionicsIntAction("sas", OnSASInput);
            precisionModeUpdatedAction = KerbalVR.Events.AvionicsIntAction("precision_mode", OnPrecisionModeInput);
        }

        void Start() {
            // define the active vessel to control
            FlightGlobals.ActiveVessel.OnFlyByWire += VesselControl;
        }

        void OnEnable() {
            stageUpdatedAction.enabled = true;
            sasUpdatedAction.enabled = true;
            precisionModeUpdatedAction.enabled = true;
        }

        void OnDisable() {
            stageUpdatedAction.enabled = false;
            sasUpdatedAction.enabled = false;
            precisionModeUpdatedAction.enabled = false;
        }

        void OnDestroy() {
            Utils.Log("KVR_AvionicsComputer shutting down...");
        }

        IEnumerator OutputSignals() {
            while (true) {
                if (FlightGlobals.ActiveVessel != null) {

                    // send altitude information
                    float altitude = (float)FlightGlobals.ActiveVessel.altitude;
                    Events.AvionicsFloat("altitude").Send(altitude);

                    // send orbital information
                    float apoapsis = (float)FlightGlobals.ActiveVessel.orbit.ApA;
                    Events.AvionicsFloat("apoapsis").Send(apoapsis);

                    float periapsis = (float)FlightGlobals.ActiveVessel.orbit.PeA;
                    Events.AvionicsFloat("periapsis").Send(periapsis);
                }

                // wait for next update
                yield return new WaitForSeconds(1f);
            }
        }

        void OnStageInput(int signal) {
            if (signal != 0) {
                KSP.UI.Screens.StageManager.ActivateNextStage();
            }
        }

        void OnSASInput(int signal) {
            FlightGlobals.ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, signal != 0);
        }

        void OnPrecisionModeInput(int signal) {
            FlightInputHandler.fetch.precisionMode = (signal != 0);
        }

        void VesselControl(FlightCtrlState state) {
            bool isControlling = false;
            float yawControl = 0f;
            float pitchControl = 0f;
            float rollControl = 0f;
            string logMsg = "";

            SteamVR_Controller.Device rightHandState = KerbalVR.DeviceManager.GetManipulatorRightState();
            if (rightHandState != null) {
                logMsg += "=== RIGHT HAND ===\n";
                logMsg += "Axis0: " + rightHandState.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0) + (rightHandState.GetTouch(Valve.VR.EVRButtonId.k_EButton_Axis0) ? "pressed" : "") + "\n";
                logMsg += "Axis1: " + rightHandState.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis1) + (rightHandState.GetTouch(Valve.VR.EVRButtonId.k_EButton_Axis1) ? "pressed" : "") + "\n";
                logMsg += "Axis2: " + rightHandState.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis2) + (rightHandState.GetTouch(Valve.VR.EVRButtonId.k_EButton_Axis2) ? "pressed" : "") + "\n"; // index ctrl joystick
                logMsg += "Axis3: " + rightHandState.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis3) + (rightHandState.GetTouch(Valve.VR.EVRButtonId.k_EButton_Axis3) ? "pressed" : "") + "\n";
                logMsg += "JoyStick: " + rightHandState.GetAxis(Valve.VR.EVRButtonId.k_EButton_IndexController_JoyStick) + "\n";

                if (rightHandState.GetTouch(Valve.VR.EVRButtonId.k_EButton_Axis2)) {
                    Vector2 joystickState = rightHandState.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis2);
                    yawControl = joystickState.x;
                    pitchControl = joystickState.y;
                    isControlling = true;
                }
            }

            SteamVR_Controller.Device leftHandState = KerbalVR.DeviceManager.GetManipulatorLeftState();
            if (leftHandState != null) {
                logMsg += "=== LEFT HAND ===\n";
                logMsg += "Axis0: " + leftHandState.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0) + (leftHandState.GetTouch(Valve.VR.EVRButtonId.k_EButton_Axis0) ? "pressed" : "") + "\n";
                logMsg += "Axis1: " + leftHandState.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis1) + (leftHandState.GetTouch(Valve.VR.EVRButtonId.k_EButton_Axis1) ? "pressed" : "") + "\n";
                logMsg += "Axis2: " + leftHandState.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis2) + (leftHandState.GetTouch(Valve.VR.EVRButtonId.k_EButton_Axis2) ? "pressed" : "") + "\n"; // index ctrl joystick
                logMsg += "Axis3: " + leftHandState.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis3) + (leftHandState.GetTouch(Valve.VR.EVRButtonId.k_EButton_Axis3) ? "pressed" : "") + "\n";
                logMsg += "JoyStick: " + leftHandState.GetAxis(Valve.VR.EVRButtonId.k_EButton_IndexController_JoyStick) + "\n";

                if (leftHandState.GetTouch(Valve.VR.EVRButtonId.k_EButton_Axis2)) {
                    Vector2 joystickState = leftHandState.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis2);
                    rollControl = joystickState.x;
                    isControlling = true;
                }
            }

            if (isControlling) {
                state.yaw = yawControl;
                state.pitch = pitchControl;
                state.roll = rollControl;
            }

            Utils.SetDebugText(logMsg);
        }

    } // class KVR_AvionicsComputer
} // namespace KerbalVR
