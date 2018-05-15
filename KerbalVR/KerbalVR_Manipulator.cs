using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    /// <summary>
    /// Manipulator is a GameObject component that represents your VR "hands",
    /// and serves as a storage container for the latest controller device
    /// button state and tracking position.
    /// 
    /// Future work: this class should probably handle *how* the manipulators
    /// are rendered (MeshFilter model, MeshRenderer material, etc).
    /// I'm not sure how all this Manipulator code is turning out, it might
    /// need to be re-factored or designed better at a later time.
    /// </summary>
    public class Manipulator : MonoBehaviour
    {

        #region Properties
        public SteamVR_Controller.Device State { get; private set; }
        public Vector3 GripPosition { get; private set; }

        public List<GameObject> FingertipCollidedGameObjects { get; private set; } = new List<GameObject>();
        #endregion


        #region Members
        public ETrackedControllerRole role;
        public Collider fingertipCollider;
        public Collider gripCollider;
        public Animator manipulatorAnimator;
        public bool isGripping = false;
        #endregion
        

        protected void Start() {
            FingertipManipulator fingertipManipulator = fingertipCollider.gameObject.AddComponent<FingertipManipulator>();
            FingertipCollidedGameObjects = fingertipManipulator.CollidedGameObjects;
        }

        protected void Update() {
            // enable this object while VR is active
            // TODO: can we make this a little more efficient?
            Utils.SetGameObjectChildrenActive(gameObject, Core.HmdIsEnabled);

            // update transforms
            GripPosition = gripCollider.transform.TransformPoint(((CapsuleCollider)gripCollider).center);

            // animate grip
            manipulatorAnimator.SetBool("Hold", isGripping);
        }

        /// <summary>
        /// Stores the latest transform and button state information.
        /// </summary>
        /// <param name="pose">Updated pose data</param>
        /// <param name="state">Updated state data</param>
        public void UpdateState(SteamVR_Utils.RigidTransform pose, SteamVR_Controller.Device state) {
            State = state;

            // position the controller object
            transform.position = Scene.Instance.DevicePoseToWorld(pose.pos);
            transform.rotation = Scene.Instance.DevicePoseToWorld(pose.rot);
        }
    } // class Manipulator

    public class FingertipManipulator : MonoBehaviour {

        #region Properties
        public List<GameObject> CollidedGameObjects { get; private set; } = new List<GameObject>();
        #endregion

        #region Private Members
        private int numCollidersTouching = 0;
        #endregion

        protected void OnTriggerEnter(Collider other) {
            // keep count of how many other colliders we've entered
            numCollidersTouching += 1;

            // keep track of what colliders we're touching
            if (!CollidedGameObjects.Contains(other.gameObject))
                CollidedGameObjects.Add(other.gameObject);
        }

        protected void OnTriggerExit(Collider other) {
            if (CollidedGameObjects.Contains(other.gameObject))
                CollidedGameObjects.Remove(other.gameObject);

            // when number of colliders exited drops back down to zero, reset default color
            numCollidersTouching -= 1;
            if (numCollidersTouching <= 0) {
                numCollidersTouching = 0;
            }
        }
    } // class FingertipManipulator
} // namespace KerbalVR
