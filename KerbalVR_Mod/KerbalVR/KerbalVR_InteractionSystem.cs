using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    /// <summary>
    /// InteractionSystem is a singleton class that encapsulates
    /// the code that manages interaction systems via SteamVR_Input,
    /// i.e. the interaction game objects (gloves), and input actions.
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
            gloveL = Instantiate(glovePrefabL);
            if (gloveL == null) {
                Utils.LogWarning("Could not Instantiate prefab: vr_glove_left_model_slim");
                return;
            }
            gloveL.name = "KVR_GloveL";
            DontDestroyOnLoad(gloveL);

            gloveR = Instantiate(glovePrefabR);
            if (gloveR == null) {
                Utils.LogWarning("Could not Instantiate prefab: vr_glove_right_model_slim");
                return;
            }
            gloveR.name = "KVR_GloveR";
            DontDestroyOnLoad(gloveR);

            // cache the glove renderers
            Transform gloveSkinL = gloveL.transform.Find("slim_l/vr_glove_right_slim");
            gloveRendererL = gloveSkinL.gameObject.GetComponent<SkinnedMeshRenderer>();
            Transform gloveSkinR = gloveR.transform.Find("slim_r/vr_glove_right_slim");
            gloveRendererR = gloveSkinR.GetComponent<SkinnedMeshRenderer>();

            // initialize visibility/layer
            SetGlovesVisible(false);
            SetGlovesLayer(0);
        }
        #endregion


        #region Properties
        #endregion


        #region Private Members
        // glove game objects
        protected GameObject glovePrefabL, gloveL;
        protected GameObject glovePrefabR, gloveR;
        protected SteamVR_Behaviour_Skeleton gloveSkeletonL, gloveSkeletonR;
        protected SteamVR_Skeleton_Poser glovePoserL, glovePoserR;
        protected SkinnedMeshRenderer gloveRendererL, gloveRendererR;

        // device behaviors and actions
        protected bool isInputInitialized = false;
        protected SteamVR_Action_Pose gloveActionPose;
        protected SteamVR_Action_Boolean teleportAction;
        protected SteamVR_Action_Boolean headsetOnAction;

        // glove render state
        protected Types.ShiftRegister<bool> isRenderingGloves = new Types.ShiftRegister<bool>(2);
        protected Types.ShiftRegister<int> renderLayerGloves = new Types.ShiftRegister<int>(2);

        // teleport system
        protected GameObject teleportSystemGameObject;
        protected TeleportSystem teleportSystem;
        #endregion


        protected void Update() {
            // initialize glove scripts (need OpenVR and SteamVR_Input already initialized)
            if (!isInputInitialized && KerbalVR.Core.IsOpenVrReady) {
                InitializeGloveScripts();
                InitializeInputScripts();
                isInputInitialized = true;
            }
            if (!isInputInitialized) {
                return;
            }

            // should we render the gloves in the current scene?
            if (KerbalVR.Core.IsVrRunning) {
                switch (HighLogic.LoadedScene) {
                    case GameScenes.MAINMENU:
                    case GameScenes.EDITOR:
                        isRenderingGloves.Push(true);
                        renderLayerGloves.Push(0);
                        break;

                    case GameScenes.FLIGHT:
                        if (KerbalVR.Scene.IsInEVA() || KerbalVR.Scene.IsInIVA()) {
                            isRenderingGloves.Push(true);

                            if (KerbalVR.Scene.IsInIVA()) {
                                // IVA-specific settings
                                renderLayerGloves.Push(20);
                            }
                            if (KerbalVR.Scene.IsInEVA()) {
                                // EVA-specific settings
                                renderLayerGloves.Push(0);
                            }
                        }
                        break;
                }
            }

            // makes changes as necessary
            if (isRenderingGloves.IsChanged()) {
                SetGlovesVisible(isRenderingGloves.Value);
            }
            if (renderLayerGloves.IsChanged()) {
                SetGlovesLayer(renderLayerGloves.Value);
            }

            // position the teleport system
            teleportSystemGameObject.transform.position = gloveSkeletonR.origin.transform.TransformPoint(gloveActionPose[SteamVR_Input_Sources.RightHand].localPosition);
            teleportSystemGameObject.transform.rotation = gloveSkeletonR.origin.rotation * gloveActionPose[SteamVR_Input_Sources.RightHand].localRotation;

            if (HighLogic.LoadedScene == GameScenes.FLIGHT && FlightGlobals.ActiveVessel != null) {
                // assign the teleport system's down vector to point towards gravity
                CelestialBody mainBody = FlightGlobals.ActiveVessel.mainBody;
                Vector2d latLon = mainBody.GetLatitudeAndLongitude(teleportSystemGameObject.transform.position);
                teleportSystem.downwardsVector = -mainBody.GetSurfaceNVector(latLon.x, latLon.y);
            }

            // set the origin for the controller space
            this.transform.position = Scene.Instance.CurrentPosition;
            this.transform.rotation = Scene.Instance.CurrentRotation;
        }

        protected void InitializeGloveScripts() {
            // add behavior scripts to the gloves
            gloveSkeletonL = gloveL.AddComponent<SteamVR_Behaviour_Skeleton>();
            gloveSkeletonL.skeletonRoot = gloveL.transform.Find("slim_l/Root");
            gloveSkeletonL.inputSource = SteamVR_Input_Sources.LeftHand;
            gloveSkeletonL.mirroring = SteamVR_Behaviour_Skeleton.MirrorType.RightToLeft;
            gloveSkeletonL.origin = this.transform;
            gloveSkeletonL.skeletonAction = SteamVR_Input.GetSkeletonAction("default", "SkeletonLeftHand", false);
            gloveSkeletonL.fallbackCurlAction = SteamVR_Input.GetSingleAction("default", "Squeeze", false);

            gloveSkeletonR = gloveR.AddComponent<SteamVR_Behaviour_Skeleton>();
            gloveSkeletonR.skeletonRoot = gloveR.transform.Find("slim_r/Root");
            gloveSkeletonR.inputSource = SteamVR_Input_Sources.RightHand;
            gloveSkeletonR.mirroring = SteamVR_Behaviour_Skeleton.MirrorType.None;
            gloveSkeletonR.origin = this.transform;
            gloveSkeletonR.skeletonAction = SteamVR_Input.GetSkeletonAction("default", "SkeletonRightHand", false);
            gloveSkeletonR.fallbackCurlAction = SteamVR_Input.GetSingleAction("default", "Squeeze", false);

            // add fallback pose scripts
            Transform gloveFallbackL = gloveL.transform.Find("fallback");
            glovePoserL = gloveFallbackL.gameObject.AddComponent<SteamVR_Skeleton_Poser>();
            glovePoserL.skeletonMainPose = GenerateHandSkeletonMainPose();
            glovePoserL.Initialize();
            gloveSkeletonL.fallbackPoser = glovePoserL;

            Transform gloveFallbackR = gloveR.transform.Find("fallback");
            glovePoserR = gloveFallbackR.gameObject.AddComponent<SteamVR_Skeleton_Poser>();
            glovePoserR.skeletonMainPose = GenerateHandSkeletonMainPose();
            glovePoserR.Initialize();
            gloveSkeletonR.fallbackPoser = glovePoserR;

            // can init the skeleton behavior now
            gloveSkeletonL.Initialize();
            gloveSkeletonR.Initialize();
        }

        protected void InitializeInputScripts() {
            // store actions for these devices
            gloveActionPose = SteamVR_Input.GetPoseAction("default", "Pose");
            headsetOnAction = SteamVR_Input.GetBooleanAction("default", "HeadsetOnHead");
            headsetOnAction.onChange += OnChangeHeadsetOnAction;
            teleportAction = SteamVR_Input.GetBooleanAction("EVA", "Teleport");

            // init the teleport system
            teleportSystemGameObject = new GameObject("KVR_TeleportSystem");
            teleportSystem = teleportSystemGameObject.AddComponent<TeleportSystem>();
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

        protected SteamVR_Skeleton_Pose GenerateHandSkeletonMainPose() {
            SteamVR_Skeleton_Pose pose = ScriptableObject.CreateInstance<SteamVR_Skeleton_Pose>();
            pose.leftHand.bonePositions = new Vector3[] {
                new Vector3(0f, 0f, 0f),
                new Vector3(-0.034037687f, 0.03650266f, 0.16472164f),
                new Vector3(-0.012083233f, 0.028070247f, 0.025049694f),
                new Vector3(0.040405963f, -0.000000051561553f, 0.000000045447194f),
                new Vector3(0.032516792f, -0.000000051137583f, -0.000000012933195f),
                new Vector3(0.030463902f, 0.00000016269207f, 0.0000000792839f),
                new Vector3(0.0006324522f, 0.026866155f, 0.015001948f),
                new Vector3(0.074204385f, 0.005002201f, -0.00023377323f),
                new Vector3(0.043930072f, 0.000000059567498f, 0.00000018367103f),
                new Vector3(0.02869547f, -0.00000009398158f, -0.00000012649753f),
                new Vector3(0.022821384f, -0.00000014365155f, 0.00000007651614f),
                new Vector3(0.0021773134f, 0.007119544f, 0.016318738f),
                new Vector3(0.07095288f, -0.00077883265f, -0.000997186f),
                new Vector3(0.043108486f, -0.00000009950596f, -0.0000000067041825f),
                new Vector3(0.033266045f, -0.00000001320567f, -0.000000021670374f),
                new Vector3(0.025892371f, 0.00000009984198f, -0.0000000020352908f),
                new Vector3(0.0005134356f, -0.0065451227f, 0.016347693f),
                new Vector3(0.06587581f, -0.0017857892f, -0.00069344096f),
                new Vector3(0.04069671f, -0.000000095347104f, -0.000000022934731f),
                new Vector3(0.028746964f, 0.00000010089892f, 0.000000045306827f),
                new Vector3(0.022430236f, 0.00000010846127f, -0.000000017428562f),
                new Vector3(-0.002478151f, -0.01898137f, 0.015213584f),
                new Vector3(0.0628784f, -0.0028440945f, -0.0003315112f),
                new Vector3(0.030219711f, -0.00000003418319f, -0.00000009332872f),
                new Vector3(0.018186597f, -0.0000000050220166f, -0.00000020934549f),
                new Vector3(0.01801794f, -0.0000000200012f, 0.0000000659746f),
                new Vector3(-0.0060591106f, 0.05628522f, 0.060063843f),
                new Vector3(-0.04041555f, -0.043017667f, 0.019344581f),
                new Vector3(-0.03935372f, -0.07567404f, 0.047048334f),
                new Vector3(-0.038340144f, -0.09098663f, 0.08257892f),
                new Vector3(-0.031805996f, -0.08721431f, 0.12101539f),
            };
            pose.leftHand.boneRotations = new Quaternion[] {
                new Quaternion(-6.123234e-17f, 1f, 6.123234e-17f, -0.00000004371139f),
                new Quaternion(-0.078608155f, -0.92027926f, 0.3792963f, -0.055146642f),
                new Quaternion(-0.24104308f, -0.76422274f, 0.45859465f, 0.38412613f),
                new Quaternion(0.085189685f, 0.0000513494f, -0.28143752f, 0.95579064f),
                new Quaternion(0.0052029183f, -0.021480577f, -0.15888694f, 0.9870494f),
                new Quaternion(-1.3877788e-17f, -1.3877788e-17f, -5.551115e-17f, 1f),
                new Quaternion(-0.6442515f, -0.42213318f, -0.4782025f, 0.42197865f),
                new Quaternion(0.08568421f, 0.023565516f, -0.19161178f, 0.9774394f),
                new Quaternion(0.045650285f, 0.0043684426f, -0.095879465f, 0.99433607f),
                new Quaternion(-0.0020507684f, 0.022764975f, -0.15681197f, 0.987364f),
                new Quaternion(6.938894e-18f, 1.9428903e-16f, -1.348151e-33f, 1f),
                new Quaternion(-0.546723f, -0.46074906f, -0.44252017f, 0.54127645f),
                new Quaternion(-0.17867392f, 0.047816366f, -0.24333772f, 0.9521429f),
                new Quaternion(0.020366715f, -0.010060345f, -0.21893612f, 0.9754748f),
                new Quaternion(-0.010457605f, 0.026426358f, -0.19179714f, 0.981023f),
                new Quaternion(1.1639192e-17f, -5.602331e-17f, -0.040125635f, 0.9991947f),
                new Quaternion(-0.5166922f, -0.4298879f, -0.49554786f, 0.5501435f),
                new Quaternion(-0.17289871f, 0.114340894f, -0.29726714f, 0.93202174f),
                new Quaternion(-0.0021954547f, -0.000443071f, -0.22544385f, 0.9742536f),
                new Quaternion(-0.00472193f, 0.011803731f, -0.35618067f, 0.93433064f),
                new Quaternion(6.938894e-18f, -9.62965e-35f, -1.3877788e-17f, 1f),
                new Quaternion(-0.5269183f, -0.32674035f, -0.5840246f, 0.52394f),
                new Quaternion(-0.2006022f, 0.15258452f, -0.36497858f, 0.8962519f),
                new Quaternion(0.0018557907f, 0.0004098564f, -0.25201905f, 0.96772045f),
                new Quaternion(-0.019474672f, 0.048342716f, -0.26703015f, 0.9622778f),
                new Quaternion(0f, 0f, 1.9081958e-17f, 1f),
                new Quaternion(0.20274544f, 0.59426665f, 0.2494411f, 0.73723847f),
                new Quaternion(0.6235274f, -0.66380864f, -0.29373443f, -0.29033053f),
                new Quaternion(0.6780625f, -0.6592852f, -0.26568344f, -0.18704711f),
                new Quaternion(0.7367927f, -0.6347571f, -0.14393571f, -0.18303718f),
                new Quaternion(0.7584072f, -0.6393418f, -0.12667806f, -0.0036594148f),
            };
            pose.rightHand.bonePositions = new Vector3[] {
                new Vector3(0f, 0f, 0f),
                new Vector3(-0.034037687f, 0.03650266f, 0.16472164f),
                new Vector3(-0.012083233f, 0.028070247f, 0.025049694f),
                new Vector3(0.040405963f, -0.000000051561553f, 0.000000045447194f),
                new Vector3(0.032516792f, -0.000000051137583f, -0.000000012933195f),
                new Vector3(0.030463902f, 0.00000016269207f, 0.0000000792839f),
                new Vector3(0.0006324522f, 0.026866155f, 0.015001948f),
                new Vector3(0.074204385f, 0.005002201f, -0.00023377323f),
                new Vector3(0.043930072f, 0.000000059567498f, 0.00000018367103f),
                new Vector3(0.02869547f, -0.00000009398158f, -0.00000012649753f),
                new Vector3(0.022821384f, -0.00000014365155f, 0.00000007651614f),
                new Vector3(0.0021773134f, 0.007119544f, 0.016318738f),
                new Vector3(0.07095288f, -0.00077883265f, -0.000997186f),
                new Vector3(0.043108486f, -0.00000009950596f, -0.0000000067041825f),
                new Vector3(0.033266045f, -0.00000001320567f, -0.000000021670374f),
                new Vector3(0.025892371f, 0.00000009984198f, -0.0000000020352908f),
                new Vector3(0.0005134356f, -0.0065451227f, 0.016347693f),
                new Vector3(0.06587581f, -0.0017857892f, -0.00069344096f),
                new Vector3(0.04069671f, -0.000000095347104f, -0.000000022934731f),
                new Vector3(0.028746964f, 0.00000010089892f, 0.000000045306827f),
                new Vector3(0.022430236f, 0.00000010846127f, -0.000000017428562f),
                new Vector3(-0.002478151f, -0.01898137f, 0.015213584f),
                new Vector3(0.0628784f, -0.0028440945f, -0.0003315112f),
                new Vector3(0.030219711f, -0.00000003418319f, -0.00000009332872f),
                new Vector3(0.018186597f, -0.0000000050220166f, -0.00000020934549f),
                new Vector3(0.01801794f, -0.0000000200012f, 0.0000000659746f),
                new Vector3(-0.0060591106f, 0.05628522f, 0.060063843f),
                new Vector3(-0.04041555f, -0.043017667f, 0.019344581f),
                new Vector3(-0.03935372f, -0.07567404f, 0.047048334f),
                new Vector3(-0.038340144f, -0.09098663f, 0.08257892f),
                new Vector3(-0.031805996f, -0.08721431f, 0.12101539f),
            };
            pose.rightHand.boneRotations = new Quaternion[] {
                new Quaternion(-6.123234e-17f, 1f, 6.123234e-17f, -0.00000004371139f),
                new Quaternion(-0.078608155f, -0.92027926f, 0.3792963f, -0.055146642f),
                new Quaternion(-0.24104308f, -0.76422274f, 0.45859465f, 0.38412613f),
                new Quaternion(0.085189685f, 0.0000513494f, -0.28143752f, 0.95579064f),
                new Quaternion(0.0052029183f, -0.021480577f, -0.15888694f, 0.9870494f),
                new Quaternion(-1.3877788e-17f, -1.3877788e-17f, -5.551115e-17f, 1f),
                new Quaternion(-0.6442515f, -0.42213318f, -0.4782025f, 0.42197865f),
                new Quaternion(0.08568421f, 0.023565516f, -0.19161178f, 0.9774394f),
                new Quaternion(0.045650285f, 0.0043684426f, -0.095879465f, 0.99433607f),
                new Quaternion(-0.0020507684f, 0.022764975f, -0.15681197f, 0.987364f),
                new Quaternion(6.938894e-18f, 1.9428903e-16f, -1.348151e-33f, 1f),
                new Quaternion(-0.546723f, -0.46074906f, -0.44252017f, 0.54127645f),
                new Quaternion(-0.17867392f, 0.047816366f, -0.24333772f, 0.9521429f),
                new Quaternion(0.020366715f, -0.010060345f, -0.21893612f, 0.9754748f),
                new Quaternion(-0.010457605f, 0.026426358f, -0.19179714f, 0.981023f),
                new Quaternion(1.1639192e-17f, -5.602331e-17f, -0.040125635f, 0.9991947f),
                new Quaternion(-0.5166922f, -0.4298879f, -0.49554786f, 0.5501435f),
                new Quaternion(-0.17289871f, 0.114340894f, -0.29726714f, 0.93202174f),
                new Quaternion(-0.0021954547f, -0.000443071f, -0.22544385f, 0.9742536f),
                new Quaternion(-0.00472193f, 0.011803731f, -0.35618067f, 0.93433064f),
                new Quaternion(6.938894e-18f, -9.62965e-35f, -1.3877788e-17f, 1f),
                new Quaternion(-0.5269183f, -0.32674035f, -0.5840246f, 0.52394f),
                new Quaternion(-0.2006022f, 0.15258452f, -0.36497858f, 0.8962519f),
                new Quaternion(0.0018557907f, 0.0004098564f, -0.25201905f, 0.96772045f),
                new Quaternion(-0.019474672f, 0.048342716f, -0.26703015f, 0.9622778f),
                new Quaternion(0f, 0f, 1.9081958e-17f, 1f),
                new Quaternion(0.20274544f, 0.59426665f, 0.2494411f, 0.73723847f),
                new Quaternion(0.6235274f, -0.66380864f, -0.29373443f, -0.29033053f),
                new Quaternion(0.6780625f, -0.6592852f, -0.26568344f, -0.18704711f),
                new Quaternion(0.7367927f, -0.6347571f, -0.14393571f, -0.18303718f),
                new Quaternion(0.7584072f, -0.6393418f, -0.12667806f, -0.0036594148f),
            };
            return pose;
        }

        public void SetGlovesVisible(bool isVisible) {
            gloveRendererL.enabled = isVisible;
            gloveRendererR.enabled = isVisible;
        }

        public void SetGlovesLayer(int layer) {
            gloveRendererL.gameObject.layer = layer;
            gloveRendererR.gameObject.layer = layer;
        }

    } // class InteractionSystem
} // namespace KerbalVR
