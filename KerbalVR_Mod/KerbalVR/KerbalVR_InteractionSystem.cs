using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    /// <summary>
    /// InteractionSystem is a singleton class that encapsulates
    /// the code that manages interaction systems via SteamVR_Input,
    /// i.e. the interaction game objects (gloves), and input actions.
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
            // glovePrefabL = AssetLoader.Instance.GetGameObject("vr_glove_left_model_slim");
            glovePrefabL = AssetLoader.Instance.GetGameObject("vr_glove_left");
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
            // Transform gloveSkinL = gloveL.transform.Find("vr_glove_model/renderMesh0");
            gloveRendererL = gloveSkinL.gameObject.GetComponent<SkinnedMeshRenderer>();
            Transform gloveSkinR = gloveR.transform.Find("slim_r/vr_glove_right_slim");
            gloveRendererR = gloveSkinR.GetComponent<SkinnedMeshRenderer>();

            // initialize visibility/layer
            SetGlovesVisible(true);
            SetGlovesLayer(0);
        }
        #endregion


        #region Properties
        #endregion


        #region Private Members
        protected GameObject glovePrefabL, gloveL;
        protected GameObject glovePrefabR, gloveR;
        protected SteamVR_Behaviour_Skeleton gloveSkeletonL, gloveSkeletonR;
        protected SkinnedMeshRenderer gloveRendererL, gloveRendererR;
        protected bool isGloveInputInitialized = false;
        #endregion


        protected void Update() {
            string logMsg = "";
            SteamVR_ActionSet[] actionSets = SteamVR_Input.GetActionSets();
            foreach (var a in actionSets) {
                logMsg += a.fullPath + " " + (a.IsActive(SteamVR_Input_Sources.Any) ? "active" : "not active") + "\n";
                foreach (var b in a.allActions) {
                    logMsg += "* " + b.GetShortName() + " " + (b.active ? "active" : "inactive") + " " + (b.activeBinding ? "activeBinding" : "inactiveBinding") + "\n";
                }
            }
            Utils.SetDebugText(logMsg);

            // initialize glove scripts (need OpenVR and SteamVR_Input already initialized)
            if (!isGloveInputInitialized && KerbalVR.Core.IsOpenVrReady) {
                InitializeGloveScripts();
            }
            if (!isGloveInputInitialized) {
                return;
            }

            // should we render the gloves in the current scene?
            if (KerbalVR.Core.IsVrRunning) {
                SetGlovesVisible(true);
            }
            else {
                switch (HighLogic.LoadedScene) {
                    case GameScenes.FLIGHT:
                        if (FlightGlobals.ActiveVessel.isEVA) {
                            SetGlovesVisible(true);
                            SetGlovesLayer(20);
                        }
                        break;

                    default:
                        //SetGlovesVisible(false);
                        break;
                }
            }
        }

        protected void InitializeGloveScripts() {
            // add behavior scripts to the gloves
            gloveSkeletonL = gloveL.AddComponent<SteamVR_Behaviour_Skeleton>();
            gloveSkeletonL.skeletonRoot = gloveL.transform.Find("slim_l/Root");
            // gloveSkeletonL.skeletonRoot = gloveL.transform.Find("vr_glove_model/Root");
            gloveSkeletonL.skeletonAction = SteamVR_Input.GetSkeletonAction("default", "SkeletonLeftHand", false);
            gloveSkeletonL.fallbackCurlAction = SteamVR_Input.GetSingleAction("default", "Squeeze", false);
            gloveSkeletonL.Initialize();

            gloveSkeletonR = gloveR.AddComponent<SteamVR_Behaviour_Skeleton>();
            gloveSkeletonR.skeletonRoot = gloveR.transform.Find("slim_r/Root");
            gloveSkeletonR.skeletonAction = SteamVR_Input.GetSkeletonAction("default", "SkeletonRightHand", false);
            gloveSkeletonR.fallbackCurlAction = SteamVR_Input.GetSingleAction("default", "Squeeze", false);
            gloveSkeletonR.Initialize();

            isGloveInputInitialized = true;
        }

        public void SetGlovesVisible(bool isVisible) {
            gloveRendererL.enabled = isVisible;
            gloveRendererR.enabled = isVisible;
        }

        public void SetGlovesLayer(int layer) {
            // Utils.SetLayer(gloveL, layer);
            // Utils.SetLayer(gloveR, layer);
            gloveRendererL.gameObject.layer = layer;
            gloveRendererR.gameObject.layer = layer;
        }

    } // class InteractionSystem
} // namespace KerbalVR
