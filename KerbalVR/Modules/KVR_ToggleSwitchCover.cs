using System;
using System.Collections.Generic;
using UnityEngine;
using KerbalVR.Components;

namespace KerbalVR.Modules
{
    public class KVR_ToggleSwitchCover : InternalModule, IActionableCollider
    {

        #region Types
        public enum SwitchState
        {
            Down,
            Up,
        }
        #endregion


        #region KSP Config Fields
        [KSPField]
        public string switchAnimationName = string.Empty;
        [KSPField]
        public string transformSwitchCollider = string.Empty;

        [KSPField]
        public string outputSignal = string.Empty;
        #endregion


        #region Properties
        public SwitchState CurrentSwitchState { get; private set; }
        public List<KVR_Label> Labels { get; private set; } = new List<KVR_Label>();
        #endregion


        #region Private Members
        private ConfigNode moduleConfigNode;

        private KVR_Cover buttonCover;

        private Animation switchAnimation;
        private AnimationState switchAnimationState;
        private GameObject switchGameObject;
        private float targetSwitchAnimationEndTime;
        #endregion


        void Start() {
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
                Utils.LogWarning("KVR_ToggleSwitch exception: " + e.ToString());
#endif
            }

            // retrieve the animations
            Animation[] animations = internalProp.FindModelAnimators(switchAnimationName);
            if (animations.Length > 0) {
                switchAnimation = animations[0];
                switchAnimationState = switchAnimation[switchAnimationName];
                switchAnimationState.wrapMode = WrapMode.Once;
            } else {
                Utils.LogWarning("KVR_ToggleSwitch (" + gameObject.name + ") has no animation \"" + switchAnimationName + "\"");
            }

            // retrieve the collider GameObjects
            Transform colliderTransform = internalProp.FindModelTransform(transformSwitchCollider);
            if (colliderTransform != null) {
                switchGameObject = colliderTransform.gameObject;
                switchGameObject.AddComponent<KVR_ActionableCollider>().module = this;
            } else {
                Utils.LogWarning("KVR_ToggleSwitch (" + gameObject.name + ") has no switch collider \"" + transformSwitchCollider + "\"");
            }

            // set initial state
            isSwitchAnimationPlayingPrev = false;
            targetSwitchAnimationEndTime = 0f;
            GoToSwitchState(SwitchState.Down);

            // create labels
            CreateLabels();
        }

        public override void OnUpdate() {
            // update the button cover if available
            if (buttonCover != null) {
                buttonCover.Update();
            }
        }

        public void OnColliderEntered(Collider thisObject, Collider otherObject) {
            if (DeviceManager.IsManipulator(otherObject.gameObject)) {

                if (thisObject.gameObject == switchGameObject && ((buttonCover == null) ||
                    (buttonCover != null && buttonCover.CurrentCoverState == KVR_Cover.State.Open))) {
                    // switch is pressed while cover is OPEN, and collider
                    // has entered from the top side of the switch
                    Vector3 manipulatorDeltaPos = switchGameObject.transform.InverseTransformPoint(
                        otherObject.transform.position);

                    if (manipulatorDeltaPos.z > 0f) {
                        PlayToSwitchState(SwitchState.Up);
                    }
                }
            }
        }

        public void OnColliderExited(Collider thisObject, Collider otherObject) {
            if (DeviceManager.IsManipulator(otherObject.gameObject)) {
                if (thisObject.gameObject == switchGameObject) {
                    PlayToSwitchState(SwitchState.Down);
                }
            }
        }

        public void SetSwitchState(SwitchState state) {
            CurrentSwitchState = state;

            if (!string.IsNullOrEmpty(outputSignal) && state == SwitchState.Up) {
                KerbalVR.Events.Avionics(outputSignal).Send();
            }
        }

        private void GoToSwitchState(SwitchState state) {
            SetSwitchState(state);

            // switch to animation state instantly
            switchAnimationState.normalizedTime = GetNormalizedTimeForSwitchState(state);
            switchAnimationState.speed = 0f;
            switchAnimation.Play(switchAnimationName);
        }

        private void PlayToSwitchState(SwitchState state) {

            // set the animation time that we want to play to
            targetSwitchAnimationEndTime = GetNormalizedTimeForSwitchState(state);

            // note that the normalizedTime always resets to zero after finishing the clip.
            // so if button was at Pressed and it was already done playing, its normalizedTime is
            // 0f, even though the Pressed state corresponds to a time of 1f. so, for this special
            // case, force it to 1f.
            if (CurrentSwitchState == SwitchState.Up &&
                switchAnimationState.normalizedTime == 0f &&
                !switchAnimation.isPlaying) {
                switchAnimationState.normalizedTime = 1f;
            }

            // move either up or down depending on where the switch is right now
            switchAnimationState.speed =
                Mathf.Sign(targetSwitchAnimationEndTime - switchAnimationState.normalizedTime) * 1f;

            // play animation and actuate switch
            switchAnimation.Play(switchAnimationName);
            SetSwitchState(state);
        }

        private float GetNormalizedTimeForSwitchState(SwitchState state) {
            float targetTime = 0f;
            switch (state) {
                case SwitchState.Up:
                    targetTime = 1f;
                    break;
                case SwitchState.Down:
                    targetTime = 0f;
                    break;
            }
            return targetTime;
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
