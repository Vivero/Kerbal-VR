using System;
using UnityEngine;

namespace KerbalVR.Components
{
    public abstract class KVR_Switch : IActionableCollider
    {
        #region Types
        public enum State {
            Down = 0,
            Middle = 1,
            Up = 2,
        }
        #endregion

        #region Properties
        public State CurrentState { get; protected set; }
        public Animation SwitchAnimation { get; protected set; }
        public string OutputSignal { get; protected set; }
        #endregion

        #region Members
        public bool enabled;
        #endregion

        #region Private Members
        protected string animationName;
        protected AnimationState animationState;
        protected float targetAnimationEndTime;
        protected bool isAnimationPlayingPrev = false;
        #endregion

        public abstract void Update();
        public abstract void OnColliderEntered(Collider thisObject, Collider otherObject);
        public abstract void OnColliderExited(Collider thisObject, Collider otherObject);

        public void SetState(State state) {
            CurrentState = state;
        }

        public void ExecuteSignal() {
            if (!string.IsNullOrEmpty(OutputSignal)) {
                Events.AvionicsInt(OutputSignal).Send((int)CurrentState);
            }
        }

        protected void GoToState(State state) {
            SetState(state);

            // switch to animation state instantly
            animationState.normalizedTime = GetNormalizedTimeForState(state);
            animationState.speed = 0f;
            SwitchAnimation.Play(animationName);
        }

        protected void PlayToState(State state) {

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
                Mathf.Sign(targetAnimationEndTime - animationState.normalizedTime) * 2f;

            // play animation and actuate switch
            SwitchAnimation.Play(animationName);
            SetState(state);
        }

        protected float GetNormalizedTimeForState(State state) {
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
