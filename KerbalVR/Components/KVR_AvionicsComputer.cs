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

        void Awake() {
            Utils.Log("KVR_AvionicsComputer booting up.");

            outputSignalsCoroutine = StartCoroutine(OutputSignals());

            stageUpdatedAction = KerbalVR.Events.AvionicsIntAction("stage", OnStageInput);
            sasUpdatedAction = KerbalVR.Events.AvionicsIntAction("sas", OnSASInput);
        }

        void Start() {
            // define the active vessel to control
            // FlightGlobals.ActiveVessel.OnFlyByWire += VesselControl;
        }

        void OnEnable() {
            stageUpdatedAction.enabled = true;
            sasUpdatedAction.enabled = true;
        }

        void OnDisable() {
            stageUpdatedAction.enabled = false;
            sasUpdatedAction.enabled = false;
        }

        void OnDestroy() {
            Utils.Log("KVR_AvionicsComputer shutting down...");
        }

        IEnumerator OutputSignals() {
            while (true) {
                // send altitude information
                if (FlightGlobals.ActiveVessel != null) {
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

    } // class KVR_AvionicsComputer
} // namespace KerbalVR
