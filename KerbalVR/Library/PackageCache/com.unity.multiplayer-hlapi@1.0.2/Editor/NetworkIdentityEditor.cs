#if ENABLE_UNET
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor
{
    [CustomEditor(typeof(NetworkIdentity), true)]
    [CanEditMultipleObjects]
    [Obsolete("The high level API classes are deprecated and will be removed in the future.")]
    public class NetworkIdentityEditor : Editor
    {
        SerializedProperty m_ServerOnlyProperty;
        SerializedProperty m_LocalPlayerAuthorityProperty;

        GUIContent m_ServerOnlyLabel;
        GUIContent m_LocalPlayerAuthorityLabel;
        GUIContent m_SpawnLabel;

        NetworkIdentity m_NetworkIdentity;
        bool m_Initialized;
        bool m_ShowObservers;

        void Init()
        {
            if (m_Initialized)
            {
                return;
            }
            m_Initialized = true;
            m_NetworkIdentity = target as NetworkIdentity;

            m_ServerOnlyProperty = serializedObject.FindProperty("m_ServerOnly");
            m_LocalPlayerAuthorityProperty = serializedObject.FindProperty("m_LocalPlayerAuthority");

            m_ServerOnlyLabel = TextUtility.TextContent("Server Only", "True if the object should only exist on the server.");
            m_LocalPlayerAuthorityLabel = TextUtility.TextContent("Local Player Authority", "True if this object will be controlled by a player on a client.");
            m_SpawnLabel = TextUtility.TextContent("Spawn Object", "This causes an unspawned server object to be spawned on clients");
        }

        public override void OnInspectorGUI()
        {
            if (m_ServerOnlyProperty == null)
            {
                m_Initialized = false;
            }

            Init();

            serializedObject.Update();

            if (m_ServerOnlyProperty.boolValue)
            {
                EditorGUILayout.PropertyField(m_ServerOnlyProperty, m_ServerOnlyLabel);
                EditorGUILayout.LabelField("Local Player Authority cannot be set for server-only objects");
            }
            else if (m_LocalPlayerAuthorityProperty.boolValue)
            {
                EditorGUILayout.LabelField("Server Only cannot be set for Local Player Authority objects");
                EditorGUILayout.PropertyField(m_LocalPlayerAuthorityProperty, m_LocalPlayerAuthorityLabel);
            }
            else
            {
                EditorGUILayout.PropertyField(m_ServerOnlyProperty, m_ServerOnlyLabel);
                EditorGUILayout.PropertyField(m_LocalPlayerAuthorityProperty, m_LocalPlayerAuthorityLabel);
            }

            serializedObject.ApplyModifiedProperties();

            if (!Application.isPlaying)
            {
                return;
            }

            // Runtime actions below here

            EditorGUILayout.Separator();

            if (m_NetworkIdentity.observers != null && m_NetworkIdentity.observers.Count > 0)
            {
                m_ShowObservers = EditorGUILayout.Foldout(m_ShowObservers, "Observers");
                if (m_ShowObservers)
                {
                    EditorGUI.indentLevel += 1;
                    foreach (var o in m_NetworkIdentity.observers)
                    {
                        GameObject obj = null;
                        foreach (var p in o.playerControllers)
                        {
                            if (p != null)
                            {
                                obj = p.gameObject;
                                break;
                            }
                        }
                        if (obj)
                            EditorGUILayout.ObjectField("Connection " + o.connectionId, obj, typeof(GameObject), false);
                        else
                            EditorGUILayout.TextField("Connection " + o.connectionId);
                    }
                    EditorGUI.indentLevel -= 1;
                }
            }

            if (PrefabUtility.IsPartOfPrefabAsset(m_NetworkIdentity.gameObject))
                return;

            if (m_NetworkIdentity.gameObject.activeSelf && m_NetworkIdentity.netId.IsEmpty() && NetworkServer.active)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(m_SpawnLabel);
                if (GUILayout.Toggle(false, "Spawn", EditorStyles.miniButtonLeft))
                {
                    NetworkServer.Spawn(m_NetworkIdentity.gameObject);
                    EditorUtility.SetDirty(target);  // preview window STILL doens't update immediately..
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif //ENABLE_UNET
