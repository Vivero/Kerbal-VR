using UnityEngine;
using Valve.VR;

public class KerbalVR_TeleportSystem : MonoBehaviour {
    public Vector3 downwardsVector = Vector3.down;
    public Transform handOriginLeft, handOriginRight;
    public float maxForwardCastDistance = 10f;
    public float maxDownwardCastDistance = 10f;
    public float teleportArcPeriod = 0.125f;

    public float bezierControlPointAngleDeg = 40f;
    public float bezierControlPointFraction = 0.75f;

    protected Transform origin;
    protected SteamVR_Action_Boolean teleportAction;
    protected SteamVR_Input_Sources teleportSource = SteamVR_Input_Sources.Any;
    protected bool isTeleportShowing = false;
    protected bool isTeleportAllowed = true;
    protected Vector3 teleportTargetPosition;

    protected const int MAX_TELEPORT_ARC_VERTICES = 10;
    protected const float TELEPORT_ARC_FRACTION = 1f / MAX_TELEPORT_ARC_VERTICES;
    protected LineRenderer[] teleportArcVertexRenderers = new LineRenderer[MAX_TELEPORT_ARC_VERTICES];

    // DEBUG ---
    protected GameObject gizmo, gizmoBezierControlPoint;
    protected LineRenderer gizmoForwardCast;
    protected LineRenderer gizmoDownwardCast;
    // ---------

    protected void Awake() {
        // DEBUG ---
        ActivateActionSet("EVA");
        // ---------

        teleportAction = SteamVR_Input.GetBooleanAction("EVA", "Teleport");

        for (int i = 0; i < MAX_TELEPORT_ARC_VERTICES; ++i) {
            GameObject arcVertex = new GameObject("KVR_TeleportArcVertex_" + i);
            teleportArcVertexRenderers[i] = arcVertex.AddComponent<LineRenderer>();
            teleportArcVertexRenderers[i].material = new Material(Shader.Find("Particles/Standard Unlit"));
            teleportArcVertexRenderers[i].startColor = teleportArcVertexRenderers[i].endColor = Color.cyan;
            teleportArcVertexRenderers[i].startWidth = teleportArcVertexRenderers[i].endWidth = 0.02f;
            teleportArcVertexRenderers[i].enabled = false;
        }
    }


    protected void Start() {
        // DEBUG ---
        gizmo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gizmo.transform.SetParent(this.transform);
        gizmo.transform.localPosition = Vector3.zero;
        gizmo.transform.localRotation = Quaternion.identity;
        gizmo.transform.localScale = Vector3.one * 0.05f;
        Destroy(gizmo.GetComponent<SphereCollider>());
        gizmo.GetComponent<MeshRenderer>().material.color = Color.red;

        gizmoBezierControlPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gizmoBezierControlPoint.transform.localPosition = Vector3.zero;
        gizmoBezierControlPoint.transform.localRotation = Quaternion.identity;
        gizmoBezierControlPoint.transform.localScale = Vector3.one * 0.1f;
        Destroy(gizmoBezierControlPoint.GetComponent<SphereCollider>());
        gizmoBezierControlPoint.GetComponent<MeshRenderer>().material.color = Color.blue;
        gizmoBezierControlPoint.SetActive(false);

        GameObject gizmoForwardCastGO = new GameObject();
        gizmoForwardCast = gizmoForwardCastGO.AddComponent<LineRenderer>();
        gizmoForwardCast.material = new Material(Shader.Find("Particles/Standard Unlit"));
        gizmoForwardCast.startColor = gizmoForwardCast.endColor = Color.red;
        gizmoForwardCast.startWidth = gizmoForwardCast.endWidth = 0.02f;
        gizmoForwardCast.enabled = false;

        GameObject gizmoDownwardCastGO = new GameObject();
        gizmoDownwardCast = gizmoDownwardCastGO.AddComponent<LineRenderer>();
        gizmoDownwardCast.material = new Material(Shader.Find("Particles/Standard Unlit"));
        gizmoDownwardCast.startColor = gizmoDownwardCast.endColor = Color.red;
        gizmoDownwardCast.startWidth = gizmoDownwardCast.endWidth = 0.02f;
        gizmoDownwardCast.enabled = false;
        // ---------
    }

    protected void OnEnable() {
        teleportAction.AddOnChangeListener(OnTeleportActionChangeL, SteamVR_Input_Sources.LeftHand);
        teleportAction.AddOnChangeListener(OnTeleportActionChangeR, SteamVR_Input_Sources.RightHand);
    }

    protected void OnDisable() {
        teleportAction.RemoveOnChangeListener(OnTeleportActionChangeL, SteamVR_Input_Sources.LeftHand);
        teleportAction.RemoveOnChangeListener(OnTeleportActionChangeR, SteamVR_Input_Sources.RightHand);
    }

    protected void Update() {
        // logic to dictate enabling/disabling teleportation
        isTeleportAllowed = true;

        // position the teleportation origin
        if (origin != null) {
            this.transform.position = origin.transform.position;
            this.transform.rotation = origin.transform.rotation;
        }

        // calculate the teleportation target
        UpdateTeleportTarget();
        isTeleportAllowed = isTeleportAllowed && isTeleportShowing;

        // render the teleport arc
        RenderTeleportArc();
        if (!isTeleportAllowed) {
            for (int i = 0; i < MAX_TELEPORT_ARC_VERTICES; i++) {
                teleportArcVertexRenderers[i].enabled = false;
            }
        }

        gizmo.GetComponent<MeshRenderer>().enabled = false;
        gizmoForwardCast.enabled = false;
        gizmoDownwardCast.enabled = false;
        gizmoBezierControlPoint.SetActive(false);
    }

    protected void OnTeleportActionChangeL(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState) {
        OnTeleportActionChange(fromAction, fromSource, newState);
    }

    protected void OnTeleportActionChangeR(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState) {
        OnTeleportActionChange(fromAction, fromSource, newState);
    }

    protected void OnTeleportActionChange(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState) {
        if (teleportSource == SteamVR_Input_Sources.Any || teleportSource == fromSource) {
            if (newState) {
                // button has been pressed down, when the teleport arc wasn't already showing.
                // switch the origin to the controller that pressed the button.
                if (fromSource == SteamVR_Input_Sources.RightHand) {
                    teleportSource = SteamVR_Input_Sources.RightHand;
                    origin = handOriginRight;
                    isTeleportShowing = true;
                }
                else if (fromSource == SteamVR_Input_Sources.LeftHand) {
                    teleportSource = SteamVR_Input_Sources.LeftHand;
                    origin = handOriginLeft;
                    isTeleportShowing = true;
                }
            }
            else {
                // button has been lifted, move to that location
                if (isTeleportAllowed && teleportTargetPosition != null) {
                    // TODO: move
                }
                teleportSource = SteamVR_Input_Sources.Any;
                isTeleportShowing = false;
            }
        }
    }

    protected void UpdateTeleportTarget() {
        if (!isTeleportShowing || origin == null) {
            // do nothing if we're not showing the teleport arc
            return;
        }

        // cast a ray forward
        RaycastHit forwardHit;
        Vector3 forwardVector = origin.TransformDirection(Vector3.forward);
        if (Physics.Raycast(origin.position, forwardVector, out forwardHit, maxForwardCastDistance)) {
            teleportTargetPosition = forwardHit.point;
            isTeleportAllowed = true;

            gizmoForwardCast.enabled = true;
            gizmoForwardCast.SetPosition(0, origin.position);
            gizmoForwardCast.SetPosition(1, teleportTargetPosition);
            gizmoDownwardCast.enabled = false;

        } else {
            // shorten the max forward cast distance when the controller starts to pitch high up,
            // so the teleport location starts to come closer faster
            float pitchAngle = Vector3.Angle(forwardVector, downwardsVector);
            float pitchAngleNorm = Map(pitchAngle, 90f, 180f, 1f, 0.01f);
            float forwardCastDistance = maxForwardCastDistance * pitchAngleNorm;
            Vector3 maxForwardCastPosition = forwardVector * forwardCastDistance + origin.position;

            // cast a ray downward
            RaycastHit downwardHit;
            if (Physics.Raycast(maxForwardCastPosition, downwardsVector, out downwardHit, maxDownwardCastDistance)) {
                teleportTargetPosition = downwardHit.point;
                isTeleportAllowed = true;

                gizmoForwardCast.enabled = true;
                gizmoForwardCast.SetPosition(0, origin.position);
                gizmoForwardCast.SetPosition(1, maxForwardCastPosition);
                gizmoDownwardCast.enabled = true;
                gizmoDownwardCast.SetPosition(0, maxForwardCastPosition);
                gizmoDownwardCast.SetPosition(1, teleportTargetPosition);
            } else {
                isTeleportAllowed = false;

                gizmoForwardCast.enabled = false;
                gizmoDownwardCast.enabled = false;
            }
        }
    }

    protected void RenderTeleportArc() {
        if (!isTeleportShowing || origin == null) {
            // do nothing if we're not showing the teleport arc
            return;
        }

        // determine the bezier control point
        // TODO: maybe increase the lift angle at very high controller pitches
        Vector3 targetFromOrigin = teleportTargetPosition - origin.position;
        Vector3 bezierControlFromOrigin = (teleportTargetPosition - origin.position) * bezierControlPointFraction;
        Vector3 rotationAxis = Vector3.Cross(targetFromOrigin, downwardsVector);
        bezierControlFromOrigin = Quaternion.AngleAxis(-bezierControlPointAngleDeg, rotationAxis) * bezierControlFromOrigin;

        // generate bezier vertices along curve
        Vector3 startPoint = origin.position;
        Vector3 controlPoint = bezierControlFromOrigin + origin.position;
        Vector3 endPoint = teleportTargetPosition;

        for (int i = 0; i < MAX_TELEPORT_ARC_VERTICES; i++) {
            // bezier vertex point
            float t = (TELEPORT_ARC_FRACTION * i) + (Time.time % teleportArcPeriod) / teleportArcPeriod * TELEPORT_ARC_FRACTION;
            Vector3 pointA = Vector3.Lerp(startPoint, controlPoint, t);
            Vector3 pointB = Vector3.Lerp(controlPoint, endPoint, t);
            teleportArcVertexRenderers[i].transform.position = Vector3.Lerp(pointA, pointB, t);

            // calculate where to start/stop the line renderers
            Vector3 lrEndPoint = teleportArcVertexRenderers[i].transform.position; // end the line at the current bezier vertex
            Vector3 lrStartPoint;
            if (i == 0) {
                // the first LR will have to start at the origin of the arc
                lrStartPoint = origin.position;
            } else {
                // the next LRs can start at the previous bezier vertex (well, midway there, so it looks like a dashed line)
                lrStartPoint = Vector3.Lerp(lrEndPoint, teleportArcVertexRenderers[i - 1].transform.position, 0.5f);
            }
            teleportArcVertexRenderers[i].SetPosition(0, lrStartPoint);
            teleportArcVertexRenderers[i].SetPosition(1, lrEndPoint);
            teleportArcVertexRenderers[i].enabled = true;
        }

        gizmoBezierControlPoint.transform.position = bezierControlFromOrigin + origin.position;
        gizmoBezierControlPoint.SetActive(true);
    }


    // DEBUG ---
    protected static void ActivateActionSet(string actionSetName) {
        SteamVR_ActionSet actionSet = SteamVR_Input.GetActionSet(actionSetName, false, true);
        if (actionSet != null) {
            actionSet.Activate(SteamVR_Input_Sources.Any);
        }
        else {
            Debug.LogError("Action Set '" + actionSetName + "' does not exist");
        }
    }

    public static float Map(float value, float fromMin, float fromMax, float toMin, float toMax) {
        if (value <= fromMin) {
            return toMin;
        }
        else if (value >= fromMax) {
            return toMax;
        }
        return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
    }
    // ---------
}
