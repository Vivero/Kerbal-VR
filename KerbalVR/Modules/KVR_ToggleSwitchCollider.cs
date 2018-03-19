using UnityEngine;

namespace KerbalVR.Modules
{
    [RequireComponent(typeof(Rigidbody))]
    public class KVR_ToggleSwitchCollider : MonoBehaviour
    {
        public KVR_ToggleSwitchDouble toggleSwitchComponent;

        private Rigidbody componentRigidbody;
        private Collider componentCollider;

        void Awake() {
            componentRigidbody = GetComponent<Rigidbody>();
            componentRigidbody.isKinematic = true;

            componentCollider = GetComponent<Collider>();
            componentCollider.isTrigger = true;
        }

        void OnTriggerEnter(Collider other) {
            if (toggleSwitchComponent != null) {
                toggleSwitchComponent.SwitchColliderEntered(gameObject);
            }
        }

        void OnTriggerStay(Collider other) {

        }

        void OnTriggerExit(Collider other) {
            if (toggleSwitchComponent != null) {
                toggleSwitchComponent.SwitchColliderExited(gameObject);
            }
        }
    }
}
