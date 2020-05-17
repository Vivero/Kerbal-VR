using UnityEngine;
using Valve.VR;

namespace KerbalVR.Modules {
    public class HandRail : InternalModule {
        public SteamVR_Skeleton_Poser GrabPoser { get; private set; }
        protected ConfigNode moduleConfigNode;
        protected CapsuleCollider handleCollider;

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
            GrabPoser = this.gameObject.AddComponent<SteamVR_Skeleton_Poser>();
            GrabPoser.skeletonMainPose = SkeletonPose_HandleRailGrabPose.GetInstance();
            GrabPoser.Initialize();

            this.gameObject.AddComponent<ColliderVisualizer>();

            GameObject gizmo = Utils.CreateGizmo();
            Utils.SetLayer(gizmo, 20);
            gizmo.transform.SetParent(this.transform);
            gizmo.transform.localPosition = Vector3.zero;
            gizmo.transform.localRotation = Quaternion.identity;
            gizmo.transform.localScale = Vector3.one * 2f;
        }
    }
}
