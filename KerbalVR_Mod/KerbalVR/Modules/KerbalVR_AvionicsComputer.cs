using UnityEngine;
using Valve.VR;

namespace KerbalVR.Modules {
    /// <summary>
    /// A part module that manages data flow to the vessel,
    /// and other modules. Start up on Flight only.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KVR_AvionicsComputer : MonoBehaviour {

        #region Private Members
        protected SteamVR_Action_Vector2 controlFlightStick;
        protected SteamVR_Action_Vector2 controlYawStick;
        protected SteamVR_Action_Vector2 controlThrottleStick;
        protected bool isInitialized = false;

        protected float commandThrottle = 0f;
        #endregion

        protected void Initialize() {
            if (isInitialized) return;
            controlFlightStick = SteamVR_Input.GetVector2Action("flight", "FlightStick");
            controlYawStick = SteamVR_Input.GetVector2Action("flight", "YawStick");
            controlThrottleStick = SteamVR_Input.GetVector2Action("flight", "ThrottleStick");

            isInitialized = true;
            Utils.Log("KVR_AvionicsComputer initialized");
        }

        protected void Start() {
            Utils.Log("KVR_AvionicsComputer Start");
            FlightGlobals.ActiveVessel.OnFlyByWire += VesselControl;

            // initialize the current throttle setting
            commandThrottle = FlightGlobals.ActiveVessel.ctrlState.mainThrottle;
        }

        protected void OnDestroy() {
            Utils.Log("KVR_AvionicsComputer shutting down...");
            FlightGlobals.ActiveVessel.OnFlyByWire -= VesselControl;
        }

        protected void Update() {
            if (!isInitialized && KerbalVR.Core.IsOpenVrReady) {
                Initialize();
            }
            if (!isInitialized) {
                return;
            }
        }

        protected void VesselControl(FlightCtrlState state) {
            // do nothing without the OpenVR input system
            if (!isInitialized) {
                return;
            }

            // `state` contains the player's current control inputs

            bool isControllingVessel = false;
            float commandYaw = 0f;
            float commandPitch = 0f;
            float commandRoll = 0f;

            // get flight stick inputs
            Vector2 stickPos = controlFlightStick.axis;
            if (controlFlightStick.axis != Vector2.zero) {
                commandPitch = stickPos.y;
                if (KerbalVR.Configuration.Instance.SwapYawRollControls) {
                    commandYaw = stickPos.x;
                } else {
                    commandRoll = stickPos.x;
                }
                isControllingVessel = true;
            }

            // get yaw stick inputs
            stickPos = controlYawStick.axis;
            if (controlYawStick.axis != Vector2.zero) {
                if (KerbalVR.Configuration.Instance.SwapYawRollControls) {
                    commandRoll = stickPos.x;
                }
                else {
                    commandYaw = stickPos.x;
                }
                isControllingVessel = true;
            }

            // get throttle control inputs
            //
            // currently, the throttle control cannot consolidate the
            // user's throttle input together with the VR controller input.
            // we'll just have to make it so that the VR controller throttle
            // input overrides whatever the player might be trying to
            // control via keyboard/gamepad/etc.
            //
            stickPos = controlThrottleStick.axis;
            if (controlThrottleStick.changed) {
                commandThrottle += stickPos.y;
            }
            commandThrottle = Mathf.Clamp(commandThrottle, 0f, 1f);

            // only actuate the vessel control if player is commanding inputs on the controller
            if (isControllingVessel) {
                state.yaw = commandYaw;
                state.pitch = commandPitch;
                state.roll = commandRoll;
            }

            // override throttle (if config allows)
            if (KerbalVR.Configuration.Instance.EnableThrottleControl) {
                state.mainThrottle = Mathf.Clamp(commandThrottle, 0f, 1f);
            }
        }

    } // class KVR_AvionicsComputer
} // namespace KerbalVR
