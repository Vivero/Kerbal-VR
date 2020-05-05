using UnityEngine;

namespace KerbalVR {
    public class TeleportSystem : MonoBehaviour
    {
        public float maxDistance = 10f;
        public Vector3 downwardsVector = Vector3.up; // in world coordinates

        protected GameObject startGizmo, targetGizmo, targetGizmo2;

        protected void Start() {
            startGizmo = Utils.CreateGizmo();
            startGizmo.transform.parent = this.transform;
            startGizmo.transform.localPosition = Vector3.zero;
            startGizmo.transform.localRotation = Quaternion.identity;

            targetGizmo = Utils.CreateGizmo();
            targetGizmo.transform.position = Vector3.zero;
            targetGizmo.transform.rotation = Quaternion.identity;
            targetGizmo.transform.localScale = Vector3.one * 2f;
            DontDestroyOnLoad(targetGizmo);

            targetGizmo2 = Utils.CreateGizmo();
            targetGizmo2.transform.position = Vector3.zero;
            targetGizmo2.transform.rotation = Quaternion.identity;
            targetGizmo2.transform.localScale = Vector3.one * 4f;
            DontDestroyOnLoad(targetGizmo2);
        }

        protected void Update() {
            // raycast forward to see if we hit something
            int layerMask = 1 << 10; // do not strike layer "Scaled Scenery"
            layerMask = ~layerMask;

            RaycastHit forwardHit;
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out forwardHit, maxDistance, layerMask)) {
                targetGizmo.transform.position = forwardHit.point;
                // orient the target hit with the forward along the collider's plane, outwards of the hand
                targetGizmo.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(targetGizmo.transform.position - this.transform.position, forwardHit.normal), forwardHit.normal);
                targetGizmo2.SetActive(false);

            } else {
                // raycast downwards to see if we hit something
                targetGizmo.transform.position = transform.TransformPoint(Vector3.forward * maxDistance);

                RaycastHit downHit;
                if (Physics.Raycast(targetGizmo.transform.position, downwardsVector, out downHit, maxDistance * 2f, layerMask)) {
                    targetGizmo2.transform.position = downHit.point;
                    targetGizmo2.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(targetGizmo2.transform.position - this.transform.position, downHit.normal), downHit.normal);
                    targetGizmo2.SetActive(true);

                } else {
                    targetGizmo2.SetActive(false);
                }
            }
        }
    }
}
