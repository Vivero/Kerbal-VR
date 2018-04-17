using System;
using System.Collections.Generic;
using UnityEngine;
using KerbalVR.Components;

namespace KerbalVR.Modules
{
    public class KVR_ToggleSwitch : InternalModule
    {
        public enum ActuationType {
            Momentary,
            LatchingTwoState,
            LatchingThreeState,
        }


        #region KSP Config Fields
        [KSPField]
        public string outputSignal = string.Empty;
        #endregion


        #region Properties
        public List<KVR_Label> Labels { get; private set; } = new List<KVR_Label>();
        #endregion


        #region Private Members
        private ConfigNode moduleConfigNode;

        private KVR_Cover buttonCover;
        private KVR_Switch toggleSwitch;
        #endregion


        void Start() {
            // no setup needed in editor mode
            if (HighLogic.LoadedScene == GameScenes.EDITOR) return;

            // obtain module configuration
            moduleConfigNode = ConfigUtils.GetModuleConfigNode(internalProp.name, moduleID);

            // if there's a cover, create it
            ConfigNode coverConfigNode = moduleConfigNode.GetNode("KVR_COVER");
            if (coverConfigNode != null) {
                buttonCover = new KVR_Cover(internalProp, coverConfigNode);
            }

            // create the switch
            ConfigNode switchConfigNode = moduleConfigNode.GetNode("KVR_SWITCH_MOMENTARY");
            if (switchConfigNode != null) {
                toggleSwitch = new KVR_SwitchMomentary(internalProp, switchConfigNode);
            }

            if (toggleSwitch == null) {
                switchConfigNode = moduleConfigNode.GetNode("KVR_SWITCH_TWO_STATE");
                if (switchConfigNode != null) {
                    toggleSwitch = new KVR_SwitchTwoState(internalProp, switchConfigNode);
                }
            }

            if (toggleSwitch == null) {
                switchConfigNode = moduleConfigNode.GetNode("KVR_SWITCH_THREE_STATE");
                if (switchConfigNode != null) {
                    toggleSwitch = new KVR_SwitchThreeState(internalProp, switchConfigNode);
                }
            }

            if (toggleSwitch == null)
                throw new ArgumentException("KVR_ToggleSwitch " + internalProp.name + " is missing a switch configuration!");
            toggleSwitch.enabled = (buttonCover == null);

            // create labels
            CreateLabels();
        }

        public override void OnUpdate() {
            // update the button cover if available
            if (buttonCover != null) {
                buttonCover.Update();
                toggleSwitch.enabled = (buttonCover.CurrentState == KVR_Cover.State.Open);
            }

            // update the button
            toggleSwitch.Update();
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
