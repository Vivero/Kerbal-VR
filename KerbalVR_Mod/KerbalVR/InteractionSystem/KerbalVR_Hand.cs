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
        #endregion

        #region Private Members
        // hand game objects
        protected SkinnedMeshRenderer handRenderer;
        protected SteamVR_Behaviour_Skeleton handSkeleton;

        // keep tracking of render state
        protected Types.ShiftRegister<bool> isRenderingHands = new Types.ShiftRegister<bool>(2);
        protected Types.ShiftRegister<int> renderLayerHands = new Types.ShiftRegister<int>(2);
        #endregion

        public void Initialize() {
            // verify members are set correctly
            if (handType != SteamVR_Input_Sources.LeftHand && handType != SteamVR_Input_Sources.RightHand) {
                throw new Exception("handType must be LeftHand or RightHand");
            }

            // cache the hand renderers
            string renderModelPath = (handType == SteamVR_Input_Sources.LeftHand) ? "slim_l/vr_glove_right_slim" : "slim_r/vr_glove_right_slim";
            Transform handSkin = this.gameObject.transform.Find(renderModelPath);
            handRenderer = handSkin.gameObject.GetComponent<SkinnedMeshRenderer>();
            handRenderer.enabled = false;
            Utils.SetLayer(this.gameObject, 0);

            // define hand-specific names
            string skeletonRoot = (handType == SteamVR_Input_Sources.LeftHand) ? "slim_l/Root" : "slim_r/Root";
            string skeletonActionName = (handType == SteamVR_Input_Sources.LeftHand) ? "SkeletonLeftHand" : "SkeletonRightHand";

            // add behavior scripts to the hands
            handSkeleton = this.gameObject.AddComponent<SteamVR_Behaviour_Skeleton>();
            handSkeleton.skeletonRoot = this.gameObject.transform.Find(skeletonRoot);
            handSkeleton.inputSource = handType;
            handSkeleton.mirroring = (handType == SteamVR_Input_Sources.LeftHand) ? SteamVR_Behaviour_Skeleton.MirrorType.RightToLeft : SteamVR_Behaviour_Skeleton.MirrorType.None;
            handSkeleton.updatePose = false;
            handSkeleton.skeletonAction = SteamVR_Input.GetSkeletonAction("default", skeletonActionName, false);
            handSkeleton.fallbackCurlAction = SteamVR_Input.GetSingleAction("default", "Squeeze", false);

            // add fallback pose scripts
            Transform handFallback = this.gameObject.transform.Find("fallback");
            SteamVR_Skeleton_Poser handPoser = handFallback.gameObject.AddComponent<SteamVR_Skeleton_Poser>();
            handPoser.skeletonMainPose = SkeletonPose_FallbackRelaxedPose.GetInstance();
            handPoser.Initialize();
            handSkeleton.fallbackPoser = handPoser;
            handSkeleton.Initialize();
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
                    SteamVR_Utils.RigidTransform handTransform = new SteamVR_Utils.RigidTransform(KerbalVR.Core.GamePoses[deviceIndex].mDeviceToAbsoluteTracking);
                    this.gameObject.transform.position = KerbalVR.Scene.Instance.DevicePoseToWorld(handTransform.pos);
                    this.gameObject.transform.rotation = KerbalVR.Scene.Instance.DevicePoseToWorld(handTransform.rot);
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
            }
        }
    }
}
