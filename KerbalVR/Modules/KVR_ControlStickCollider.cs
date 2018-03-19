using UnityEngine;

namespace KerbalVR.Modules
{
    [RequireComponent(typeof(Rigidbody))]
    public class KVR_ControlStickCollider : MonoBehaviour
    {
        public KVR_ControlStick controlStickComponent;

        private Rigidbody componentRigidbody;
        private Collider componentCollider;

        void Awake() {
            componentRigidbody = GetComponent<Rigidbody>();
            componentRigidbody.isKinematic = true;

            componentCollider = GetComponent<Collider>();
            componentCollider.isTrigger = true;
        }

        /*void OnTriggerEnter(Collider other) {
            Utils.Log("KVR_ControlStickCollider enter " + other.gameObject.name);
        }*/

        void OnTriggerStay(Collider other) {
            if (controlStickComponent != null) {
                controlStickComponent.StickColliderStayed(gameObject);
            }
        }
    }
}
