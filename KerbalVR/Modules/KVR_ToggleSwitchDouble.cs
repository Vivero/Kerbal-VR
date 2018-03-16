using System.Collections;
using UnityEngine;

namespace KerbalVR.Modules
{
    public class KVR_ToggleSwitchDouble : InternalModule {
        #region Types
        public enum SwitchState {
            Up,
            Mid,
            Down,
        }

        public enum SwitchStateInput {
            ColliderUpEnter,
            ColliderUpExit,
            ColliderMidEnter,
            ColliderMidExit,
            ColliderDownEnter,
            ColliderDownExit,
        }

        public enum SwitchFSMState {
            Up,
            WaitingForDown,
            Down,
            WaitingForUp,
        }
        #endregion

        #region KSP Config Fields
        [KSPField]
        public string animationName = string.Empty;
        [KSPField]
        public string transformSwitchUp = string.Empty;
        [KSPField]
        public string transformSwitchDown = string.Empty;
        #endregion

        #region Properties
        public SwitchState switchState { get; private set; }
        #endregion

        #region Private Members
        private Animation switchAnimation;
        private AnimationState switchAnimationState;
        private GameObject switchUpGameObject;
        private GameObject switchDownGameObject;
        private SwitchFSMState switchFSMState;
        private float targetAnimationEndTime;
        #endregion

        /// <summary>
        /// Loads the animations and hooks into the colliders for this toggle switch.
        /// </summary>
        void Start() {
            Utils.Log("KVR_ToggleSwitchDouble Start " + gameObject.name);
            // Utils.PrintGameObjectTree(gameObject);

            // no setup needed in editor mode
            if (HighLogic.LoadedScene == GameScenes.EDITOR) return;

            // retrieve the animation
            Animation[] animations = internalProp.FindModelAnimators(animationName);
            if (animations.Length > 0) {
                switchAnimation = animations[0];
                switchAnimationState = switchAnimation[animationName];
                switchAnimationState.wrapMode = WrapMode.Once;
            } else {
                Utils.LogWarning("KVR_ToggleSwitchDouble (" + gameObject.name + ") has no animation \"" + animationName + "\"");
            }

            // retrieve the collider GameObjects
            Transform switchTransform = internalProp.FindModelTransform(transformSwitchUp);
            if (switchTransform != null) {
                switchUpGameObject = switchTransform.gameObject;
                switchUpGameObject.AddComponent<KVR_ToggleSwitchCollider>().toggleSwitchComponent = this;
            } else {
                Utils.LogWarning("KVR_ToggleSwitchDouble (" + gameObject.name + ") has no switch collider \"" + transformSwitchUp + "\"");
            }

            switchTransform = internalProp.FindModelTransform(transformSwitchDown);
            if (switchTransform != null) {
                switchDownGameObject = switchTransform.gameObject;
                switchDownGameObject.AddComponent<KVR_ToggleSwitchCollider>().toggleSwitchComponent = this;
            } else {
                Utils.LogWarning("KVR_ToggleSwitchDouble (" + gameObject.name + ") has no switch collider \"" + transformSwitchDown + "\"");
            }

            // set initial state
            targetAnimationEndTime = 0f;
            switchFSMState = SwitchFSMState.Down;
            SetState(SwitchState.Down);
        }

        void Update() {
            if (switchAnimation.isPlaying &&
                ((switchAnimationState.speed > 0f && switchAnimationState.normalizedTime >= targetAnimationEndTime) ||
                (switchAnimationState.speed < 0f && switchAnimationState.normalizedTime <= targetAnimationEndTime))) {

                switchAnimation.Stop();
            }
        }

        private void UpdateSwitchFSM(SwitchStateInput colliderInput) {
            switch (switchFSMState) {
                case SwitchFSMState.Up:
                    if (colliderInput == SwitchStateInput.ColliderUpEnter) {
                        switchFSMState = SwitchFSMState.WaitingForDown;
                    }
                    break;

                case SwitchFSMState.WaitingForDown:
                    if (colliderInput == SwitchStateInput.ColliderUpExit) {
                        switchFSMState = SwitchFSMState.Up;
                    } else if (colliderInput == SwitchStateInput.ColliderDownEnter) {
                        switchFSMState = SwitchFSMState.Down;
                        PlayToState(SwitchState.Down);
                    }
                    break;

                case SwitchFSMState.Down:
                    if (colliderInput == SwitchStateInput.ColliderDownEnter) {
                        switchFSMState = SwitchFSMState.WaitingForUp;
                    }
                    break;

                case SwitchFSMState.WaitingForUp:
                    if (colliderInput == SwitchStateInput.ColliderDownExit) {
                        switchFSMState = SwitchFSMState.Down;
                    } else if (colliderInput == SwitchStateInput.ColliderUpEnter) {
                        switchFSMState = SwitchFSMState.Up;
                        PlayToState(SwitchState.Up);
                    }
                    break;

                default:
                    break;
            }
        }

        public void SwitchColliderEntered(GameObject colliderObject) {
            if (colliderObject == switchUpGameObject) {
                UpdateSwitchFSM(SwitchStateInput.ColliderUpEnter);
            } else if (colliderObject == switchDownGameObject) {
                UpdateSwitchFSM(SwitchStateInput.ColliderDownEnter);
            }
        }

        public void SwitchColliderExited(GameObject colliderObject) {
            if (colliderObject == switchUpGameObject) {
                UpdateSwitchFSM(SwitchStateInput.ColliderUpExit);
            } else if (colliderObject == switchDownGameObject) {
                UpdateSwitchFSM(SwitchStateInput.ColliderDownExit);
            }
        }

        private void SetState(SwitchState state) {
            switchState = state;
            switchAnimationState.normalizedTime = GetNormalizedTimeForState(state);
            switchAnimationState.speed = 0f;
            switchAnimation.Play(animationName);
        }

        private void PlayToState(SwitchState state) {

            // set the animation time that we want to play to
            targetAnimationEndTime = GetNormalizedTimeForState(state);

            // note that the normalizedTime always resets to zero after finishing the clip.
            // so if switch was at Up and it was already done playing, its normalizedTime is
            // 0f, even though the Up state corresponds to a time of 1f. so, for this special
            // case, force set it to 1f.
            if (switchState == SwitchState.Up &&
                switchAnimationState.normalizedTime == 0f &&
                !switchAnimation.isPlaying) {
                switchAnimationState.normalizedTime = 1f;
            }

            // move either up or down depending on where the switch is right now
            switchAnimationState.speed =
                Mathf.Sign(targetAnimationEndTime - switchAnimationState.normalizedTime) * 1f;

            /*Utils.Log("Play to state " + state + ", current state = " + switchState +
                ", start = " + switchAnimationState.normalizedTime.ToString("F2") +
                ", end = " + targetAnimationEndTime.ToString("F1") +
                ", speed = " + switchAnimationState.speed.ToString("F1"));*/
            switchAnimation.Play(animationName);

            switchState = state;
        }

        private float GetNormalizedTimeForState(SwitchState state) {
            float targetTime = 0f;
            switch (state) {
                case SwitchState.Up:
                    targetTime = 1f;
                    break;
                case SwitchState.Mid:
                    targetTime = 0.5f;
                    break;
                case SwitchState.Down:
                    targetTime = 0f;
                    break;
            }
            return targetTime;
        }
    }
}
