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

        public static Animation GetAnimation(InternalProp prop, ConfigNode configuration, string configKey, out string configValue) {
            configValue = "";
            bool success = configuration.TryGetValue(configKey, ref configValue);
            if (success) {
                Animation[] animations = prop.FindModelAnimators(configValue);
                if (animations.Length > 0) {
                    return animations[0];
                } else {
                    throw new ArgumentException("InternalProp \"" + prop.name +
                        "\" does not have animations (config node " + configuration.id + ")");
                }
            } else {
                throw new ArgumentException(configKey + " not specified for " +
                    prop.name + " (config node " + configuration.id + ")");
            }
        }

        public static Transform GetTransform(InternalProp prop, ConfigNode configuration, string configKey) {
            string transformName = "";
            bool success = configuration.TryGetValue(configKey, ref transformName);
            if (!success) throw new ArgumentException(configKey + " not specified for " +
                prop.name + " (config node " + configuration.id + ")");

            Transform transform = prop.FindModelTransform(transformName);
            if (transform == null) throw new ArgumentException("Transform \"" + transformName +
                "\" not found for " + prop.name + " (config node " + configuration.id + ")");

            return transform;
        }

        public static AudioSource SetupAudioClip(InternalProp prop, ConfigNode configuration, string configKey) {
            string audioClipName = "";
            bool success = configuration.TryGetValue(configKey, ref audioClipName);
            if (!success) throw new ArgumentException(configKey + " not specified for " +
                prop.name + " (config node " + configuration.id + ")");

            AudioClip audioClip = GameDatabase.Instance.GetAudioClip(audioClipName);
            if (audioClip == null) throw new ArgumentException("AudioClip \"" + audioClipName +
                "\" not found for " + prop.name + " (config node " + configuration.id + ")");

            AudioSource audioSource = prop.gameObject.AddComponent<AudioSource>();
            audioSource.clip = audioClip;
            audioSource.Stop();
            audioSource.volume = GameSettings.SHIP_VOLUME;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = 10f;
            audioSource.minDistance = 2f;
            audioSource.dopplerLevel = 0f;
            audioSource.panStereo = 0f;
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.pitch = 1f;

            return audioSource;
        }
    }
}
