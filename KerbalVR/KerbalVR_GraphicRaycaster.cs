using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using Valve.VR;

namespace KerbalVR
{
    public class KVR_GraphicRaycaster : GraphicRaycaster
    {
        /*public Camera WorldCamera;
        public GameObject RenderSurface;

        private PointerEventData _data;

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) {
            if (this.WorldCamera == null || this.RenderSurface == null) return;

            //project to see if we hit the tablet surface
            Vector3 pos = eventData.position;
            pos.x /= this.WorldCamera.pixelWidth;
            pos.y /= this.WorldCamera.pixelHeight;
            var ray = this.WorldCamera.ViewportPointToRay(pos);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit)) return;
            if (hit.collider.gameObject != this.RenderSurface) return;

            //create a data if one doesn't exist
            if (_data == null) _data = new PointerEventData(EventSystem.current);

            //scale to the new camera size
            pos = hit.textureCoord;
            if (this.eventCamera != null) {
                pos.x *= this.eventCamera.pixelWidth;
                pos.y *= this.eventCamera.pixelHeight;
            }
            else {
                pos.x *= Screen.width;
                pos.y *= Screen.height;
            }

            //copy other info needed
            _data.position = pos;
            _data.delta = eventData.delta;
            _data.scrollDelta = eventData.scrollDelta;
            _data.pointerCurrentRaycast = eventData.pointerCurrentRaycast;
            _data.pointerEnter = eventData.pointerEnter;

            base.Raycast(_data, resultAppendList);
        }*/


        private KerbalVR.LaserPointer uiPointer;
        private PointerEventData _data;

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            _data = null;
            if (uiPointer == null) {
                uiPointer = GameObject.FindObjectOfType<KerbalVR.LaserPointer>();
            }
            if (uiPointer == null) return;

            Vector2 laserScreenPos = eventData.position;

            string logMsg = "Laser: ";
            if (uiPointer.TargetObject != null) {
                logMsg += uiPointer.TargetObject.name + "\n";
                if (uiPointer.TargetObject.name == "KVR_KSP_UI_Screen") {
                    logMsg += "Coords: " + uiPointer.HitCoordinates.ToString() + "\n";

                    laserScreenPos = uiPointer.HitCoordinates;
                    laserScreenPos.x *= Screen.width;
                    laserScreenPos.y *= Screen.height;

                    eventData.position = laserScreenPos;
                    _data = eventData;
                }
            }

            base.Raycast(eventData, resultAppendList);
        }

        protected override void OnEnable() {
            Events.ManipulatorLeftUpdated.Listen(OnManipulatorUpdated);
            Events.ManipulatorRightUpdated.Listen(OnManipulatorUpdated);
            base.OnEnable();
        }

        protected override void OnDisable() {
            Events.ManipulatorLeftUpdated.Remove(OnManipulatorUpdated);
            Events.ManipulatorRightUpdated.Remove(OnManipulatorUpdated);
            base.OnDisable();
        }

        protected void OnManipulatorUpdated(SteamVR_Controller.Device state) {
            if (state.GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger)) {
                // ExecuteEvents.Execute<IPointerDownHandler>(gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerDownHandler);
            }

            if (state.GetPressUp(EVRButtonId.k_EButton_SteamVR_Trigger)) {
                // ExecuteEvents.Execute<IPointerUpHandler>(gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerUpHandler);
            }
        }
    }
}