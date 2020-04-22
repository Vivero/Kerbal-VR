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
        public ETrackedControllerRole Role { get; private set; } = ETrackedControllerRole.Invalid;
        public SteamVR_Controller.Device State { get; private set; }
        public SteamVR_Utils.RigidTransform Pose { get; private set; }
        public Vector3 GripPosition { get; private set; }
        public Collider FingertipCollider { get; private set; }
        public Collider GripCollider { get; private set; }

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
        public bool isGripping = false;
        #endregion


        #region Private Members
        private Animator manipulatorAnimator;
        private GameObject glovePrefab = null;
        private GameObject gloveGameObject = null;
        private GameObject laserPointer = null;
        private LineRenderer laserPointerRenderer = null;
        private GameObject uiScreen;
        #endregion


        protected void Awake() {
            // listen to when VR is enabled/disabled
            KerbalVR.Events.HmdStatusUpdated.AddListener(OnHmdRunStatusUpdated);
        }

        protected void Update() {
            // determine if we need to load the Glove object
            if (gloveGameObject == null) {
                LoadManipulatorRenderGameObject();
            }

            // apply logic once we have a Glove object
            if (gloveGameObject != null) {
                // update transforms
                GripPosition = GripCollider.transform.TransformPoint(((CapsuleCollider)GripCollider).center);

                // animate grip
                manipulatorAnimator.SetBool("Hold", isGripping);
            }

            // position the controller object
            transform.position = Scene.Instance.DevicePoseToWorld(Pose.pos);
            transform.rotation = Scene.Instance.DevicePoseToWorld(Pose.rot);
        }

        protected void OnHmdRunStatusUpdated(bool isRunning) {
            // when VR is turned on, enable this manipulator object so it renders on screen and is interactable
            if (isRunning) {
                gameObject.SetActive(true);
            } else {
                gameObject.SetActive(false);
            }
        }

        protected void LoadManipulatorRenderGameObject() {
            AssetLoader assetLoader = AssetLoader.Instance;
            if (assetLoader != null) {
                // define the render model
                glovePrefab = AssetLoader.Instance.GetGameObject("GlovePrefab");
                if (glovePrefab == null) {
                    Utils.LogError("GameObject \"GlovePrefab\" was not found!");
                    return;
                }
                gloveGameObject = Instantiate(glovePrefab);
                gloveGameObject.transform.SetParent(this.transform);
                Vector3 gloveObjectScale = Vector3.one * ManipulatorSize;
                if (Role == ETrackedControllerRole.RightHand) {
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
                FingertipCollider = colliderObject.GetComponent<SphereCollider>();

                colliderObject = gloveGameObject.transform.Find("HandDummy/Arm Bone L/Wrist Bone L");
                if (colliderObject == null) {
                    Utils.LogWarning("Manipulator is missing grip collider child object");
                    return;
                }
                GripCollider = colliderObject.GetComponent<CapsuleCollider>();


                // retrieve the animator
                manipulatorAnimator = gloveGameObject.GetComponent<Animator>();

                FingertipManipulator fingertipManipulator = FingertipCollider.gameObject.AddComponent<FingertipManipulator>();
                FingertipCollidedGameObjects = fingertipManipulator.CollidedGameObjects;

                // create a laser pointer
                laserPointer = new GameObject();
#if DEBUG
                laserPointer.SetActive(true);
#else
                laserPointer.SetActive(false);
#endif
                Utils.SetLayer(laserPointer, KerbalVR.Scene.Instance.RenderLayer);
                laserPointer.transform.SetParent(gloveGameObject.transform);
                laserPointer.transform.localPosition = Vector3.zero;
                laserPointerRenderer = laserPointer.AddComponent<LineRenderer>();
                laserPointerRenderer.material = new Material(Shader.Find("KSP/Particles/Alpha Blended"));
                laserPointerRenderer.startColor = Color.blue;
                laserPointerRenderer.endColor = Color.red;
                laserPointerRenderer.startWidth = 0.01f;
                laserPointerRenderer.endWidth = 0.01f;

                // create a UI screen
                uiScreen = GameObject.CreatePrimitive(PrimitiveType.Quad);
#if DEBUG
                uiScreen.SetActive(true);
#else
                uiScreen.SetActive(false);
#endif
                Utils.SetLayer(uiScreen, KerbalVR.Scene.Instance.RenderLayer);
                uiScreen.transform.SetParent(gloveGameObject.transform);
                uiScreen.transform.localPosition = Vector3.forward * 0.4f;
                uiScreen.transform.localRotation = Quaternion.Euler(0f, 270f, 0f);
                uiScreen.transform.localScale = Vector3.one * 0.8f;
                MeshRenderer uiScreenRenderer = uiScreen.GetComponent<MeshRenderer>();
                uiScreenRenderer.material = new Material(Shader.Find("KSP/UnlitColor"));
                uiScreenRenderer.material.color = Color.green;
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

        public void SetRole(ETrackedControllerRole role) {
            if (role != ETrackedControllerRole.LeftHand && role != ETrackedControllerRole.RightHand) {
                throw new ArgumentException("Cannot assign controller role \"" + role.ToString() + "\"");
            }

            // assign role
            this.Role = role;
        }
    } // class Manipulator


    public class FingertipManipulator : MonoBehaviour {

        /// <summary>
        /// A list of objects that are colliding with this fingertip.
        /// </summary>
        public List<GameObject> CollidedGameObjects { get; private set; } = new List<GameObject>();

        private int numCollidersTouching = 0;

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
