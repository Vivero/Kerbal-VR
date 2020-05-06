using UnityEngine;
using Valve.VR;

namespace KerbalVR {
    public class TeleportSystem : MonoBehaviour
    {
        public float maxDistance = 10f;
        public Vector3 downwardsVector = Vector3.up; // in world coordinates

        protected GameObject startGizmo, targetGizmo, maxPivotGizmo, raycastGizmo, arrowGizmo, parabolaVertex, bezierVertex;
        protected bool isTeleportShowing = false;
        protected bool isTeleportAllowed = false;
        protected SteamVR_Action_Boolean teleportAction;
        protected Vector3 currentTargetPosition;

        protected const int NUM_ARC_VERTICES = 15;
        protected Vector3[] teleportArcVertices = new Vector3[NUM_ARC_VERTICES];
        protected GameObject[] teleportArcVertexObjects = new GameObject[NUM_ARC_VERTICES];

        protected void Awake() {
            startGizmo = Utils.CreateGizmo();
            startGizmo.transform.parent = this.transform;
            startGizmo.transform.localPosition = Vector3.zero;
            startGizmo.transform.localRotation = Quaternion.identity;
            startGizmo.transform.localScale = Vector3.one * 0.5f;

            targetGizmo = Utils.CreateGizmo();
            targetGizmo.transform.position = Vector3.zero;
            targetGizmo.transform.rotation = Quaternion.identity;
            targetGizmo.transform.localScale = Vector3.one * 4f;
            DontDestroyOnLoad(targetGizmo);

            maxPivotGizmo = Utils.CreateGizmo();
            maxPivotGizmo.transform.position = Vector3.zero;
            maxPivotGizmo.transform.rotation = Quaternion.identity;
            maxPivotGizmo.transform.localScale = Vector3.one * 2f;
            DontDestroyOnLoad(maxPivotGizmo);

            raycastGizmo = Utils.CreateGizmo();
            raycastGizmo.transform.position = Vector3.zero;
            raycastGizmo.transform.rotation = Quaternion.identity;
            raycastGizmo.transform.localScale = Vector3.one * 3f;
            DontDestroyOnLoad(raycastGizmo);

            arrowGizmo = Utils.CreateArrow();
            arrowGizmo.transform.position = Vector3.zero;
            arrowGizmo.transform.rotation = Quaternion.identity;
            arrowGizmo.transform.localScale = Vector3.one;
            DontDestroyOnLoad(arrowGizmo);

            parabolaVertex = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(parabolaVertex.GetComponent<SphereCollider>());
            parabolaVertex.transform.position = Vector3.zero;
            parabolaVertex.transform.rotation = Quaternion.identity;
            parabolaVertex.transform.localScale = Vector3.one * 0.1f;
            DontDestroyOnLoad(parabolaVertex);
            parabolaVertex.GetComponent<MeshRenderer>().material.color = Color.red;

            bezierVertex = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(bezierVertex.GetComponent<SphereCollider>());
            bezierVertex.transform.position = Vector3.zero;
            bezierVertex.transform.rotation = Quaternion.identity;
            bezierVertex.transform.localScale = Vector3.one * 0.05f;
            DontDestroyOnLoad(bezierVertex);
            bezierVertex.GetComponent<MeshRenderer>().material.color = Color.cyan;

            for (int i = 0; i < NUM_ARC_VERTICES; ++i) {
                teleportArcVertexObjects[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(teleportArcVertexObjects[i].GetComponent<SphereCollider>());
                teleportArcVertexObjects[i].transform.position = Vector3.zero;
                teleportArcVertexObjects[i].transform.rotation = Quaternion.identity;
                teleportArcVertexObjects[i].transform.localScale = Vector3.one * 0.05f;
                DontDestroyOnLoad(teleportArcVertexObjects[i]);
                teleportArcVertexObjects[i].GetComponent<MeshRenderer>().material.color = Color.green;
            }

            teleportAction = SteamVR_Input.GetBooleanAction("EVA", "teleport");
        }

        protected void OnEnable() {
            teleportAction.onChange += OnTeleportActionChange;
        }

        protected void OnDisable() {
            teleportAction.onChange -= OnTeleportActionChange;
        }

        private void OnTeleportActionChange(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState) {
            isTeleportShowing = newState; // show the teleport arc
            if (newState) {
                // button has been pressed down
            } else {
                // button has been lifted
                if (isTeleportAllowed && currentTargetPosition != null) {
                    if (HighLogic.LoadedScene == GameScenes.FLIGHT && FlightGlobals.ActiveVessel != null) {
                        FlightGlobals.ActiveVessel.SetPosition(currentTargetPosition);
                    }
                    else if (HighLogic.LoadedScene == GameScenes.MAINMENU) {
                        KerbalVR.Scene.Instance.CurrentPosition = currentTargetPosition;
                    }
                }
            }
        }

        protected void Update() {
            TeleportRaycast2();

            // RenderBezierCurve();
        }

        protected void TeleportRaycast1() {
            // raycasts should not strike layer "Scaled Scenery"
            int layerMask = 1 << 10;
            layerMask = ~layerMask;

            // raycast forward to see if we hit something
            RaycastHit forwardHit;
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out forwardHit, maxDistance, layerMask)) {
                currentTargetPosition = forwardHit.point;
                isTeleportAllowed = true;

                maxPivotGizmo.transform.position = currentTargetPosition;
                // orient the target hit with the forward along the collider's plane, outwards of the hand
                maxPivotGizmo.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(maxPivotGizmo.transform.position - this.transform.position, forwardHit.normal), forwardHit.normal);
                raycastGizmo.SetActive(false);

            }
            else {
                // raycast downwards to see if we hit something
                maxPivotGizmo.transform.position = transform.TransformPoint(Vector3.forward * maxDistance);

                RaycastHit downHit;
                if (Physics.Raycast(maxPivotGizmo.transform.position, downwardsVector, out downHit, maxDistance * 2f, layerMask)) {
                    currentTargetPosition = downHit.point;
                    isTeleportAllowed = true;

                    raycastGizmo.transform.position = downHit.point;
                    raycastGizmo.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(raycastGizmo.transform.position - this.transform.position, downHit.normal), downHit.normal);
                    raycastGizmo.SetActive(true);

                }
                else {
                    isTeleportAllowed = false;
                    raycastGizmo.SetActive(false);
                }
            }
        }

        protected void TeleportRaycast2() {
            // raycasts should not strike layer "Scaled Scenery"
            int layerMask = 1 << 10;
            layerMask = ~layerMask;

            // project a ray forward (parallel to ground)
            Vector3 forwardTowardsTarget = Vector3.ProjectOnPlane(transform.forward, downwardsVector).normalized;
            arrowGizmo.transform.position = transform.position;
            arrowGizmo.transform.rotation = Quaternion.LookRotation(forwardTowardsTarget);
            Ray horizontalRay = new Ray(transform.position, forwardTowardsTarget);

            float maxDistance = Configuration.Instance.Control1 * 5f;
            float pitchAngle = Vector3.Angle(transform.forward, downwardsVector);
            float horizontalDistance = MathUtils.Map(pitchAngle, 60f, 120f, 0.06f, maxDistance);
            Vector3 pivotPoint = horizontalRay.origin + horizontalRay.direction * horizontalDistance;

            maxPivotGizmo.transform.position = pivotPoint;
            maxPivotGizmo.transform.rotation = Quaternion.LookRotation(forwardTowardsTarget, -downwardsVector);

            float castHeight = Configuration.Instance.Control2 * 5f;
            Vector3 castPosition = transform.position + (-downwardsVector.normalized * castHeight);
            Ray castRay = new Ray(castPosition, (pivotPoint - castPosition).normalized);

            RaycastHit castHit;
            bool isFwdHit = false;
            if (Physics.Raycast(castRay, out castHit, 12f, layerMask)) {
                // check if we hit from a position above the player
                currentTargetPosition = castHit.point;
                isTeleportAllowed = true;

                targetGizmo.transform.position = currentTargetPosition;
                // orient the target hit with the forward along the collider's plane, outwards of the hand
                targetGizmo.transform.rotation = Quaternion.LookRotation(
                    Vector3.ProjectOnPlane(targetGizmo.transform.position - this.transform.position, castHit.normal),
                    castHit.normal);

            } else {
                // check if we hit along the horizontal ray
                RaycastHit forwardHit;
                if (Physics.Raycast(horizontalRay, out forwardHit, horizontalDistance, layerMask)) {
                    currentTargetPosition = forwardHit.point;
                    isTeleportAllowed = true;

                    targetGizmo.transform.position = forwardHit.point;
                    targetGizmo.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(targetGizmo.transform.position - this.transform.position, forwardHit.normal), forwardHit.normal);
                    targetGizmo.SetActive(true);

                    isFwdHit = true;

                }
                else {
                    isTeleportAllowed = false;
                    raycastGizmo.SetActive(false);
                }
            }

            string logMsg = "Max Distance = " + maxDistance.ToString("F3") + "\n";
            logMsg += "pitchAngle = " + pitchAngle + "\n";
            logMsg += "horizontalDistance = " + horizontalDistance + "\n";
            logMsg += "isFwdHit = " + isFwdHit + "\n\n";
            logMsg += "Control1 = " + Configuration.Instance.Control1 + "\n";
            logMsg += "Control2 = " + Configuration.Instance.Control2 + "\n\n";
            logMsg += "hand = " + transform.position.ToString("F3") + "\n";

            if (isTeleportAllowed) {
                Vector3 controlPoint = pivotPoint;
                /*Vector3 castToTarget = currentTargetPosition - castPosition;
                float distToTargetFromCast = castToTarget.magnitude;
                float distToPivotFromCast = (pivotPoint - castPosition).magnitude;
                if (distToTargetFromCast < distToPivotFromCast) {
                    controlPoint = castPosition + castToTarget.normalized * distToTargetFromCast * 0.5f;
                }*/

                Vector3 aboveTarget = currentTargetPosition + (-downwardsVector * 2f);
                controlPoint = aboveTarget;

                teleportArcVertices = MathUtils.GenerateBezierCurveVertices(transform.position, currentTargetPosition, controlPoint, NUM_ARC_VERTICES);

                for (int i = 0; i < NUM_ARC_VERTICES; ++i) {
                    teleportArcVertexObjects[i].transform.position = teleportArcVertices[i];
                }
            }

            // Utils.SetDebugText(logMsg);
        }

        protected void RenderParabolaCurve() {
            if (isTeleportAllowed) {
                // define a parabola between the origin and target
                //
                // f(x) = a*(x-h)^2 + k, where (h,k) is the vertex of the parabola.
                //
                // in a coordinate system where forwards points towards the target (but parallel to the ground),
                // upwards points the opposite of downwardsVector

                Vector3 originToTarget = currentTargetPosition - transform.position;
                Vector3 forwardTowardsTarget = Vector3.ProjectOnPlane(originToTarget, downwardsVector);

                Matrix4x4 parabolaTRS = Matrix4x4.TRS(transform.position, Quaternion.LookRotation(forwardTowardsTarget, -downwardsVector), Vector3.one);
                Vector3 originToTargetRelToOrigin = parabolaTRS.inverse * originToTarget;

                float a = -0.2f;
                float x1 = originToTargetRelToOrigin.z;
                float y1 = originToTargetRelToOrigin.y;
                float h = (a * x1 * x1 - y1) / (2f * a * x1);
                float k = -a * h * h;
                Vector3 originToVertexRelToOrigin = new Vector3(0f, k, h);
                Vector3 originToVertex = parabolaTRS * originToVertexRelToOrigin;

                parabolaVertex.transform.position = originToVertex + transform.position;

                // determine points along the arc
                float dx = x1 / (NUM_ARC_VERTICES + 1);
                for (int i = 0; i < NUM_ARC_VERTICES; ++i) {
                    float xi = (i + 1) * dx;
                    float yi = a * (xi - h) * (xi - h) + k;
                    teleportArcVertices[i] = (Vector3)(parabolaTRS * new Vector3(0f, yi, xi)) + transform.position;
                    teleportArcVertexObjects[i].transform.position = teleportArcVertices[i];
                }


                string logMsg = "originToTargetRelToOrigin1 = " + originToTargetRelToOrigin.ToString("F3") + "\n";
                // Utils.SetDebugText(logMsg);


            }
        }

        protected void RenderBezierCurve() {
            if (isTeleportAllowed) {
                // define a point above the player
                float pitchAngle = Vector3.Angle(transform.forward, downwardsVector);
                // float clampedPitchAngle = Mathf.Clamp(pitchAngle, 0f, 120f);
                float maxAboveHeight = Configuration.Instance.Control2 * 10f;
                float aboveHeight = MathUtils.Map(pitchAngle, 90f, 120f, 0f, 5f);
                float controlDistanceFraction = Configuration.Instance.Control1;
                Vector3 abovePoint = transform.position + (Vector3.Normalize(-downwardsVector) * aboveHeight);
                Vector3 aboveToTarget = currentTargetPosition - abovePoint;
                Vector3 controlPoint = abovePoint + aboveToTarget * controlDistanceFraction;

                teleportArcVertices = MathUtils.GenerateBezierCurveVertices(transform.position, currentTargetPosition, controlPoint, NUM_ARC_VERTICES);

                for (int i = 0; i < NUM_ARC_VERTICES; ++i) {
                    teleportArcVertexObjects[i].transform.position = teleportArcVertices[i];
                }

                string logMsg = "Pitch = " + pitchAngle + " deg\n\n";
                logMsg += "Control1 = " + Configuration.Instance.Control1 + "\n";
                logMsg += "Control2 = " + Configuration.Instance.Control2 + "\n\n";
                logMsg += "controlDistanceFraction = " + controlDistanceFraction + "\n";
                logMsg += "maxAboveHeight = " + maxAboveHeight + "\n";
                logMsg += "aboveHeight = " + aboveHeight + "\n";
                // Utils.SetDebugText(logMsg);
            } 
        }
    }
}
