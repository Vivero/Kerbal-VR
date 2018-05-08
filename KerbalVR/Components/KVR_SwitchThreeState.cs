using System;
using UnityEngine;

namespace KerbalVR.Components
{
    public class KVR_SwitchThreeState : KVR_Switch
    {
        #region Types
        public enum StateInput {
            ColliderDownEnter,
            ColliderDownExit,
            ColliderMiddleEnter,
            ColliderMiddleExit,
            ColliderUpEnter,
            ColliderUpExit,
        }

        public enum FSMState {
            IsDown,
            IsMiddle,
            IsUp,
            IsWaitingForMiddle,
            IsWaitingForUpOrDown,
        }
        #endregion

        #region Properties
        public Transform ColliderDownTransform { get; private set; }
        public Transform ColliderMiddleTransform { get; private set; }
        public Transform ColliderUpTransform { get; private set; }
        #endregion

        #region Private Members
        private GameObject colliderDownGameObject;
        private GameObject colliderMiddleGameObject;
        private GameObject colliderUpGameObject;
        private FSMState fsmState;
        #endregion

        #region Constructors
        public KVR_SwitchThreeState(InternalProp prop, ConfigNode configuration) : base(prop, configuration) {
            // collider game objects
            ColliderDownTransform = ConfigUtils.GetTransform(prop, configuration, "colliderDownTransformName");
            colliderDownGameObject = ColliderDownTransform.gameObject;
            colliderDownGameObject.AddComponent<KVR_ActionableCollider>().module = this;
            
            ColliderMiddleTransform = ConfigUtils.GetTransform(prop, configuration, "colliderMiddleTransformName");
            colliderMiddleGameObject = ColliderMiddleTransform.gameObject;
            colliderMiddleGameObject.AddComponent<KVR_ActionableCollider>().module = this;
            
            ColliderUpTransform = ConfigUtils.GetTransform(prop, configuration, "colliderUpTransformName");
            colliderUpGameObject = ColliderUpTransform.gameObject;
            colliderUpGameObject.AddComponent<KVR_ActionableCollider>().module = this;

            // set initial state
            fsmState = FSMState.IsDown;
        }
        #endregion

        public override void Update() {
            bool isAnimationPlaying = SwitchAnimation.isPlaying;

            if (SwitchAnimation.isPlaying &&
                ((animationState.speed > 0f && animationState.normalizedTime >= targetAnimationEndTime) ||
                (animationState.speed < 0f && animationState.normalizedTime <= targetAnimationEndTime))) {

                Utils.Log("Stopped anim at " + animationState.normalizedTime.ToString("F2"));
                SwitchAnimation.Stop();
            }

            // check if animation finished playing
            if (!isAnimationPlaying && isAnimationPlayingPrev) {
                ExecuteSignal();
            }

            // keep track of whether animation was playing
            isAnimationPlayingPrev = isAnimationPlaying;
        }

        private void UpdateFSM(StateInput colliderInput) {
            Utils.Log("UpdateFSM, state = " + CurrentState + ", fsm = " + fsmState + ", input = " + colliderInput);
            switch (fsmState) {
                case FSMState.IsDown:
                    if (colliderInput == StateInput.ColliderDownEnter) {
                        fsmState = FSMState.IsWaitingForMiddle;
                    }
                    break;

                case FSMState.IsWaitingForMiddle:
                    if (colliderInput == StateInput.ColliderDownExit) {
                        fsmState = FSMState.IsDown;
                    } else if (colliderInput == StateInput.ColliderUpExit) {
                        fsmState = FSMState.IsUp;
                    } else if (colliderInput == StateInput.ColliderMiddleEnter) {
                        fsmState = FSMState.IsMiddle;
                        PlayToState(State.Middle);
                    }
                    break;

                case FSMState.IsMiddle:
                    if (colliderInput == StateInput.ColliderMiddleEnter) {
                        fsmState = FSMState.IsWaitingForUpOrDown;
                    }
                    break;

                case FSMState.IsWaitingForUpOrDown:
                    if (colliderInput == StateInput.ColliderMiddleExit) {
                        fsmState = FSMState.IsMiddle;
                    } else if (colliderInput == StateInput.ColliderUpEnter) {
                        fsmState = FSMState.IsUp;
                        PlayToState(State.Up);
                    } else if (colliderInput == StateInput.ColliderDownEnter) {
                        fsmState = FSMState.IsDown;
                        PlayToState(State.Down);
                    }
                    break;

                case FSMState.IsUp:
                    if (colliderInput == StateInput.ColliderUpEnter) {
                        fsmState = FSMState.IsWaitingForMiddle;
                    }
                    break;

                default:
                    break;
            }
        }

        public override void OnColliderEntered(Collider thisObject, Collider otherObject) {
            if (DeviceManager.IsManipulatorFingertip(otherObject)) {
                if (thisObject.gameObject == colliderDownGameObject) {
                    UpdateFSM(StateInput.ColliderDownEnter);
                } else if (thisObject.gameObject == colliderMiddleGameObject) {
                    UpdateFSM(StateInput.ColliderMiddleEnter);
                } else if (thisObject.gameObject == colliderUpGameObject) {
                    UpdateFSM(StateInput.ColliderUpEnter);
                }
            }
        }

        public override void OnColliderExited(Collider thisObject, Collider otherObject) {
            if (DeviceManager.IsManipulatorFingertip(otherObject)) {
                if (thisObject.gameObject == colliderDownGameObject) {
                    UpdateFSM(StateInput.ColliderDownExit);
                } else if (thisObject.gameObject == colliderMiddleGameObject) {
                    UpdateFSM(StateInput.ColliderMiddleExit);
                } else if (thisObject.gameObject == colliderUpGameObject) {
                    UpdateFSM(StateInput.ColliderUpExit);
                }
            }
        }
    }
}
