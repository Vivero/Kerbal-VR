using System;
using UnityEngine;

namespace KerbalVR.Modules {
    public class HandRail : InternalModule {
        protected ConfigNode moduleConfigNode;

        protected void Start() {
            // no setup needed in editor mode
            if (HighLogic.LoadedScene == GameScenes.EDITOR) return;

            // obtain module configuration
            moduleConfigNode = KerbalVR.ConfigUtils.GetModuleConfigNode(internalProp.name, moduleID);

            Utils.Log("DEBUG " + moduleConfigNode);
        }
    }
}
