using UnityEngine;
using Valve.VR;

namespace KerbalVR.InternalModules {
    public class HandRail : InteractableInternalModule {

        #region KSP Config Fields
        [KSPField]
        public float railLength = 0.1f;
        #endregion

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
            handleCollider.height = railLength;
            handleCollider.direction = 1; // y-axis aligned

            // add a pose for this object
            SkeletonPoser = this.gameObject.AddComponent<SteamVR_Skeleton_Poser>();
            SkeletonPoser.skeletonMainPose = SkeletonPose_HandleRailGrabPose.GetInstance();
            SkeletonPoser.Initialize();

            /*
            GameObject gizmo = Utils.CreateGizmo(0.25f);
            gizmo.transform.SetParent(this.transform);
            gizmo.transform.localPosition = Vector3.zero;
            gizmo.transform.localRotation = Quaternion.identity;
            gizmo.transform.localScale = Vector3.one;

            this.gameObject.AddComponent<ColliderVisualizer>();
            */

            // set layer for this object to 20 (Internal Space)
            Utils.SetLayer(this.gameObject, 20);
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
