using System;
using UnityEngine;

namespace KerbalVR.Components
{
    public class KVR_Button : IActionableCollider
    {
        #region Types
        public enum ActuationType {
            Momentary,
            Latching
        }

        public enum State {
            Unpressed = 0,
            Pressed = 1,
        }

        public enum StateInput {
            ColliderEnter,
            ColliderExit,
            FinishedAction,
        }

        public enum FSMState {
            IsUnpressed,
            IsPressing,
            IsPressed,
            IsUnpressing,
        }
        #endregion

        #region Properties
        public ActuationType Type { get; private set; }
        public State CurrentState { get; private set; }
        public Animation ButtonAnimation { get; private set; }
        public Transform ColliderTransform { get; private set; }
        public AudioSource SoundEffect { get; protected set; }
        public string OutputSignal { get; protected set; }
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
        public KVR_Button(InternalProp prop, ConfigNode configuration) {
            // button type
            ActuationType type = ActuationType.Latching;
            bool success = configuration.TryGetEnum("type", ref type, ActuationType.Latching);
            Type = type;

            // animation
            ButtonAnimation = ConfigUtils.GetAnimation(prop, configuration, "animationName", out animationName);
            animationState = ButtonAnimation[animationName];
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

            // output signal
            string outputSignalName = "";
            success = configuration.TryGetValue("outputSignal", ref outputSignalName);
            if (success) OutputSignal = outputSignalName;

            // set initial state
            enabled = false;
            isAnimationPlayingPrev = false;
            fsmState = FSMState.IsUnpressed;
            targetAnimationEndTime = 0f;
            GoToState(State.Unpressed);
        }
        #endregion

        public void Update() {
            bool isAnimationPlaying = ButtonAnimation.isPlaying;

            // check if animation finished playing
            if (!isAnimationPlaying && isAnimationPlayingPrev) {
                UpdateFSM(StateInput.FinishedAction);
            }

            // keep track of whether animation was playing
            isAnimationPlayingPrev = isAnimationPlaying;
        }

        private void UpdateFSM(StateInput input) {
            // Utils.Log("KVR_Button UpdateFSM, fsm = " + fsmState + ", state = " + CurrentState + ", input = " + input);
            switch (fsmState) {
                case FSMState.IsUnpressed:
                    if (input == StateInput.ColliderEnter) {
                        fsmState = FSMState.IsPressing;
                        PlayToState(State.Pressed);
                    }
                    break;

                case FSMState.IsPressing:
                    if (input == StateInput.FinishedAction) {
                        fsmState = FSMState.IsPressed;
                        ExecuteSignal();
                    }
                    break;

                case FSMState.IsPressed:
                    if ((Type == ActuationType.Latching && input == StateInput.ColliderEnter) || 
                        (Type == ActuationType.Momentary && input == StateInput.ColliderExit)) {
                        fsmState = FSMState.IsUnpressing;
                        PlayToState(State.Unpressed);
                    }
                    break;

                case FSMState.IsUnpressing:
                    if (input == StateInput.FinishedAction) {
                        fsmState = FSMState.IsUnpressed;
                        ExecuteSignal();
                    }
                    break;

                default:
                    break;
            }
        }

        public void OnColliderEntered(Collider thisObject, Collider otherObject) {
            if (DeviceManager.IsManipulatorFingertip(otherObject)) {

                if (enabled && thisObject.gameObject == colliderGameObject) {
                    // actuate only when collider enters from the top
                    Vector3 manipulatorDeltaPos = colliderGameObject.transform.InverseTransformPoint(
                        otherObject.transform.position);

                    if (manipulatorDeltaPos.y > 0f) {
                        UpdateFSM(StateInput.ColliderEnter);
                    }
                }
            }
        }

        public void OnColliderExited(Collider thisObject, Collider otherObject) {
            if (DeviceManager.IsManipulatorFingertip(otherObject)) {
                if (Type == ActuationType.Momentary) {
                    UpdateFSM(StateInput.ColliderExit);
                }
            }
        }

        public void SetState(State state) {
            CurrentState = state;
        }

        public void ExecuteSignal() {
            if (!string.IsNullOrEmpty(OutputSignal)) {
                Events.AvionicsInt(OutputSignal).Send((int)CurrentState);
            }
        }

        private void GoToState(State state) {
            SetState(state);

            // switch to animation state instantly
            animationState.normalizedTime = GetNormalizedTimeForState(state);
            animationState.speed = 0f;
            ButtonAnimation.Play(animationName);
        }

        private void PlayToState(State state) {

            // set the animation time that we want to play to
            targetAnimationEndTime = GetNormalizedTimeForState(state);

            // note that the normalizedTime always resets to zero after finishing the clip.
            // so if button was at Pressed and it was already done playing, its normalizedTime is
            // 0f, even though the Pressed state corresponds to a time of 1f. so, for this special
            // case, force it to 1f.
            if (CurrentState == State.Pressed &&
                animationState.normalizedTime == 0f &&
                !ButtonAnimation.isPlaying) {
                animationState.normalizedTime = 1f;
            }

            // move either up or down depending on where the switch is right now
            animationState.speed =
                Mathf.Sign(targetAnimationEndTime - animationState.normalizedTime) * 1f;

            // play animation and actuate switch
            ButtonAnimation.Play(animationName);
            SetState(state);

            // play sound effect if available
            if (SoundEffect != null) {
                SoundEffect.Play();
            }
        }

        private float GetNormalizedTimeForState(State state) {
            float targetTime = 0f;
            switch (state) {
                case State.Unpressed:
                    targetTime = 0f;
                    break;
                case State.Pressed:
                    targetTime = 1f;
                    break;
            }
            return targetTime;
        }
    }
}
