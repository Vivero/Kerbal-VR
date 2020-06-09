using UnityEngine;
using Valve.VR;

namespace KerbalVR.Components {
    /// <summary>
    /// A part module that manages data flow to the vessel,
    /// and other modules. Start up on Flight only.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AvionicsComputer : MonoBehaviour {

        #region Private Members
        protected SteamVR_Action_Vector2 controlFlightStick;
        protected SteamVR_Action_Vector2 controlYawStick;
        protected SteamVR_Action_Vector2 controlThrottleStick;
        protected bool isVrFunctionalityInitialized = false;

        protected float commandThrottle = 0f;
        #endregion


        #region Properties
        public float YawAngle { get; private set; } = 0f;
        public float PitchAngle { get; private set; } = 0f;
        public float RollAngle { get; private set; } = 0f;
        #endregion


        #region Singleton
        /// <summary>
        /// This is a singleton class, and there must be exactly one GameObject with this Component in the scene.
        /// </summary>
        protected static AvionicsComputer _instance;
        public static AvionicsComputer Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<AvionicsComputer>();
                    if (_instance == null) {
                        Utils.LogError("The scene needs to have one active GameObject with an AvionicsComputer script attached!");
                    }
                    else {
                        _instance.Initialize();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// One-time initialization for this singleton class.
        /// </summary>
        protected void Initialize() {
            Utils.Log("AvionicsComputer initialized");
        }
        #endregion


        protected void InitializeVrFunctionality() {
            if (isVrFunctionalityInitialized) return;
            controlFlightStick = SteamVR_Input.GetVector2Action("flight", "FlightStick");
            controlYawStick = SteamVR_Input.GetVector2Action("flight", "YawStick");
            controlThrottleStick = SteamVR_Input.GetVector2Action("flight", "ThrottleStick");

            isVrFunctionalityInitialized = true;
        }

        protected void Start() {
            Utils.Log("AvionicsComputer Start");
            FlightGlobals.ActiveVessel.OnFlyByWire += VesselControl;

            // initialize the current throttle setting
            commandThrottle = FlightGlobals.ActiveVessel.ctrlState.mainThrottle;
        }

        protected void OnDestroy() {
            Utils.Log("AvionicsComputer shutting down...");
            FlightGlobals.ActiveVessel.OnFlyByWire -= VesselControl;
        }

        protected void Update() {
            if (!isVrFunctionalityInitialized && KerbalVR.Core.IsOpenVrReady) {
                InitializeVrFunctionality();
            }

            UpdateFlightData();
        }

        protected void UpdateFlightData() {
            Vessel activeVessel = FlightGlobals.ActiveVessel;
            if (activeVessel != null) {
                //
                // activeVessel.ReferenceTransform.up      : faces towards front (bow) of vessel
                // activeVessel.ReferenceTransform.forward : faces downwards (floor/nadir) of the vessel
                // activeVessel.ReferenceTransform.right   : faces towards right side (starboard) of vessel
                //

                // vector from celestial body to the vessel. this vector is normal to the surface of the body.
                Vector3d cbToVessel = activeVessel.GetWorldPos3D() - activeVessel.mainBody.position;
                Vector3d surfaceNormal = cbToVessel.normalized;

                // project the vessel's forward and right direction onto the surface normal
                Vector3 surfaceForward = Vector3.ProjectOnPlane(activeVessel.ReferenceTransform.up, surfaceNormal).normalized;
                Vector3 surfaceRight = Vector3.Cross(surfaceNormal, surfaceForward);

                // calculate roll angle as the angle between the vessel's right axis and the surface right axis
                RollAngle = -Vector3.SignedAngle(surfaceRight, activeVessel.ReferenceTransform.right, activeVessel.ReferenceTransform.up);

                // calculate the pitch angle as the angle between the vessel's forward axis and the surface forward axis
                PitchAngle = -Vector3.SignedAngle(surfaceForward, activeVessel.ReferenceTransform.up, surfaceRight);

                // project an always-north vector onto surface
                Vector3d surfaceNorth2 = (activeVessel.mainBody.rotation * Vector3d.up).normalized;

                // calculate yaw angle as angle between the north-facing vector on surface and the vessel's forward axis
                YawAngle = -Vector3.SignedAngle(surfaceForward, surfaceNorth2, surfaceNormal);
                if (YawAngle < 0f) {
                    YawAngle += 360f;
                }
            }
        }

        protected void VesselControl(FlightCtrlState state) {
            // do nothing without the OpenVR input system
            if (!isVrFunctionalityInitialized) {
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

    }
}
