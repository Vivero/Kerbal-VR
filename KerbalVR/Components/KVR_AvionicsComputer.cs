using System.Collections;
using UnityEngine;

namespace KerbalVR.Components
{
    // start plugin at startup
    //
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KVR_AvionicsComputer : MonoBehaviour
    {
        private Coroutine outputSignalsFunction;

        private Events.Action stageUpdatedAction;
        private Events.Action sasUpdatedAction;

        void Awake() {
            // Utils.Log("KVR_AvionicsComputer booting up.");

            outputSignalsFunction = StartCoroutine(OutputSignals());

            stageUpdatedAction = KerbalVR.Events.AvionicsAction("stage", OnStageInput);
            sasUpdatedAction = KerbalVR.Events.AvionicsAction("sas", OnSASInput);
        }

        void Start() {
            // define the active vessel to control
            // FlightGlobals.ActiveVessel.OnFlyByWire += VesselControl;
        }

        void OnEnable() {
            if (stageUpdatedAction != null) {
                stageUpdatedAction.enabled = true;
            }
            if (sasUpdatedAction != null) {
                sasUpdatedAction.enabled = true;
            }
        }

        void OnDisable() {
            if (stageUpdatedAction != null) {
                stageUpdatedAction.enabled = false;
            }
            if (sasUpdatedAction != null) {
                sasUpdatedAction.enabled = false;
            }
        }

        void OnDestroy() {
            // Utils.Log("KVR_AvionicsComputer closing.");
        }

        IEnumerator OutputSignals() {
            while (true) {
                // send altitude information
                if (FlightGlobals.ActiveVessel != null) {
                    float altitude = (float)FlightGlobals.ActiveVessel.altitude;
                    Events.Avionics("altitude").Send(altitude);

                    // send orbital information
                    float apoapsis = (float)FlightGlobals.ActiveVessel.orbit.ApA;
                    Events.Avionics("apoapsis").Send(apoapsis);

                    float periapsis = (float)FlightGlobals.ActiveVessel.orbit.PeA;
                    Events.Avionics("periapsis").Send(periapsis);
                }

                // wait for next update
                yield return new WaitForSeconds(1f);
            }
        }

        void OnStageInput(float signal) {
            Utils.Log("OnStageInput = " + signal);
            if (signal < 0.5f) {
                KSP.UI.Screens.StageManager.ActivateNextStage();
            }
        }

        void OnSASInput(float signal) {
            Utils.Log("OnSASInput = " + signal);
            if (signal < 0.5f) {
                FlightGlobals.ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, signal < 0.5f);
            }
        }
    } // class KVR_AvionicsComputer
} // namespace KerbalVR
