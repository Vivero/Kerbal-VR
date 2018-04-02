using System;
using System.Collections.Generic;
using UnityEngine;
using KerbalVR.Components;

namespace KerbalVR.Modules
{
    public class KVR_PushButton : InternalModule, IActionableCollider
    {

        #region Types
        public enum ButtonState {
            Unpressed,
            Pressed,
        }

        public enum ButtonStateInput
        {
            ColliderEnter,
            ColliderExit,
            FinishedAction,
        }

        public enum ButtonFSMState
        {
            IsUnpressed,
            IsPressing,
            IsPressed,
            IsUnpressing,
        }
        #endregion

        #region KSP Config Fields
        [KSPField]
        public string buttonAnimationName = string.Empty;
        [KSPField]
        public string transformButtonCollider = string.Empty;
        [KSPField]
        public string coloredObject = string.Empty;
        #endregion

        #region Properties
        public ButtonState CurrentButtonState { get; private set; }
        public List<KVR_Label> Labels { get; private set; } = new List<KVR_Label>();
        #endregion

        #region Private Members
        private ConfigNode moduleConfigNode;

        private KVR_Cover buttonCover;

        private Animation buttonAnimation;
        private AnimationState buttonAnimationState;
        private GameObject buttonGameObject;
        private ButtonFSMState buttonFSMState;
        private float targetButtonAnimationEndTime;
        private bool isButtonAnimationPlayingPrev;

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
                Utils.LogWarning("KVR_PushButton exception: " + e.ToString());
#endif
            }

            // retrieve the animations
            Animation[] animations = internalProp.FindModelAnimators(buttonAnimationName);
            if (animations.Length > 0) {
                buttonAnimation = animations[0];
                buttonAnimationState = buttonAnimation[buttonAnimationName];
                buttonAnimationState.wrapMode = WrapMode.Once;
            } else {
                Utils.LogWarning("KVR_PushButton (" + gameObject.name + ") has no animation \"" + buttonAnimationName + "\"");
            }

            // retrieve the collider GameObjects
            Transform colliderTransform = internalProp.FindModelTransform(transformButtonCollider);
            if (colliderTransform != null) {
                buttonGameObject = colliderTransform.gameObject;
                buttonGameObject.AddComponent<KVR_ActionableCollider>().module = this;
            } else {
                Utils.LogWarning("KVR_PushButton (" + gameObject.name + ") has no button collider \"" + transformButtonCollider + "\"");
            }

            // special effects
            Transform coloredObjectTransform = internalProp.FindModelTransform(coloredObject);
            if (coloredObjectTransform != null) {
                coloredGameObject = coloredObjectTransform.gameObject;
                coloredGameObjectMaterial = coloredGameObject.GetComponent<MeshRenderer>().sharedMaterial;
                coloredGameObjectMaterial.SetColor(Shader.PropertyToID("_EmissiveColor"), Color.black);
            }

            // set initial state
            isButtonAnimationPlayingPrev = false;
            buttonFSMState = ButtonFSMState.IsUnpressed;
            targetButtonAnimationEndTime = 0f;
            GoToButtonState(ButtonState.Unpressed);

            // create labels
            CreateLabels();
        }

        public override void OnUpdate() {
            // update the button cover if available
            if (buttonCover != null) {
                buttonCover.Update();
            }
            
            bool isButtonAnimationPlaying = buttonAnimation.isPlaying;
            if (!isButtonAnimationPlaying && isButtonAnimationPlayingPrev) {
                UpdateButtonFSM(ButtonStateInput.FinishedAction);
            }
            
            isButtonAnimationPlayingPrev = isButtonAnimationPlaying;
        }

        private void UpdateButtonFSM(ButtonStateInput input) {
            switch (buttonFSMState) {
                case ButtonFSMState.IsUnpressed:
                    if (input == ButtonStateInput.ColliderEnter) {
                        buttonFSMState = ButtonFSMState.IsPressing;
                        PlayToButtonState(ButtonState.Pressed);
                    }
                    break;

                case ButtonFSMState.IsPressing:
                    if (input == ButtonStateInput.FinishedAction) {
                        buttonFSMState = ButtonFSMState.IsPressed;
                    }
                    break;

                case ButtonFSMState.IsPressed:
                    if (input == ButtonStateInput.ColliderEnter) {
                        buttonFSMState = ButtonFSMState.IsUnpressing;
                        PlayToButtonState(ButtonState.Unpressed);
                    }
                    break;

                case ButtonFSMState.IsUnpressing:
                    if (input == ButtonStateInput.FinishedAction) {
                        buttonFSMState = ButtonFSMState.IsUnpressed;
                    }
                    break;

                default:
                    break;
            }
        }

        public void OnColliderEntered(Collider thisObject, Collider otherObject) {
            if (DeviceManager.IsManipulator(otherObject.gameObject)) {

                if (thisObject.gameObject == buttonGameObject && ((buttonCover == null) ||
                    (buttonCover != null && buttonCover.CurrentCoverState == KVR_Cover.State.Open))) {
                    // button is pressed while cover is OPEN, and collider
                    // has entered from the top side of the button
                    Vector3 manipulatorDeltaPos = buttonGameObject.transform.InverseTransformPoint(
                        otherObject.transform.position);

                    if (manipulatorDeltaPos.y > 0f) {
                        UpdateButtonFSM(ButtonStateInput.ColliderEnter);
                    }
                }
            }
        }

        public void OnColliderExited(Collider thisObject, Collider otherObject) { }

        public void SetButtonState(ButtonState state) {
            CurrentButtonState = state;
			
            if (state == ButtonState.Pressed) {
                coloredGameObjectMaterial.SetColor(Shader.PropertyToID("_EmissiveColor"), Color.cyan);
            } else if (state == ButtonState.Unpressed) {
                coloredGameObjectMaterial.SetColor(Shader.PropertyToID("_EmissiveColor"), Color.black);
            }
        }

        private void GoToButtonState(ButtonState state) {
            SetButtonState(state);

            // switch to animation state instantly
            buttonAnimationState.normalizedTime = GetNormalizedTimeForButtonState(state);
            buttonAnimationState.speed = 0f;
            buttonAnimation.Play(buttonAnimationName);
        }

        private void PlayToButtonState(ButtonState state) {

            // set the animation time that we want to play to
            targetButtonAnimationEndTime = GetNormalizedTimeForButtonState(state);

            // note that the normalizedTime always resets to zero after finishing the clip.
            // so if button was at Pressed and it was already done playing, its normalizedTime is
            // 0f, even though the Pressed state corresponds to a time of 1f. so, for this special
            // case, force it to 1f.
            if (CurrentButtonState == ButtonState.Pressed &&
                buttonAnimationState.normalizedTime == 0f &&
                !buttonAnimation.isPlaying) {
                buttonAnimationState.normalizedTime = 1f;
            }

            // move either up or down depending on where the switch is right now
            buttonAnimationState.speed =
                Mathf.Sign(targetButtonAnimationEndTime - buttonAnimationState.normalizedTime) * 1f;

            // play animation and actuate switch
            buttonAnimation.Play(buttonAnimationName);
            SetButtonState(state);
        }

        private float GetNormalizedTimeForButtonState(ButtonState state) {
            float targetTime = 0f;
            switch (state) {
                case ButtonState.Pressed:
                    targetTime = 1f;
                    break;
                case ButtonState.Unpressed:
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
