using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private GameObject uiScreen;

        private GameObject laserPointer = null;
        private LaserPointer laserPointerComponent = null;
        #endregion


        protected void Awake() {
            // listen to when VR is enabled/disabled
            KerbalVR.Events.HmdStatusUpdated.AddListener(OnHmdRunStatusUpdated);

            // create game objects for this hand
            CreateGloveGameObject();
            CreateOtherGameObjects();
        }

        protected void Update() {
            // position the controller object
            transform.position = Scene.Instance.DevicePoseToWorld(Pose.pos);
            transform.rotation = Scene.Instance.DevicePoseToWorld(Pose.rot);

            // apply logic once we have a Glove object
            if (gloveGameObject != null) {
                // update transforms
                GripPosition = GripCollider.transform.TransformPoint(((CapsuleCollider)GripCollider).center);

                // animate grip
                manipulatorAnimator.SetBool("Hold", isGripping);
            }
        }

        protected void OnHmdRunStatusUpdated(bool isRunning) {
            // when VR is turned on, enable this manipulator object so it renders on screen and is interactable
            if (isRunning) {
                gameObject.SetActive(true);
            } else {
                gameObject.SetActive(false);
            }
        }

        protected void CreateGloveGameObject() {
            // get the prefab
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
        }

        protected void CreateOtherGameObjects() {
            // create a laser pointer
            laserPointer = new GameObject();
            laserPointerComponent = laserPointer.AddComponent<LaserPointer>();
            laserPointerComponent.MaxLength = 3f;
            laserPointer.SetActive(true);
            Utils.SetLayer(laserPointer, KerbalVR.Scene.Instance.RenderLayer);
            laserPointer.transform.SetParent(this.transform);
            laserPointer.transform.localPosition = Vector3.zero;

            // create a UI screen
            uiScreen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(uiScreen.GetComponent<MeshCollider>());
            uiScreen.SetActive(true);
            Utils.SetLayer(uiScreen, KerbalVR.Scene.Instance.RenderLayer);
            uiScreen.transform.SetParent(this.transform);
            uiScreen.transform.localPosition = Vector3.forward * 0.1f;
            uiScreen.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            Vector3 uiScreenScale = Vector3.one * 0.6f;
            uiScreenScale.x = uiScreenScale.y * (16f / 9f);
            uiScreen.transform.localScale = uiScreenScale;
            MeshRenderer uiScreenRenderer = uiScreen.GetComponent<MeshRenderer>();
            Material uiScreenMaterial = new Material(Shader.Find("KSP/Alpha/Unlit Transparent"));
            uiScreenMaterial.mainTexture = Core.KspUiRenderTexture;
            uiScreenRenderer.material = uiScreenMaterial;

            // create a visual gizmo
            GameObject gizmo = Utils.CreateGizmo();
            gizmo.SetActive(true);
            Utils.SetLayer(gizmo, KerbalVR.Scene.Instance.RenderLayer);
            gizmo.transform.SetParent(this.transform);
            gizmo.transform.localPosition = Vector3.zero;
            gizmo.transform.localRotation = Quaternion.identity;
            gizmo.transform.localScale = Vector3.one;
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

            // ensure glove object is mirrored correctly
            Vector3 gloveObjectScale = gloveGameObject.transform.localScale;
            if (Role == ETrackedControllerRole.RightHand) {
                gloveObjectScale.y *= -1f;
                gloveGameObject.transform.localScale = gloveObjectScale;
            }
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


    public class LaserPointer : MonoBehaviour {
        public GameObject TargetObject { get; private set; } = null;
        public Vector3 Direction { get; set; } = Vector3.forward;
        public float MaxLength { get; set; } = 1f;
        public Vector2 HitCoordinates { get; private set; } = Vector2.zero;

        private LineRenderer lineRenderer = null;
        private GameObject laserStrike = null;

        protected void Awake() {
            // create a simple line renderer
            lineRenderer = this.gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("KSP/Particles/Alpha Blended"));
            lineRenderer.startColor = new Color(0f, 1f, 1f, 0.9f);
            lineRenderer.endColor = new Color(0f, 1f, 1f, 0.2f);
            lineRenderer.startWidth = 0.009f;
            lineRenderer.endWidth = 0.002f;

            // create an object that represents where the laser is hitting
            laserStrike = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(laserStrike.GetComponent<SphereCollider>());
            laserStrike.SetActive(false);
            laserStrike.transform.SetParent(this.transform);
            laserStrike.transform.localPosition = Vector3.zero;
            laserStrike.transform.localRotation = Quaternion.identity;
            laserStrike.transform.localScale = Vector3.one * 0.04f;
            MeshRenderer laserStrikeRenderer = laserStrike.GetComponent<MeshRenderer>();
            Material laserStrikeMaterial = new Material(Shader.Find("KSP/Alpha/Unlit Transparent"));
            laserStrikeMaterial.color = new Color(0f, 1f, 1f, 0.6f);
            laserStrikeRenderer.material = laserStrikeMaterial;
        }

        protected void Update() {
            Vector3 normalizedDirection = Vector3.Normalize(this.transform.rotation * Direction);
            // position the laser pointer
            lineRenderer.SetPosition(0, this.transform.position);
            lineRenderer.SetPosition(1, this.transform.position + normalizedDirection * MaxLength);

            // cast a ray to see what objects are being hit
            RaycastHit laserHit;
            Ray laserRay = new Ray(this.transform.position, normalizedDirection);
            if (Physics.Raycast(laserRay, out laserHit, MaxLength)) {
                if (laserHit.collider != null) {
                    TargetObject = laserHit.collider.gameObject;
                    TargetObject.SendMessage("OnMouseEnter"); // simulate hovering mouse over target
                    laserStrike.SetActive(true); // show the laser strike
                    laserStrike.transform.position = laserHit.point;
                    lineRenderer.SetPosition(1, laserHit.point);


                    HitCoordinates = laserHit.textureCoord;
                }
            } else {
                if (TargetObject != null) {
                    // if we were hitting a target, simulate the mouse leaving
                    TargetObject.SendMessage("OnMouseExit");
                }

                // drop the target, if any. stop showing the laser strike
                TargetObject = null;
                laserStrike.SetActive(false);
            }
        }

        void OnEnable() {
            Events.ManipulatorLeftUpdated.Listen(OnManipulatorUpdated);
            Events.ManipulatorRightUpdated.Listen(OnManipulatorUpdated);
        }

        void OnDisable() {
            Events.ManipulatorLeftUpdated.Remove(OnManipulatorUpdated);
            Events.ManipulatorRightUpdated.Remove(OnManipulatorUpdated);
        }

        protected void OnManipulatorUpdated(SteamVR_Controller.Device state) {
            // simulate mouse touch events with the trigger
            if (state.GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger)) {
                if (TargetObject != null) {
                    TargetObject.SendMessage("OnMouseDown");
                }

                GameObject go = GameObject.Find("KVR_UI_ResetPosButton");
                if (go != null) {
                    Utils.Log("PointerDown KVR_UI_ResetPosButton");
                    ExecuteEvents.Execute<IPointerDownHandler>(go, new PointerEventData(EventSystem.current), ExecuteEvents.pointerDownHandler);
                }
            }

            if (state.GetPressUp(EVRButtonId.k_EButton_SteamVR_Trigger)) {
                if (TargetObject != null) {
                    TargetObject.SendMessage("OnMouseUp");
                }

                GameObject go = GameObject.Find("KVR_UI_ResetPosButton");
                if (go != null) {
                    Utils.Log("PointerUp KVR_UI_ResetPosButton");
                    ExecuteEvents.Execute<IPointerUpHandler>(go, new PointerEventData(EventSystem.current), ExecuteEvents.pointerUpHandler);
                }
            }
        }
    }
} // namespace KerbalVR
