using System;
using UnityEngine;

namespace KerbalVR.Components
{
    public class KVR_SwitchTwoState : KVR_Switch
    {
        #region Types
        public enum StateInput {
            ColliderUpEnter,
            ColliderUpExit,
            ColliderDownEnter,
            ColliderDownExit,
        }

        public enum FSMState {
            IsDown,
            IsUp,
            IsWaitingForDown,
            IsWaitingForUp,
        }
        #endregion

        #region Properties
        public Transform ColliderDownTransform { get; private set; }
        public Transform ColliderUpTransform { get; private set; }
        #endregion

        #region Private Members
        private GameObject colliderDownGameObject;
        private GameObject colliderUpGameObject;
        private FSMState fsmState;
        #endregion

        #region Constructors
        public KVR_SwitchTwoState(InternalProp prop, ConfigNode configuration) : base(prop, configuration) {
            // collider game objects
            ColliderDownTransform = ConfigUtils.GetTransform(prop, configuration, "colliderDownTransformName");
            colliderDownGameObject = ColliderDownTransform.gameObject;
            colliderDownGameObject.AddComponent<KVR_ActionableCollider>().module = this;
            
            ColliderUpTransform = ConfigUtils.GetTransform(prop, configuration, "colliderUpTransformName");
            colliderUpGameObject = ColliderUpTransform.gameObject;
            colliderUpGameObject.AddComponent<KVR_ActionableCollider>().module = this;

            // set initial state
            fsmState = FSMState.IsDown;
        }
        #endregion

        public override void Update() {
            bool isAnimationPlaying = SwitchAnimation.isPlaying;

            // check if animation finished playing
            if (!isAnimationPlaying && isAnimationPlayingPrev) {
                ExecuteSignal();
            }

            // keep track of whether animation was playing
            isAnimationPlayingPrev = isAnimationPlaying;
        }

        private void UpdateFSM(StateInput colliderInput) {
            switch (fsmState) {
                case FSMState.IsUp:
                    if (colliderInput == StateInput.ColliderUpEnter) {
                        fsmState = FSMState.IsWaitingForDown;
                    }
                    break;

                case FSMState.IsWaitingForDown:
                    if (colliderInput == StateInput.ColliderUpExit) {
                        fsmState = FSMState.IsUp;
                    } else if (colliderInput == StateInput.ColliderDownEnter) {
                        fsmState = FSMState.IsDown;
                        PlayToState(State.Down);
                    }
                    break;

                case FSMState.IsDown:
                    if (colliderInput == StateInput.ColliderDownEnter) {
                        fsmState = FSMState.IsWaitingForUp;
                    }
                    break;

                case FSMState.IsWaitingForUp:
                    if (colliderInput == StateInput.ColliderDownExit) {
                        fsmState = FSMState.IsDown;
                    } else if (colliderInput == StateInput.ColliderUpEnter) {
                        fsmState = FSMState.IsUp;
                        PlayToState(State.Up);
                    }
                    break;

                default:
                    break;
            }
        }

        public override void OnColliderEntered(Collider thisObject, Collider otherObject) {
            if (DeviceManager.IsManipulatorFingertip(otherObject)) {
                if (thisObject.gameObject == colliderUpGameObject) {
                    UpdateFSM(StateInput.ColliderUpEnter);
                } else if (thisObject.gameObject == colliderDownGameObject) {
                    UpdateFSM(StateInput.ColliderDownEnter);
                }
            }
        }

        public override void OnColliderExited(Collider thisObject, Collider otherObject) {
            if (DeviceManager.IsManipulatorFingertip(otherObject)) {
                if (thisObject.gameObject == colliderUpGameObject) {
                    UpdateFSM(StateInput.ColliderUpExit);
                } else if (thisObject.gameObject == colliderDownGameObject) {
                    UpdateFSM(StateInput.ColliderDownExit);
                }
            }
        }
    }
}
