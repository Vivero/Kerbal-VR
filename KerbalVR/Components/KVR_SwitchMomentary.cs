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
        private GameObject colliderGameObject;
        private FSMState fsmState;
        #endregion

        #region Constructors
        public KVR_SwitchMomentary(InternalProp prop, ConfigNode configuration) : base(prop, configuration) {
            // collider game object
            ColliderTransform = ConfigUtils.GetTransform(prop, configuration, "colliderTransformName");
            colliderGameObject = ColliderTransform.gameObject;
            colliderGameObject.AddComponent<KVR_ActionableCollider>().module = this;

            // set initial state
            fsmState = FSMState.IsDown;
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
            if (DeviceManager.IsManipulatorFingertip(otherObject)) {
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
            if (DeviceManager.IsManipulatorFingertip(otherObject)) {
                UpdateFSM(StateInput.ColliderExit);
            }
        }
    }
}
