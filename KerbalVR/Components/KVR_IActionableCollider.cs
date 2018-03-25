using UnityEngine;

namespace KerbalVR.Components
{
    public interface IActionableCollider
    {
        void OnColliderEntered(Collider thisObject, Collider otherObject);
        // void OnColliderStayed(Collider thisObject, Collider otherObject);
        void OnColliderExited(Collider thisObject, Collider otherObject);
    }
}
