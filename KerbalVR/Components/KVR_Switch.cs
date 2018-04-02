using System;
using UnityEngine;

namespace KerbalVR.Components
{
    public class KVR_Switch : IActionableCollider
    {
        #region Types
        public enum ActuationType {
            Momentary,
            LatchingTwoState,
            LatchingThreeState,
        }

        public enum State {
            Down,
            Middle,
            Up,
        }

        public enum StateInput {
            ColliderEnter,
            ColliderExit,
            FinishedAction,
        }

        public enum FSMState {
            IsDown,
            IsSwitchingUp,
            IsUp,
            IsSwitchingDown,
        }
        #endregion

        #region Properties
        public ActuationType Type { get; private set; }
        public State CurrentState { get; private set; }
        public Animation SwitchAnimation { get; private set; }
        public Transform ColliderTransform { get; private set; }
        #endregion

        #region Members
        public bool enabled;
        #endregion

        #region Private Members
        private string animationName;
        private AnimationState animationState;
        private GameObject colliderGameObject;
        private FSMState fsmState;
        private float targetAnimationEndTime;
        private bool isAnimationPlayingPrev = false;
        #endregion

        #region Constructors
        public KVR_Switch(InternalProp prop, ConfigNode configuration) {
            // button type
            ActuationType type = ActuationType.Momentary;
            bool success = configuration.TryGetEnum("type", ref type, ActuationType.Momentary);
            Type = type;

            // animation
            SwitchAnimation = ConfigUtils.GetAnimation(prop, configuration, "animationName", out animationName);
            animationState = SwitchAnimation[animationName];
            animationState.wrapMode = WrapMode.Once;

            // collider game objects
            ColliderTransform = ConfigUtils.GetTransform(prop, configuration, "colliderDownTransformName");
            colliderGameObject = ColliderTransform.gameObject;
            colliderGameObject.AddComponent<KVR_ActionableCollider>().module = this;

            // set initial state
            enabled = false;
            isAnimationPlayingPrev = false;
            fsmState = FSMState.IsDown;
            targetAnimationEndTime = 0f;
            GoToState(State.Down);
        }
        #endregion

        public void Update() {
            bool isAnimationPlaying = SwitchAnimation.isPlaying;

            // check if animation finished playing
            if (!isAnimationPlaying && isAnimationPlayingPrev) {
                UpdateFSM(StateInput.FinishedAction);
            }

            // keep track of whether animation was playing
            isAnimationPlayingPrev = isAnimationPlaying;
        }

        private void UpdateFSM(StateInput input) {
            // Utils.Log("KVR_Switch UpdateFSM, fsm = " + fsmState + ", state = " + CurrentState + ", input = " + input);
            switch (fsmState) {
                case FSMState.IsDown:
                    if (input == StateInput.ColliderEnter) {
                        fsmState = FSMState.IsSwitchingUp;
                        PlayToState(State.Up);
                    }
                    break;

                case FSMState.IsSwitchingUp:
                    if (input == StateInput.FinishedAction) {
                        fsmState = FSMState.IsUp;
                    }
                    break;

                case FSMState.IsUp:
                    if (input == StateInput.ColliderExit) {
                        fsmState = FSMState.IsSwitchingDown;
                        PlayToState(State.Down);
                    }
                    break;

                case FSMState.IsSwitchingDown:
                    if (input == StateInput.FinishedAction) {
                        fsmState = FSMState.IsDown;
                    }
                    break;

                default:
                    break;
            }
        }

        public void OnColliderEntered(Collider thisObject, Collider otherObject) {
            if (DeviceManager.IsManipulator(otherObject.gameObject)) {

                if (enabled && thisObject.gameObject == colliderGameObject) {
                    // actuate only when collider enters from the bottom
                    Vector3 manipulatorDeltaPos = colliderGameObject.transform.InverseTransformPoint(
                        otherObject.transform.position);

                    if (manipulatorDeltaPos.z > 0f) {
                        UpdateFSM(StateInput.ColliderEnter);
                    }
                }
            }
        }

        public void OnColliderExited(Collider thisObject, Collider otherObject) {
            if (DeviceManager.IsManipulator(otherObject.gameObject)) {
                if (Type == ActuationType.Momentary) {
                    UpdateFSM(StateInput.ColliderExit);
                }
            }
        }

        public void SetState(State state) {
            CurrentState = state;
        }

        private void GoToState(State state) {
            SetState(state);

            // switch to animation state instantly
            animationState.normalizedTime = GetNormalizedTimeForState(state);
            animationState.speed = 0f;
            SwitchAnimation.Play(animationName);
        }

        private void PlayToState(State state) {

            // set the animation time that we want to play to
            targetAnimationEndTime = GetNormalizedTimeForState(state);

            // note that the normalizedTime always resets to zero after finishing the clip.
            // so if switch was at Up and it was already done playing, its normalizedTime is
            // 0f, even though the Up state corresponds to a time of 1f. so, for this special
            // case, force it to 1f.
            if (CurrentState == State.Up &&
                animationState.normalizedTime == 0f &&
                !SwitchAnimation.isPlaying) {
                animationState.normalizedTime = 1f;
            }

            // move either up or down depending on where the switch is right now
            animationState.speed =
                Mathf.Sign(targetAnimationEndTime - animationState.normalizedTime) * 1f;

            // play animation and actuate switch
            SwitchAnimation.Play(animationName);
            SetState(state);
        }

        private float GetNormalizedTimeForState(State state) {
            float targetTime = 0f;
            switch (state) {
                case State.Down:
                    targetTime = 0f;
                    break;
                case State.Middle:
                    targetTime = 0.5f;
                    break;
                case State.Up:
                    targetTime = 1f;
                    break;
            }
            return targetTime;
        }
    }
}
