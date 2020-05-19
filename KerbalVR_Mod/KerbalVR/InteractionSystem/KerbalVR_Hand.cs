using KerbalVR.InternalModules;
using System;
using UnityEngine;
using Valve.VR;

namespace KerbalVR {
    /// <summary>
    /// The Hand component is applied to each of the two hand GameObjects.
    /// It handles all the interactions related to using the hands in VR.
    /// </summary>
    public class Hand : MonoBehaviour {

        #region Public Members
        /// <summary>
        /// The prefab GameObject to use to instantiate empty hands.
        /// </summary>
        public GameObject handPrefab;

        /// <summary>
        /// Either the LeftHand or RightHand for this object.
        /// </summary>
        public SteamVR_Input_Sources handType;

        /// <summary>
        /// The SteamVR_Input action for the hand pose data.
        /// </summary>
        public SteamVR_Action_Pose handActionPose;

        /// <summary>
        /// The other hand's GameObject
        /// </summary>
        public GameObject otherHand;
        #endregion


        #region Private Members
        // hand game objects
        protected GameObject handObject;
        protected SkinnedMeshRenderer handRenderer;
        protected SteamVR_Behaviour_Skeleton handSkeleton;
        protected SphereCollider handCollider;
        protected Rigidbody handRigidbody;

        // keep tracking of render state
        protected Types.ShiftRegister<bool> isRenderingHands = new Types.ShiftRegister<bool>(2);
        protected Types.ShiftRegister<int> renderLayerHands = new Types.ShiftRegister<int>(2);

        // keep track of held objects
        protected InteractableInternalModule hoveredObject;
        protected InteractableInternalModule heldObject;
        protected SteamVR_Action_Boolean actionGrab;
        #endregion


        public void Initialize() {
            // verify members are set correctly
            if (handType != SteamVR_Input_Sources.LeftHand && handType != SteamVR_Input_Sources.RightHand) {
                throw new Exception("handType must be LeftHand or RightHand");
            }

            // make instance object out of the hand prefab
            handObject = Instantiate(handPrefab);
            if (handObject == null) {
                throw new Exception("Could not Instantiate prefab for " + handType);
            }
            handObject.name = "KVR_HandObject_" + handType;
            DontDestroyOnLoad(handObject);

            // cache the hand renderers
            string renderModelPath = (handType == SteamVR_Input_Sources.LeftHand) ? "slim_l/vr_glove_right_slim" : "slim_r/vr_glove_right_slim";
            Transform handSkin = handObject.transform.Find(renderModelPath);
            handRenderer = handSkin.gameObject.GetComponent<SkinnedMeshRenderer>();
            handRenderer.enabled = false;
            Utils.SetLayer(handObject, 0);

            // define hand-specific names
            string skeletonRoot = (handType == SteamVR_Input_Sources.LeftHand) ? "slim_l/Root" : "slim_r/Root";
            string skeletonActionName = (handType == SteamVR_Input_Sources.LeftHand) ? "SkeletonLeftHand" : "SkeletonRightHand";

            // add behavior scripts to the hands
            handSkeleton = handObject.AddComponent<SteamVR_Behaviour_Skeleton>();
            handSkeleton.skeletonRoot = handObject.transform.Find(skeletonRoot);
            handSkeleton.inputSource = handType;
            handSkeleton.mirroring = (handType == SteamVR_Input_Sources.LeftHand) ? SteamVR_Behaviour_Skeleton.MirrorType.RightToLeft : SteamVR_Behaviour_Skeleton.MirrorType.None;
            handSkeleton.updatePose = false;
            handSkeleton.skeletonAction = SteamVR_Input.GetSkeletonAction("default", skeletonActionName, false);
            handSkeleton.fallbackCurlAction = SteamVR_Input.GetSingleAction("default", "Squeeze", false);

            // add fallback pose scripts
            Transform handFallback = handObject.transform.Find("fallback");
            SteamVR_Skeleton_Poser handPoser = handFallback.gameObject.AddComponent<SteamVR_Skeleton_Poser>();
            handPoser.skeletonMainPose = SkeletonPose_FallbackRelaxedPose.GetInstance();
            handPoser.Initialize();
            handSkeleton.fallbackPoser = handPoser;
            handSkeleton.Initialize();

            // add interactable collider
            handCollider = this.gameObject.AddComponent<SphereCollider>();
            handCollider.isTrigger = true;
            handCollider.center = new Vector3(0f, 0f, -0.1f);
            handCollider.radius = 0.08f;

            handRigidbody = this.gameObject.AddComponent<Rigidbody>();
            handRigidbody.useGravity = false;
            handRigidbody.isKinematic = true;

            // set up actions
            actionGrab = SteamVR_Input.GetBooleanAction("default", "GrabGrip");
            actionGrab[handType].onChange += OnChangeGrab;

#if DEBUG
            GameObject gizmo = Utils.CreateGizmo();
            gizmo.transform.SetParent(this.transform);
            gizmo.transform.localPosition = Vector3.zero;
            gizmo.transform.localRotation = Quaternion.identity;
            gizmo.transform.localScale = Vector3.one * 0.5f;

            GameObject gizmoHand = Utils.CreateGizmo();
            gizmo.transform.SetParent(handObject.transform);
            gizmo.transform.localPosition = Vector3.zero;
            gizmo.transform.localRotation = Quaternion.identity;
#endif
        }

        protected void OnChangeGrab(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState) {
            if (newState) {
                if (hoveredObject != null) {
                    heldObject = hoveredObject;
                    heldObject.IsGrabbed = true;
                    heldObject.GrabbedHand = this;
                    handSkeleton.BlendToPoser(heldObject.SkeletonPoser);
                }
            } else {
                if (heldObject != null) {
                    heldObject.IsGrabbed = false;
                    heldObject.GrabbedHand = null;
                    heldObject = null;
                    handSkeleton.BlendToSkeleton();
                }
            }
        }

        protected void Update() {
            // should we render the hands in the current scene?
            bool isRendering = false;
            if (KerbalVR.Core.IsVrRunning) {
                switch (HighLogic.LoadedScene) {
                    case GameScenes.MAINMENU:
                    case GameScenes.EDITOR:
                        isRendering = true;
                        renderLayerHands.Push(0);
                        break;

                    case GameScenes.FLIGHT:
                        if (KerbalVR.Scene.IsInEVA() || KerbalVR.Scene.IsInIVA()) {
                            isRendering = true;

                            if (KerbalVR.Scene.IsInIVA()) {
                                // IVA-specific settings
                                renderLayerHands.Push(20);
                            }
                            if (KerbalVR.Scene.IsInEVA()) {
                                // EVA-specific settings
                                renderLayerHands.Push(0);
                            }
                        }
                        else {
                            isRendering = false;
                        }
                        break;
                }
            }
            else {
                isRendering = false;
            }

            // if rendering, update the hand positions
            if (isRendering) {
                // get device indices for each hand, then set the transform
                bool isConnected = handActionPose.GetDeviceIsConnected(handType);
                uint deviceIndex = handActionPose.GetDeviceIndex(handType);
                if (isConnected && deviceIndex < OpenVR.k_unMaxTrackedDeviceCount) {
                    // keep this object (Hand script) always tracking the device
                    SteamVR_Utils.RigidTransform handTransform = new SteamVR_Utils.RigidTransform(KerbalVR.Core.GamePoses[deviceIndex].mDeviceToAbsoluteTracking);
                    Vector3 handTransformPos = KerbalVR.Scene.Instance.DevicePoseToWorld(handTransform.pos);
                    Quaternion handTransformRot = KerbalVR.Scene.Instance.DevicePoseToWorld(handTransform.rot);
                    this.transform.position = handTransformPos;
                    this.transform.rotation = handTransformRot;

                    // determine if the rendered hand object needs to track device, or the interacting object
                    if (heldObject != null && heldObject.SkeletonPoser != null) {
                        // this equation looks messy because of the way the SteamVR_Skeleton_Pose_Hand object
                        // records the skeleton offset position/rotation. it's like a negative offset, relative
                        // to the object we are interacting with. i think it boils down to this:
                        //
                        // handTransform + skeletonOffset = heldTransform
                        //
                        // so, we are solving for `handTransform`, taking into account the rotations of the various frames of reference.
                        //
                        Quaternion skeletonRotInv = (handType == SteamVR_Input_Sources.LeftHand) ?
                            Quaternion.Inverse(heldObject.SkeletonPoser.skeletonMainPose.leftHand.rotation) :
                            Quaternion.Inverse(heldObject.SkeletonPoser.skeletonMainPose.rightHand.rotation);
                        Vector3 skeletonPos = (handType == SteamVR_Input_Sources.LeftHand) ?
                            heldObject.SkeletonPoser.skeletonMainPose.leftHand.position :
                            heldObject.SkeletonPoser.skeletonMainPose.rightHand.position;

                        handObject.transform.position = heldObject.transform.position - heldObject.transform.rotation * skeletonRotInv * skeletonPos;
                        handObject.transform.rotation = heldObject.transform.rotation * skeletonRotInv;
                    } else {
                        handObject.transform.position = handTransformPos;
                        handObject.transform.rotation = handTransformRot;
                    }
                } else {
                    isRendering = false;
                }
            }

            // makes changes as necessary
            isRenderingHands.Push(isRendering);
            if (isRenderingHands.IsChanged()) {
                handRenderer.enabled = isRenderingHands.Value;
            }
            if (renderLayerHands.IsChanged()) {
                Utils.SetLayer(this.gameObject, renderLayerHands.Value);
                Utils.SetLayer(handObject, renderLayerHands.Value);
            }
        }

        protected void OnTriggerEnter(Collider other) {
            if (other.gameObject.name.StartsWith("KVR_")) {
                InteractableInternalModule interactable = other.gameObject.GetComponent<InteractableInternalModule>();
                if (interactable != null) {
                    hoveredObject = interactable;
                }
            }
        }

        protected void OnTriggerExit(Collider other) {
            if (hoveredObject != null) {
                InteractableInternalModule interactable = other.gameObject.GetComponent<InteractableInternalModule>();
                if (interactable == hoveredObject) {
                    hoveredObject = null;
                }
            }
        }
    }
}
