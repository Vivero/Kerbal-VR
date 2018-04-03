using System;
using UnityEngine;

namespace KerbalVR.Components
{
    public class KVR_SwitchMomentary : KVR_Switch
    {
        #region Types
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
        public Transform ColliderTransform { get; private set; }
        #endregion

        #region Private Members
        private GameObject colliderGameObject, colliderUpGameObject, colliderMiddleGameObject;
        private FSMState fsmState;
        #endregion

        #region Constructors
        public KVR_SwitchMomentary(InternalProp prop, ConfigNode configuration) {
            // animation
            SwitchAnimation = ConfigUtils.GetAnimation(prop, configuration, "animationName", out animationName);
            animationState = SwitchAnimation[animationName];
            animationState.wrapMode = WrapMode.Once;

            // collider game objects
            ColliderTransform = ConfigUtils.GetTransform(prop, configuration, "colliderTransformName");
            colliderGameObject = ColliderTransform.gameObject;
            colliderGameObject.AddComponent<KVR_ActionableCollider>().module = this;

            // output signal
            string outputSignalName = "";
            bool success = configuration.TryGetValue("outputSignal", ref outputSignalName);
            if (success) OutputSignal = outputSignalName;

            // set initial state
            enabled = false;
            isAnimationPlayingPrev = false;
            fsmState = FSMState.IsDown;
            targetAnimationEndTime = 0f;
            GoToState(State.Down);
        }
        #endregion

        public override void Update() {
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
                    if (input == StateInput.ColliderExit) {
                        fsmState = FSMState.IsSwitchingDown;
                        PlayToState(State.Down);
                    } else if (input == StateInput.FinishedAction) {
                        ExecuteSignal();
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
                        ExecuteSignal();
                        fsmState = FSMState.IsDown;
                    }
                    break;

                default:
                    break;
            }
        }

        public override void OnColliderEntered(Collider thisObject, Collider otherObject) {
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

        public override void OnColliderExited(Collider thisObject, Collider otherObject) {
            if (DeviceManager.IsManipulator(otherObject.gameObject)) {
                UpdateFSM(StateInput.ColliderExit);
            }
        }
    }
}
