using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    /// <summary>
    /// InteractionSystem is a singleton class that encapsulates
    /// the code that manages interaction systems via SteamVR_Input,
    /// i.e. the interaction game objects (hands), and input actions.
    /// Use the position of this GameObject as the origin for the
    /// interaction system devices (the VR controllers).
    /// </summary>
    public class InteractionSystem : MonoBehaviour
    {
        #region Singleton
        /// <summary>
        /// This is a singleton class, and there must be exactly one GameObject with this Component in the scene.
        /// </summary>
        private static InteractionSystem _instance;
        public static InteractionSystem Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<InteractionSystem>();
                    if (_instance == null) {
                        Utils.LogError("The scene needs to have one active GameObject with an InteractionSystem script attached!");
                    } else {
                        _instance.Initialize();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// One-time initialization for this singleton class.
        /// </summary>
        private void Initialize() {
            // load glove prefab assets
            glovePrefabL = AssetLoader.Instance.GetGameObject("vr_glove_left_model_slim");
            if (glovePrefabL == null) {
                Utils.LogWarning("Could not load prefab: vr_glove_left_model_slim");
                return;
            }
            glovePrefabR = AssetLoader.Instance.GetGameObject("vr_glove_right_model_slim");
            if (glovePrefabR == null) {
                Utils.LogWarning("Could not load prefab: vr_glove_right_model_slim");
                return;
            }

            // make instance objects out of them
            handL = Instantiate(glovePrefabL);
            if (handL == null) {
                Utils.LogWarning("Could not Instantiate prefab: vr_glove_left_model_slim");
                return;
            }
            handL.name = "KVR_HandL";
            DontDestroyOnLoad(handL);

            handR = Instantiate(glovePrefabR);
            if (handR == null) {
                Utils.LogWarning("Could not Instantiate prefab: vr_glove_right_model_slim");
                return;
            }
            handR.name = "KVR_HandR";
            DontDestroyOnLoad(handR);

            // cache the hand renderers
            Transform handSkinL = handL.transform.Find("slim_l/vr_glove_right_slim");
            handRendererL = handSkinL.gameObject.GetComponent<SkinnedMeshRenderer>();
            Transform handSkinR = handR.transform.Find("slim_r/vr_glove_right_slim");
            handRendererR = handSkinR.GetComponent<SkinnedMeshRenderer>();

            // initialize visibility/layer
            SetHandsVisible(false);
            SetHandsLayer(0);
        }
        #endregion


        #region Private Members
        // hand game objects
        protected GameObject glovePrefabL, handL;
        protected GameObject glovePrefabR, handR;
        protected SteamVR_Behaviour_Skeleton handSkeletonL, handSkeletonR;
        protected SteamVR_Skeleton_Poser handPoserL, handPoserR;
        protected SkinnedMeshRenderer handRendererL, handRendererR;

        // device behaviors and actions
        protected bool isInputInitialized = false;
        protected SteamVR_Action_Pose handActionPose;
        protected SteamVR_Action_Boolean teleportAction;
        protected SteamVR_Action_Boolean headsetOnAction;

        // hand render state
        protected Types.ShiftRegister<bool> isRenderingHands = new Types.ShiftRegister<bool>(2);
        protected Types.ShiftRegister<int> renderLayerHands = new Types.ShiftRegister<int>(2);

        // teleport system
        protected GameObject teleportSystemGameObject;
        protected TeleportSystem teleportSystem;
        #endregion


        protected void Update() {
            // initialize hand scripts (need OpenVR and SteamVR_Input already initialized)
            if (!isInputInitialized && KerbalVR.Core.IsOpenVrReady) {
                InitializeHandScripts();
                InitializeInputScripts();
                isInputInitialized = true;
            }
            if (!isInputInitialized) {
                return;
            }

            // should we render the hands in the current scene?
            if (KerbalVR.Core.IsVrRunning) {
                switch (HighLogic.LoadedScene) {
                    case GameScenes.MAINMENU:
                    case GameScenes.EDITOR:
                        isRenderingHands.Push(true);
                        renderLayerHands.Push(0);
                        break;

                    case GameScenes.FLIGHT:
                        if (KerbalVR.Scene.IsInEVA() || KerbalVR.Scene.IsInIVA()) {
                            isRenderingHands.Push(true);

                            if (KerbalVR.Scene.IsInIVA()) {
                                // IVA-specific settings
                                renderLayerHands.Push(20);
                            }
                            if (KerbalVR.Scene.IsInEVA()) {
                                // EVA-specific settings
                                renderLayerHands.Push(0);
                            }
                        } else {
                            isRenderingHands.Push(false);
                        }
                        break;
                }
            } else {
                isRenderingHands.Push(false);
            }

            // makes changes as necessary
            if (isRenderingHands.IsChanged()) {
                SetHandsVisible(isRenderingHands.Value);
            }
            if (renderLayerHands.IsChanged()) {
                SetHandsLayer(renderLayerHands.Value);
            }

            // if rendering, update the hand positions
            if (isRenderingHands.Value) {
                // get device indices for each hand, then set the transform
                bool isConnected = handActionPose.GetDeviceIsConnected(SteamVR_Input_Sources.LeftHand);
                uint deviceIndex = handActionPose.GetDeviceIndex(SteamVR_Input_Sources.LeftHand);
                if (isConnected && deviceIndex < OpenVR.k_unMaxTrackedDeviceCount) {
                    SteamVR_Utils.RigidTransform handTransform = new SteamVR_Utils.RigidTransform(KerbalVR.Core.GamePoses[deviceIndex].mDeviceToAbsoluteTracking);
                    handL.transform.position = KerbalVR.Scene.Instance.DevicePoseToWorld(handTransform.pos);
                    handL.transform.rotation = KerbalVR.Scene.Instance.DevicePoseToWorld(handTransform.rot);
                }

                isConnected = handActionPose.GetDeviceIsConnected(SteamVR_Input_Sources.RightHand);
                deviceIndex = handActionPose.GetDeviceIndex(SteamVR_Input_Sources.RightHand);
                if (isConnected && deviceIndex < OpenVR.k_unMaxTrackedDeviceCount) {
                    SteamVR_Utils.RigidTransform handTransform = new SteamVR_Utils.RigidTransform(KerbalVR.Core.GamePoses[deviceIndex].mDeviceToAbsoluteTracking);
                    handR.transform.position = KerbalVR.Scene.Instance.DevicePoseToWorld(handTransform.pos);
                    handR.transform.rotation = KerbalVR.Scene.Instance.DevicePoseToWorld(handTransform.rot);
                }
            }

            // position the teleport system
            if (HighLogic.LoadedScene == GameScenes.MAINMENU) {
                teleportSystem.downwardsVector = Vector3.down;
            }
            else if (HighLogic.LoadedScene == GameScenes.FLIGHT && FlightGlobals.ActiveVessel != null) {
                // assign the teleport system's down vector to point towards gravity
                CelestialBody mainBody = FlightGlobals.ActiveVessel.mainBody;
                Vector2d latLon = mainBody.GetLatitudeAndLongitude(teleportSystemGameObject.transform.position);
                teleportSystem.downwardsVector = -mainBody.GetSurfaceNVector(latLon.x, latLon.y);
            }
        }

        protected void InitializeHandScripts() {
            // add behavior scripts to the hands
            handSkeletonL = handL.AddComponent<SteamVR_Behaviour_Skeleton>();
            handSkeletonL.skeletonRoot = handL.transform.Find("slim_l/Root");
            handSkeletonL.inputSource = SteamVR_Input_Sources.LeftHand;
            handSkeletonL.mirroring = SteamVR_Behaviour_Skeleton.MirrorType.RightToLeft;
            handSkeletonL.updatePose = false;
            handSkeletonL.skeletonAction = SteamVR_Input.GetSkeletonAction("default", "SkeletonLeftHand", false);
            handSkeletonL.fallbackCurlAction = SteamVR_Input.GetSingleAction("default", "Squeeze", false);

            handSkeletonR = handR.AddComponent<SteamVR_Behaviour_Skeleton>();
            handSkeletonR.skeletonRoot = handR.transform.Find("slim_r/Root");
            handSkeletonR.inputSource = SteamVR_Input_Sources.RightHand;
            handSkeletonR.mirroring = SteamVR_Behaviour_Skeleton.MirrorType.None;
            handSkeletonR.updatePose = false;
            handSkeletonR.skeletonAction = SteamVR_Input.GetSkeletonAction("default", "SkeletonRightHand", false);
            handSkeletonR.fallbackCurlAction = SteamVR_Input.GetSingleAction("default", "Squeeze", false);

            // add fallback pose scripts
            Transform handFallbackL = handL.transform.Find("fallback");
            handPoserL = handFallbackL.gameObject.AddComponent<SteamVR_Skeleton_Poser>();
            handPoserL.skeletonMainPose = SkeletonPose_FallbackRelaxedPose.GetInstance();
            handPoserL.Initialize();
            handSkeletonL.fallbackPoser = handPoserL;

            Transform handFallbackR = handR.transform.Find("fallback");
            handPoserR = handFallbackR.gameObject.AddComponent<SteamVR_Skeleton_Poser>();
            handPoserR.skeletonMainPose = SkeletonPose_FallbackRelaxedPose.GetInstance();
            handPoserR.Initialize();
            handSkeletonR.fallbackPoser = handPoserR;

            // can init the skeleton behavior now
            handSkeletonL.Initialize();
            handSkeletonR.Initialize();
        }

        protected void InitializeInputScripts() {
            // store actions for these devices
            handActionPose = SteamVR_Input.GetPoseAction("default", "Pose");
            headsetOnAction = SteamVR_Input.GetBooleanAction("default", "HeadsetOnHead");
            headsetOnAction.onChange += OnChangeHeadsetOnAction;
            teleportAction = SteamVR_Input.GetBooleanAction("EVA", "Teleport");

            // init the teleport system
            teleportSystemGameObject = new GameObject("KVR_TeleportSystem");
            teleportSystem = teleportSystemGameObject.AddComponent<TeleportSystem>();
            teleportSystem.handOriginLeft = handL.transform;
            teleportSystem.handOriginRight = handR.transform;
            DontDestroyOnLoad(teleportSystemGameObject);
        }

        /// <summary>
        /// Activate or deactivate VR when the headset is worn or not, respectively.
        /// </summary
        /// <param name="fromAction">The HeadsetOnHead action</param>
        /// <param name="fromSource">The source for the event</param>
        /// <param name="newState">True if the headset is being worn by the user, false otherwise</param>
        protected void OnChangeHeadsetOnAction(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState) {
            KerbalVR.Core.IsVrEnabled = newState;
        }

        public void SetHandsVisible(bool isVisible) {
            handRendererL.enabled = isVisible;
            handRendererR.enabled = isVisible;
        }

        public void SetHandsLayer(int layer) {
            handRendererL.gameObject.layer = layer;
            handRendererR.gameObject.layer = layer;
        }

    } // class InteractionSystem
} // namespace KerbalVR
