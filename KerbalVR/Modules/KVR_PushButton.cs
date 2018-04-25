using System;
using System.Collections.Generic;
using UnityEngine;
using KerbalVR.Components;

namespace KerbalVR.Modules
{
    public class KVR_PushButton : InternalModule
    {
        #region KSP Config Fields
        [KSPField]
        public string coloredObject = string.Empty;
        #endregion

        #region Properties
        public List<KVR_Label> Labels { get; private set; } = new List<KVR_Label>();
        #endregion

        #region Private Members
        private ConfigNode moduleConfigNode;

        private KVR_Cover buttonCover;
        private KVR_Button pushButton;

        private GameObject coloredGameObject;
        private Material coloredGameObjectMaterial;
        #endregion
        

        protected void Start() {
            // no setup needed in editor mode
            if (HighLogic.LoadedScene == GameScenes.EDITOR) return;

            // obtain module configuration
            moduleConfigNode = ConfigUtils.GetModuleConfigNode(internalProp.name, moduleID);

            // if there's a cover, create it
            ConfigNode coverConfigNode = moduleConfigNode.GetNode("KVR_COVER");
            try {
                buttonCover = new KVR_Cover(internalProp, coverConfigNode);
            } catch (Exception e) {
#if DEBUG
                Utils.LogWarning("KVR_PushButton KVR_COVER exception: " + e.ToString());
#endif
            }

            // create the button
            ConfigNode buttonConfigNode = moduleConfigNode.GetNode("KVR_BUTTON");
            try {
                pushButton = new KVR_Button(internalProp, buttonConfigNode);
                pushButton.enabled = (buttonCover == null);
            } catch (Exception e) {
                throw e;
            }

            // special effects
            Transform coloredObjectTransform = internalProp.FindModelTransform(coloredObject);
            if (coloredObjectTransform != null) {
                coloredGameObject = coloredObjectTransform.gameObject;
                coloredGameObjectMaterial = coloredGameObject.GetComponent<MeshRenderer>().sharedMaterial;
                coloredGameObjectMaterial.SetColor(Shader.PropertyToID("_EmissiveColor"), Color.black);
            }

            // create labels
            CreateLabels();
        }

        public override void OnUpdate() {
            // update the button cover if available
            if (buttonCover != null) {
                buttonCover.Update();
                pushButton.enabled = (buttonCover.CurrentState == KVR_Cover.State.Open);
            }

            // update the button
            pushButton.Update();
        }

        private void CreateLabels() {
            // create labels from the module configuration
            ConfigNode[] labelNodes = moduleConfigNode.GetNodes("KVR_LABEL");
            for (int i = 0; i < labelNodes.Length; i++) {
                ConfigNode node = labelNodes[i];
                node.id = i.ToString();
                try {
                    KVR_Label label = new KVR_Label(internalProp, node);
                    Labels.Add(label);
                } catch (Exception e) {
                    Utils.LogWarning(e.ToString());
                }
            }
        }
    }
}
