using UnityEngine;

namespace KerbalVR.Components
{
    /// <summary>
    /// This component can be attached to a collider to provide
    /// a way to communicate actions to another object (such as
    /// an InternalModule whose prop has colliders).
    /// 
    /// Example: You have an InternalProp with an InternalModule.
    /// The InternalProp has child objects with colliders. Attach
    /// a KVR_ActionableCollider component to the prop's colliders.
    /// When some other GameObject (with a collider)
    /// enters/stays/exits the prop collider, the corresponding
    /// function will be called in the InternalModule.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class KVR_ActionableCollider : MonoBehaviour
    {
        // a reference to the module we want to call upon
        // interacting with this collider
        public IActionableCollider module;

        private Rigidbody componentRigidbody;
        private Collider componentCollider;

        void Awake() {
            componentRigidbody = GetComponent<Rigidbody>();
            componentRigidbody.isKinematic = true;

            componentCollider = GetComponent<Collider>();
            componentCollider.isTrigger = true;
        }

        void OnTriggerEnter(Collider other) {
            if (module != null) {
                module.OnColliderEntered(componentCollider, other);
            }
        }

        /*void OnTriggerStay(Collider other) {
            if (module != null) {
                module.OnColliderStayed(componentCollider, other);
            }
        }*/

        void OnTriggerExit(Collider other) {
            if (module != null) {
                module.OnColliderExited(componentCollider, other);
            }
        }
    }
}
