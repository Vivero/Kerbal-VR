using System;
using UnityEngine;

namespace KerbalVR
{
    public class ConfigUtils
    {
        public static ConfigNode GetModuleConfigNode(string propName, int moduleId) {
            ConfigNode moduleConfigNode = null;

            // look through all the config nodes in the GameDatabase
            ConfigNode[] propConfigNodes = GameDatabase.Instance.GetConfigNodes("PROP");
            for (int i = 0; i < propConfigNodes.Length; i++) {
                ConfigNode propConfigNode = propConfigNodes[i];

                // find one that matches the prop
                if (propConfigNode.GetValue("name") == propName) {
                    moduleConfigNode = propConfigNode.GetNodes("MODULE")[moduleId];
                    break;
                }
            }

            return moduleConfigNode;
        }
    }
}
