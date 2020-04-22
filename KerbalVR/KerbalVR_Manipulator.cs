using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    /// <summary>
    /// Manipulator is a GameObject component that represents your VR "hands",
    /// and serves as a storage container for the latest controller device
    /// button state, tracking position, and rendering.
    /// </summary>
    public class Manipulator : MonoBehaviour
    {
        #region Constants
        public readonly Vector3 GLOVE_POSITION = new Vector3(0f, 0.02f, -0.1f);
        public readonly Vector3 GLOVE_ROTATION = new Vector3(-45f, 0f, 90f);
        #endregion

        #region Properties
        public SteamVR_Controller.Device State { get; private set; }
        public SteamVR_Utils.RigidTransform Pose { get; private set; }
        public Vector3 GripPosition { get; private set; }

        // Manipulator object properties
        private float _manipulatorSize = 0.45f;
        public float ManipulatorSize {
            get {
                return _manipulatorSize;
            }
            set {
                _manipulatorSize = value;
                SetManipulatorSize(_manipulatorSize);
            }
        }

        public List<GameObject> FingertipCollidedGameObjects { get; private set; } = new List<GameObject>();
        #endregion


        #region Members
        public ETrackedControllerRole role;
        public Collider fingertipCollider;
        public Collider gripCollider;
        public Animator manipulatorAnimator;
        public bool isGripping = false;
        public GameObject gloveGameObject = null;
        #endregion


        protected void Update() {
            // determine if we need to load the Glove object
            if (gloveGameObject == null) {
                LoadManipulatorRenderGameObject();
            }

            // enable this object while VR is active
            // TODO: can we make this a little more efficient?
            Utils.SetGameObjectChildrenActive(this.gameObject, Core.HmdIsEnabled);

            // apply logic once we have a Glove object
            if (gloveGameObject != null) {
                // update transforms
                GripPosition = gripCollider.transform.TransformPoint(((CapsuleCollider)gripCollider).center);

                // animate grip
                manipulatorAnimator.SetBool("Hold", isGripping);
            }

            // position the controller object
            transform.position = Scene.Instance.DevicePoseToWorld(Pose.pos);
            transform.rotation = Scene.Instance.DevicePoseToWorld(Pose.rot);
        }

        protected void LoadManipulatorRenderGameObject() {
            AssetLoader assetLoader = AssetLoader.Instance;
            if (assetLoader != null) {
                // define the render model
                GameObject glovePrefab = AssetLoader.Instance.GetGameObject("GlovePrefab");
                if (glovePrefab == null) {
                    Utils.LogError("GameObject \"GlovePrefab\" was not found!");
                    return;
                }
                gloveGameObject = Instantiate(glovePrefab);
                gloveGameObject.transform.SetParent(this.transform);
                Vector3 gloveObjectScale = Vector3.one * ManipulatorSize;
                if (role == ETrackedControllerRole.RightHand) {
                    gloveObjectScale.y *= -1f;
                }
                gloveGameObject.transform.localPosition = GLOVE_POSITION;
                gloveGameObject.transform.localRotation = Quaternion.Euler(GLOVE_ROTATION);
                gloveGameObject.transform.localScale = gloveObjectScale;
                Utils.SetLayer(gloveGameObject, KerbalVR.Scene.Instance.RenderLayer);

                // define the colliders
                Transform colliderObject = gloveGameObject.transform.Find("HandDummy/Arm Bone L/Wrist Bone L/Finger Index Bone L1/Finger Index Bone L2/Finger Index Bone L3/Finger Index Bone L4");
                if (colliderObject == null) {
                    Utils.LogWarning("Manipulator is missing fingertip collider child object");
                    return;
                }
                fingertipCollider = colliderObject.GetComponent<SphereCollider>();

                colliderObject = gloveGameObject.transform.Find("HandDummy/Arm Bone L/Wrist Bone L");
                if (colliderObject == null) {
                    Utils.LogWarning("Manipulator is missing grip collider child object");
                    return;
                }
                gripCollider = colliderObject.GetComponent<CapsuleCollider>();


                // retrieve the animator
                manipulatorAnimator = gloveGameObject.GetComponent<Animator>();

                FingertipManipulator fingertipManipulator = fingertipCollider.gameObject.AddComponent<FingertipManipulator>();
                FingertipCollidedGameObjects = fingertipManipulator.CollidedGameObjects;
            }
        }

        /// <summary>
        /// Sets the size of each "VR hand".
        /// </summary>
        /// <param name="size">Size of the hand, in meters.</param>
        protected void SetManipulatorSize(float size) {
            if (gloveGameObject != null) {
                gloveGameObject.transform.localScale = Vector3.one * ManipulatorSize;
            }
        }

        /// <summary>
        /// Stores the latest transform and button state information.
        /// </summary>
        /// <param name="pose">Updated pose data</param>
        /// <param name="state">Updated state data</param>
        public void UpdateState(SteamVR_Utils.RigidTransform pose, SteamVR_Controller.Device state) {
            State = state;
            Pose = pose;
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
