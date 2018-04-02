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
        public State CurrentCoverState { get; private set; }
        public Animation CoverAnimation { get; private set; }
        public Transform ColliderTransform { get; private set; }
        #endregion

        #region Private Members
        private string coverAnimationName;
        private AnimationState coverAnimationState;
        private GameObject colliderGameObject;
        private FSMState fsmState;
        private float targetAnimationEndTime;
        private bool isAnimationPlayingPrev = false;
        #endregion

        #region Constructors
        public KVR_Cover(InternalProp prop, ConfigNode configuration) {
            // animation
            bool success = configuration.TryGetValue("animationName", ref coverAnimationName);
            if (success) {
                Animation[] animations = prop.FindModelAnimators(coverAnimationName);
                if (animations.Length > 0) {
                    CoverAnimation = animations[0];
                    coverAnimationState = CoverAnimation[coverAnimationName];
                    coverAnimationState.wrapMode = WrapMode.Once;
                } else {
                    throw new ArgumentException("InternalProp \"" + prop.name + "\" does not have animations (config node " +
                        configuration.id + ")");
                }
            } else {
                throw new ArgumentException("animationName not specified for KVR_Cover " +
                    prop.name + " (config node " + configuration.id + ")");
            }

            // collider game object
            string colliderTransformName = "";
            success = configuration.TryGetValue("colliderTransformName", ref colliderTransformName);
            if (!success) throw new ArgumentException("colliderTransformName not specified for KVR_Cover " +
                prop.name + " (config node " + configuration.id + ")");

            ColliderTransform = prop.FindModelTransform(colliderTransformName);
            if (ColliderTransform == null) throw new ArgumentException("Transform \"" + colliderTransformName +
                "\" not found for KVR_Cover " + prop.name + " (config node " + configuration.id + ")");

            colliderGameObject = ColliderTransform.gameObject;
            colliderGameObject.AddComponent<KVR_ActionableCollider>().module = this;

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
            if (DeviceManager.IsManipulator(otherObject.gameObject)) {

                if (thisObject.gameObject == colliderGameObject) {
                    // when cover is closed, can only be opened from the bottom edge of the
                    // collider on the top side. when cover is open, can only be opened from the top
                    // side of the collider.
                    Vector3 manipulatorDeltaPos = colliderGameObject.transform.InverseTransformPoint(
                        otherObject.transform.position);
                    if ((CurrentCoverState == State.Closed &&
                        manipulatorDeltaPos.z > 0f &&
                        manipulatorDeltaPos.y > 0f) ||
                        (CurrentCoverState == State.Open &&
                        manipulatorDeltaPos.y > 0f))

                        UpdateFSM(StateInput.ColliderEnter);
                }
            }
        }

        public void OnColliderExited(Collider thisObject, Collider otherObject) { }

        public void SetState(State state) {
            CurrentCoverState = state;
        }

        private void GoToState(State state) {
            SetState(state);

            // switch to animation state instantly
            coverAnimationState.normalizedTime = GetNormalizedTimeForState(state);
            coverAnimationState.speed = 0f;
            CoverAnimation.Play(coverAnimationName);
        }

        private void PlayToState(State state) {

            // set the animation time that we want to play to
            targetAnimationEndTime = GetNormalizedTimeForState(state);

            // note that the normalizedTime always resets to zero after finishing the clip.
            // so if cover was at Open and it was already done playing, its normalizedTime is
            // 0f, even though the Open state corresponds to a time of 1f. so, for this special
            // case, force it to 1f.
            if (CurrentCoverState == State.Open &&
                coverAnimationState.normalizedTime == 0f &&
                !CoverAnimation.isPlaying) {
                coverAnimationState.normalizedTime = 1f;
            }

            // move either up or down depending on where the switch is right now
            coverAnimationState.speed =
                Mathf.Sign(targetAnimationEndTime - coverAnimationState.normalizedTime) * 1f;

            // play animation and actuate switch
            CoverAnimation.Play(coverAnimationName);
            SetState(state);
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
