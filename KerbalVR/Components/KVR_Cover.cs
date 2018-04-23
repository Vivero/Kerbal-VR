using System;
using UnityEngine;

namespace KerbalVR.Components
{
    public class KVR_Cover : IActionableCollider
    {
        #region Types
        public enum State {
            Closed,
            Open,
        }

        public enum StateInput {
            ColliderEnter,
            FinishedAction,
        }

        public enum FSMState {
            IsClosed,
            IsOpening,
            IsOpen,
            IsClosing,
        }
        #endregion

        #region Properties
        public State CurrentState { get; private set; }
        public Animation CoverAnimation { get; private set; }
        public Transform ColliderTransform { get; private set; }
        public AudioSource SoundEffect { get; protected set; }
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
        public KVR_Cover(InternalProp prop, ConfigNode configuration) {
            // animation
            CoverAnimation = ConfigUtils.GetAnimation(prop, configuration, "animationName", out animationName);
            animationState = CoverAnimation[animationName];
            animationState.wrapMode = WrapMode.Once;

            // collider game object
            ColliderTransform = ConfigUtils.GetTransform(prop, configuration, "colliderTransformName");
            colliderGameObject = ColliderTransform.gameObject;
            colliderGameObject.AddComponent<KVR_ActionableCollider>().module = this;

            // sound effect
            try {
                SoundEffect = ConfigUtils.SetupAudioClip(prop, configuration, "sound");
            } catch (Exception e) {
                Utils.LogWarning(e.ToString());
            }

            // set initial state
            isAnimationPlayingPrev = false;
            fsmState = FSMState.IsClosed;
            targetAnimationEndTime = 0f;
            GoToState(State.Closed);
        }
        #endregion

        public void Update() {
            bool isAnimationPlaying = CoverAnimation.isPlaying;

            // check if animation finished playing
            if (!isAnimationPlaying && isAnimationPlayingPrev) {
                UpdateFSM(StateInput.FinishedAction);
            }

            // keep track of whether animation was playing
            isAnimationPlayingPrev = isAnimationPlaying;
        }

        private void UpdateFSM(StateInput input) {
            switch (fsmState) {
                case FSMState.IsClosed:
                    if (input == StateInput.ColliderEnter) {
                        fsmState = FSMState.IsOpening;
                        PlayToState(State.Open);
                    }
                    break;

                case FSMState.IsOpening:
                    if (input == StateInput.FinishedAction) {
                        fsmState = FSMState.IsOpen;
                    }
                    break;

                case FSMState.IsOpen:
                    if (input == StateInput.ColliderEnter) {
                        fsmState = FSMState.IsClosing;
                        PlayToState(State.Closed);
                    }
                    break;

                case FSMState.IsClosing:
                    if (input == StateInput.FinishedAction) {
                        fsmState = FSMState.IsClosed;
                    }
                    break;

                default:
                    break;
            }
        }

        public void OnColliderEntered(Collider thisObject, Collider otherObject) {
            if (DeviceManager.IsManipulatorFingertip(otherObject)) {

                if (thisObject.gameObject == colliderGameObject) {
                    // when cover is closed, can only be opened from the bottom edge of the
                    // collider on the top side. when cover is open, can only be opened from the top
                    // side of the collider.
                    Vector3 manipulatorDeltaPos = colliderGameObject.transform.InverseTransformPoint(
                        otherObject.transform.position);
                    if ((CurrentState == State.Closed &&
                        manipulatorDeltaPos.z > 0f &&
                        manipulatorDeltaPos.y > 0f) ||
                        (CurrentState == State.Open &&
                        manipulatorDeltaPos.y > 0f))

                        UpdateFSM(StateInput.ColliderEnter);
                }
            }
        }

        public void OnColliderExited(Collider thisObject, Collider otherObject) { }

        public void SetState(State state) {
            CurrentState = state;
        }

        private void GoToState(State state) {
            SetState(state);

            // switch to animation state instantly
            animationState.normalizedTime = GetNormalizedTimeForState(state);
            animationState.speed = 0f;
            CoverAnimation.Play(animationName);
        }

        private void PlayToState(State state) {

            // set the animation time that we want to play to
            targetAnimationEndTime = GetNormalizedTimeForState(state);

            // note that the normalizedTime always resets to zero after finishing the clip.
            // so if cover was at Open and it was already done playing, its normalizedTime is
            // 0f, even though the Open state corresponds to a time of 1f. so, for this special
            // case, force it to 1f.
            if (CurrentState == State.Open &&
                animationState.normalizedTime == 0f &&
                !CoverAnimation.isPlaying) {
                animationState.normalizedTime = 1f;
            }

            // move either up or down depending on where the switch is right now
            animationState.speed =
                Mathf.Sign(targetAnimationEndTime - animationState.normalizedTime) * 1f;

            // play animation and actuate switch
            CoverAnimation.Play(animationName);
            SetState(state);

            // play sound effect if available
            if (SoundEffect != null) {
                SoundEffect.Play();
            }
        }

        private float GetNormalizedTimeForState(State state) {
            float targetTime = 0f;
            switch (state) {
                case State.Closed:
                    targetTime = 0f;
                    break;
                case State.Open:
                    targetTime = 1f;
                    break;
            }
            return targetTime;
        }
    }
}
