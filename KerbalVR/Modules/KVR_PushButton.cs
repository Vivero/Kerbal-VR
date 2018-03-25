using UnityEngine;
using KerbalVR.Components;

namespace KerbalVR.Modules
{
    public class KVR_PushButton : InternalModule, IActionableCollider {

        #region Types
        public enum CoverState {
            Closed,
            Open,
        }

        public enum CoverStateInput
        {
            ColliderEnter,
            FinishedAction,
        }

        public enum CoverFSMState
        {
            IsClosed,
            IsOpening,
            IsOpen,
            IsClosing,
        }

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
        public string coverAnimationName = string.Empty;
        [KSPField]
        public string transformCoverCollider = string.Empty;
        [KSPField]
        public string buttonAnimationName = string.Empty;
        [KSPField]
        public string transformButtonCollider = string.Empty;
        [KSPField]
        public string labelMainText = string.Empty;
        [KSPField]
        public Vector3 labelMainOffset = Vector3.zero;
        [KSPField]
        public string labelUpText = string.Empty;
        [KSPField]
        public Vector3 labelUpOffset = Vector3.zero;
        [KSPField]
        public string labelDownText = string.Empty;
        [KSPField]
        public Vector3 labelDownOffset = Vector3.zero;
        [KSPField]
        public string coloredObject = string.Empty;
        #endregion

        #region Properties
        public CoverState CurrentCoverState { get; private set; }
        public ButtonState CurrentButtonState { get; private set; }
        #endregion

        #region Private Members
        private Animation coverAnimation;
        private AnimationState coverAnimationState;
        private GameObject coverGameObject;
        private CoverFSMState coverFSMState;
        private float targetCoverAnimationEndTime;
        private bool isCoverAnimationPlayingPrev;

        private Animation buttonAnimation;
        private AnimationState buttonAnimationState;
        private GameObject buttonGameObject;
        private ButtonFSMState buttonFSMState;
        private float targetButtonAnimationEndTime;
        private bool isButtonAnimationPlayingPrev;

        private GameObject coloredGameObject;
        #endregion


        void Start() {
            // no setup needed in editor mode
            if (HighLogic.LoadedScene == GameScenes.EDITOR) return;

            // retrieve the animations
            Animation[] animations = internalProp.FindModelAnimators(coverAnimationName);
            if (animations.Length > 0) {
                coverAnimation = animations[0];
                coverAnimationState = coverAnimation[coverAnimationName];
                coverAnimationState.wrapMode = WrapMode.Once;
            } else {
                Utils.LogWarning("KVR_PushButtonCover (" + gameObject.name + ") has no animation \"" + coverAnimationName + "\"");
            }

            animations = internalProp.FindModelAnimators(buttonAnimationName);
            if (animations.Length > 0) {
                buttonAnimation = animations[0];
                buttonAnimationState = buttonAnimation[buttonAnimationName];
                buttonAnimationState.wrapMode = WrapMode.Once;
            } else {
                Utils.LogWarning("KVR_PushButtonCover (" + gameObject.name + ") has no animation \"" + buttonAnimationName + "\"");
            }

            // retrieve the collider GameObjects
            Transform colliderTransform = internalProp.FindModelTransform(transformCoverCollider);
            if (colliderTransform != null) {
                coverGameObject = colliderTransform.gameObject;
                coverGameObject.AddComponent<KVR_ActionableCollider>().module = this;
            } else {
                Utils.LogWarning("KVR_PushButtonCover (" + gameObject.name + ") has no cover collider \"" + transformCoverCollider + "\"");
            }

            colliderTransform = internalProp.FindModelTransform(transformButtonCollider);
            if (colliderTransform != null) {
                buttonGameObject = colliderTransform.gameObject;
                buttonGameObject.AddComponent<KVR_ActionableCollider>().module = this;
            } else {
                Utils.LogWarning("KVR_PushButtonCover (" + gameObject.name + ") has no button collider \"" + transformButtonCollider + "\"");
            }

            // special effects
            Transform coloredObjectTransform = internalProp.FindModelTransform(coloredObject);
            if (coloredObjectTransform != null) {
                coloredGameObject = coloredObjectTransform.gameObject;
                MeshRenderer r = coloredGameObject.GetComponent<MeshRenderer>();
                Material rmat = r.sharedMaterial;
                rmat.SetColor(Shader.PropertyToID("_EmissiveColor"), Color.red);
            }

            // set initial state
            isCoverAnimationPlayingPrev = false;
            coverFSMState = CoverFSMState.IsClosed;
            targetCoverAnimationEndTime = 0f;
            GoToCoverState(CoverState.Closed);

            isButtonAnimationPlayingPrev = false;
            buttonFSMState = ButtonFSMState.IsUnpressed;
            targetButtonAnimationEndTime = 0f;
            GoToButtonState(ButtonState.Unpressed);

            // create labels
            //CreateLabels(); // TODO
        }

        public override void OnUpdate() {
            bool isCoverAnimationPlaying = coverAnimation.isPlaying;
            bool isButtonAnimationPlaying = buttonAnimation.isPlaying;

            if (!isCoverAnimationPlaying && isCoverAnimationPlayingPrev) {
                UpdateCoverFSM(CoverStateInput.FinishedAction);
            }
            if (!isButtonAnimationPlaying && isButtonAnimationPlayingPrev) {
                UpdateButtonFSM(ButtonStateInput.FinishedAction);
            }

            isCoverAnimationPlayingPrev = isCoverAnimationPlaying;
            isButtonAnimationPlayingPrev = isButtonAnimationPlaying;
        }

        private void UpdateCoverFSM(CoverStateInput input) {
            switch (coverFSMState) {
                case CoverFSMState.IsClosed:
                    if (input == CoverStateInput.ColliderEnter) {
                        coverFSMState = CoverFSMState.IsOpening;
                        PlayToCoverState(CoverState.Open);
                    }
                    break;

                case CoverFSMState.IsOpening:
                    if (input == CoverStateInput.FinishedAction) {
                        coverFSMState = CoverFSMState.IsOpen;
                    }
                    break;

                case CoverFSMState.IsOpen:
                    if (input == CoverStateInput.ColliderEnter) {
                        coverFSMState = CoverFSMState.IsClosing;
                        PlayToCoverState(CoverState.Closed);
                    }
                    break;

                case CoverFSMState.IsClosing:
                    if (input == CoverStateInput.FinishedAction) {
                        coverFSMState = CoverFSMState.IsClosed;
                    }
                    break;

                default:
                    break;
            }
        }

        private void UpdateButtonFSM(ButtonStateInput input) {
            Utils.Log("UpdateButtonFSM state: " + buttonFSMState + ", input: " + input);

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
            // Utils.Log("OnColliderEntered object: " + thisObject.name + ", other: " + otherObject.name);

            if ((DeviceManager.Instance.ManipulatorLeft != null &&
                otherObject.gameObject == DeviceManager.Instance.ManipulatorLeft.gameObject) ||
                (DeviceManager.Instance.ManipulatorRight != null &&
                otherObject.gameObject == DeviceManager.Instance.ManipulatorRight.gameObject)) {

                if (thisObject.gameObject == coverGameObject) {
                    UpdateCoverFSM(CoverStateInput.ColliderEnter);

                } else if (CurrentCoverState == CoverState.Open &&
                    thisObject.gameObject == buttonGameObject) {
                    // button is pressed while cover is OPEN
                    UpdateButtonFSM(ButtonStateInput.ColliderEnter);
                }
            }
        }

        public void OnColliderStayed(Collider thisObject, Collider otherObject) { }

        public void OnColliderExited(Collider thisObject, Collider otherObject) { }

        public void SetCoverState(CoverState state) {
            CurrentCoverState = state;
        }

        private void GoToCoverState(CoverState state) {
            SetCoverState(state);

            // switch to animation state instantly
            coverAnimationState.normalizedTime = GetNormalizedTimeForCoverState(state);
            coverAnimationState.speed = 0f;
            coverAnimation.Play(coverAnimationName);
        }

        private void PlayToCoverState(CoverState state) {

            // set the animation time that we want to play to
            targetCoverAnimationEndTime = GetNormalizedTimeForCoverState(state);

            // note that the normalizedTime always resets to zero after finishing the clip.
            // so if cover was at Open and it was already done playing, its normalizedTime is
            // 0f, even though the Open state corresponds to a time of 1f. so, for this special
            // case, force it to 1f.
            if (CurrentCoverState == CoverState.Open &&
                coverAnimationState.normalizedTime == 0f &&
                !coverAnimation.isPlaying) {
                coverAnimationState.normalizedTime = 1f;
            }

            // move either up or down depending on where the switch is right now
            coverAnimationState.speed =
                Mathf.Sign(targetCoverAnimationEndTime - coverAnimationState.normalizedTime) * 1f;

            // play animation and actuate switch
            coverAnimation.Play(coverAnimationName);
            SetCoverState(state);
        }

        public void SetButtonState(ButtonState state) {
            CurrentButtonState = state;
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

        private float GetNormalizedTimeForCoverState(CoverState state) {
            float targetTime = 0f;
            switch (state) {
                case CoverState.Open:
                    targetTime = 1f;
                    break;
                case CoverState.Closed:
                    targetTime = 0f;
                    break;
            }
            return targetTime;
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
            GameObject labelMainGameObject = CreateLabel(
                "labelMain",
                labelMainText,
                0.2f, new Vector3(0f, 0f, -0.05f) + labelMainOffset,
                TMPro.FontStyles.Bold);

            GameObject labelUpGameObject = CreateLabel(
                "labelUp",
                labelUpText,
                0.1f, new Vector3(0f, 0f, -0.035f) + labelUpOffset);

            GameObject labelDownGameObject = CreateLabel(
                "labelDown",
                labelDownText,
                0.1f, new Vector3(0f, 0f, 0.035f) + labelDownOffset);
        }

        private GameObject CreateLabel(
            string name,
            string text,
            float fontSize,
            Vector3 offset,
            TMPro.FontStyles fontStyle = TMPro.FontStyles.Normal) {

            GameObject labelGameObject = new GameObject(internalProp.name + " " + name);
            labelGameObject.layer = 20;
            labelGameObject.transform.SetParent(internalProp.transform);

            TMPro.TextMeshPro tmpLabel = labelGameObject.AddComponent<TMPro.TextMeshPro>();
            tmpLabel.SetText(text);
            tmpLabel.fontSize = fontSize;
            tmpLabel.alignment = TMPro.TextAlignmentOptions.Center;
            tmpLabel.fontStyle = fontStyle;

            tmpLabel.rectTransform.localPosition = offset;
            tmpLabel.rectTransform.localRotation = Quaternion.Euler(90f, 0f, 180f);
            tmpLabel.rectTransform.sizeDelta = new Vector2(0.2f, 0.2f);
            
            return labelGameObject;
        }
    }
}
