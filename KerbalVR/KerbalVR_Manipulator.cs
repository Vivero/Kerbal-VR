using System;
using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    public class Manipulator : MonoBehaviour
    {
        #region Properties
        public SteamVR_Controller.Device State { get; private set; }
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


        // TODO: think about these fields
        public float movementVelocity = 1f;


        void Start() {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        void Update() {
#if DEBUG
            meshRenderer.enabled = KerbalVR.HmdIsEnabled;
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
        }

        void OnTriggerExit(Collider other) {
            // when number of colliders exited drops back down to zero, reset default color
            numCollidersTouching -= 1;
            if (numCollidersTouching <= 0) {
                numCollidersTouching = 0;
                meshRenderer.sharedMaterial.color = defaultColor;
            }
        }
    }
}
