using UnityEngine;
using Valve.VR;

namespace KerbalVR.InternalModules {
    public class HandRail : InteractableInternalModule {

        #region Private Members
        protected ConfigNode moduleConfigNode;
        protected CapsuleCollider handleCollider;
        #endregion

        protected void Start() {
            // no setup needed in editor mode
            if (HighLogic.LoadedScene == GameScenes.EDITOR) return;

            // obtain module configuration
            moduleConfigNode = KerbalVR.ConfigUtils.GetModuleConfigNode(internalProp.name, moduleID);

            // add an interactable collider
            handleCollider = this.gameObject.AddComponent<CapsuleCollider>();
            handleCollider.isTrigger = true;
            handleCollider.center = new Vector3(0f, 0f, -0.0527f);
            handleCollider.radius = 0.01f;
            handleCollider.height = 0.45f; // 0.38f;
            handleCollider.direction = 1; // y-axis aligned

            // set layer for this object to 20 (Internal Space)
            Utils.SetLayer(this.gameObject, 20);

            // add a pose for this object
            SkeletonPoser = this.gameObject.AddComponent<SteamVR_Skeleton_Poser>();
            SkeletonPoser.skeletonMainPose = SkeletonPose_HandleRailGrabPose.GetInstance();
            SkeletonPoser.Initialize();

#if DEBUG
            GameObject gizmo = Utils.CreateGizmo(0.25f);
            Utils.SetLayer(gizmo, 20);
            gizmo.transform.SetParent(this.transform);
            gizmo.transform.localPosition = Vector3.zero;
            gizmo.transform.localRotation = Quaternion.identity;
            gizmo.transform.localScale = Vector3.one;
#endif
        }

        protected void Update() {
            if (IsGrabbed) {
                Vector3 deltaPos = GrabbedHand.handActionPose.GetLastLocalPosition(GrabbedHand.handType) -
                    GrabbedHand.handActionPose.GetLocalPosition(GrabbedHand.handType);
                KerbalVR.Scene.Instance.CurrentPosition += KerbalVR.Scene.Instance.CurrentRotation * deltaPos;
            }
        }
    }
}
