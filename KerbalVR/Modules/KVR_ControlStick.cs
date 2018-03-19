using UnityEngine;

namespace KerbalVR.Modules
{
    public class KVR_ControlStick : InternalModule
    {
        #region KSP Config Fields
        [KSPField]
        public string transformStickCollider = string.Empty;
        #endregion

        private Transform stickCollider;

        void Start() {
            Utils.PrintGameObjectTree(gameObject);

            stickCollider = internalProp.FindModelTransform(transformStickCollider);
            if (stickCollider != null) {
                Utils.Log("stickCollider " + stickCollider.GetComponent<CapsuleCollider>().bounds);
            } else {
                Utils.LogWarning("stickCollider null!");
            }
            

        }
    }
}
