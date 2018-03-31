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
    /// </summary>
    public class Manipulator : MonoBehaviour
    {

        #region Properties
        public SteamVR_Controller.Device State { get; private set; }

        public List<GameObject> CollidedGameObjects { get; private set; } = new List<GameObject>();
        #endregion

        #region Members
        public ETrackedControllerRole role;
        public Color defaultColor = Color.white;
        public Color activeColor = Color.black;
        #endregion

        #region Private Members
        private MeshRenderer meshRenderer;
        private int numCollidersTouching = 0;
        #endregion


        void Start() {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        void Update() {
#if DEBUG
            meshRenderer.enabled = Core.HmdIsEnabled;
#endif
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

            // set the layer to render to
            gameObject.layer = Scene.Instance.RenderLayer;
        }

        void OnTriggerEnter(Collider other) {
            // keep count of how many other colliders we've entered
            numCollidersTouching += 1;
            meshRenderer.sharedMaterial.color = activeColor;
            if (!CollidedGameObjects.Contains(other.gameObject))
                CollidedGameObjects.Add(other.gameObject);
        }

        void OnTriggerExit(Collider other) {
            // when number of colliders exited drops back down to zero, reset default color
            if (CollidedGameObjects.Contains(other.gameObject))
                CollidedGameObjects.Remove(other.gameObject);

            numCollidersTouching -= 1;
            if (numCollidersTouching <= 0) {
                numCollidersTouching = 0;
                meshRenderer.sharedMaterial.color = defaultColor;
            }
        }
    } // class Manipulator
} // namespace KerbalVR
