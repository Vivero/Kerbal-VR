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

        void Awake() {
            Utils.Log("KVR_AvionicsComputer booting up.");

            outputSignalsFunction = StartCoroutine(OutputSignals());
        }

        void OnDestroy() {
            Utils.Log("KVR_AvionicsComputer closing.");
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

                    //Utils.Log("altitude = " + altitude.ToString("F3"));
                    //Utils.Log("apoapsis = " + apoapsis.ToString("F3"));
                }

                // wait for next update
                yield return new WaitForSeconds(1f);
            }
        }
    }
}
