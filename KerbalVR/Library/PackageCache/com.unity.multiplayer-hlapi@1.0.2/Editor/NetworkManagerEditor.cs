#if ENABLE_UNET
using System;
using System.IO;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;
using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    [CustomEditor(typeof(NetworkManager), true)]
    [CanEditMultipleObjects]
    [Obsolete("The high level API classes are deprecated and will be removed in the future.")]
    public class NetworkManagerEditor : Editor
    {
        protected SerializedProperty m_DontDestroyOnLoadProperty;
        protected SerializedProperty m_RunInBackgroundProperty;
        protected SerializedProperty m_ScriptCRCCheckProperty;
        SerializedProperty m_NetworkAddressProperty;

        SerializedProperty m_NetworkPortProperty;
        SerializedProperty m_ServerBindToIPProperty;
        SerializedProperty m_ServerBindAddressProperty;
        SerializedProperty m_MaxDelayProperty;
        SerializedProperty m_MaxBufferedPacketsProperty;
        SerializedProperty m_AllowFragmentationProperty;

        protected SerializedProperty m_LogLevelProperty;
        SerializedProperty m_MatchHostProperty;
        SerializedProperty m_MatchPortProperty;
        SerializedProperty m_MatchNameProperty;
        SerializedProperty m_MatchSizeProperty;

        SerializedProperty m_PlayerPrefabProperty;
        SerializedProperty m_AutoCreatePlayerProperty;
        SerializedProperty m_PlayerSpawnMethodProperty;
        SerializedProperty m_SpawnListProperty;

        SerializedProperty m_CustomConfigProperty;

        SerializedProperty m_UseWebSocketsProperty;
        SerializedProperty m_UseSimulatorProperty;
        SerializedProperty m_SimulatedLatencyProperty;
        SerializedProperty m_PacketLossPercentageProperty;

        SerializedProperty m_ChannelListProperty;
        ReorderableList m_ChannelList;

        GUIContent m_ShowNetworkLabel;
        GUIContent m_ShowSpawnLabel;

        GUIContent m_OfflineSceneLabel;
        GUIContent m_OnlineSceneLabel;
        protected GUIContent m_DontDestroyOnLoadLabel;
        protected GUIContent m_RunInBackgroundLabel;
        protected GUIContent m_ScriptCRCCheckLabel;

        GUIContent m_MaxConnectionsLabel;
        GUIContent m_MinUpdateTimeoutLabel;
        GUIContent m_ConnectTimeoutLabel;
        GUIContent m_DisconnectTimeoutLabel;
        GUIContent m_PingTimeoutLabel;

        GUIContent m_ThreadAwakeTimeoutLabel;
        GUIContent m_ReactorModelLabel;
        GUIContent m_ReactorMaximumReceivedMessagesLabel;
        GUIContent m_ReactorMaximumSentMessagesLabel;

        GUIContent m_MaxBufferedPacketsLabel;
        GUIContent m_AllowFragmentationLabel;
        GUIContent m_UseWebSocketsLabel;
        GUIContent m_UseSimulatorLabel;
        GUIContent m_LatencyLabel;
        GUIContent m_PacketLossPercentageLabel;
        GUIContent m_MatchHostLabel;
        GUIContent m_MatchPortLabel;
        GUIContent m_MatchNameLabel;
        GUIContent m_MatchSizeLabel;

        GUIContent m_NetworkAddressLabel;
        GUIContent m_NetworkPortLabel;
        GUIContent m_ServerBindToIPLabel;
        GUIContent m_ServerBindAddressLabel;
        GUIContent m_MaxDelayLabel;

        GUIContent m_PlayerPrefabLabel;
        GUIContent m_AutoCreatePlayerLabel;
        GUIContent m_PlayerSpawnMethodLabel;

        GUIContent m_AdvancedConfigurationLabel;

        ReorderableList m_SpawnList;

        protected bool m_Initialized;

        protected NetworkManager m_NetworkManager;

        protected void Init()
        {
            if (m_Initialized)
            {
                return;
            }
            m_Initialized = true;
            m_NetworkManager = target as NetworkManager;

            m_ShowNetworkLabel = TextUtility.TextContent("Network Info", "Network host settings");
            m_ShowSpawnLabel = TextUtility.TextContent("Spawn Info", "Registered spawnable objects");
            m_OfflineSceneLabel = TextUtility.TextContent("Offline Scene", "The scene loaded when the network goes offline (disconnected from server)");
            m_OnlineSceneLabel = TextUtility.TextContent("Online Scene", "The scene loaded when the network comes online (connected to server)");
            m_DontDestroyOnLoadLabel = TextUtility.TextContent("Don't Destroy on Load", "Enable to persist the NetworkManager across scene changes.");
            m_RunInBackgroundLabel = TextUtility.TextContent("Run in Background", "Enable to ensure that the application runs when it does not have focus.\n\nThis is required when testing multiple instances on a single machine, but not recommended for shipping on mobile platforms.");
            m_ScriptCRCCheckLabel = TextUtility.TextContent("Script CRC Check", "Enable to cause a CRC check between server and client that ensures the NetworkBehaviour scripts match.\n\nThis may not be appropriate in some cases, such as when the client and server are different Unity projects.");

            m_MaxConnectionsLabel  = TextUtility.TextContent("Max Connections", "Maximum number of network connections");
            m_MinUpdateTimeoutLabel = TextUtility.TextContent("Min Update Timeout", "Minimum time network thread waits for events");
            m_ConnectTimeoutLabel = TextUtility.TextContent("Connect Timeout", "Time to wait for timeout on connecting");
            m_DisconnectTimeoutLabel = TextUtility.TextContent("Disconnect Timeout", "Time to wait for detecting disconnect");
            m_PingTimeoutLabel = TextUtility.TextContent("Ping Timeout", "Time to wait for ping messages");

            m_ThreadAwakeTimeoutLabel = TextUtility.TextContent("Thread Awake Timeout", "The minimum time period when system will check if there are any messages for send (or receive).");
            m_ReactorModelLabel = TextUtility.TextContent("Reactor Model", "Defines reactor model for the network library");
            m_ReactorMaximumReceivedMessagesLabel = TextUtility.TextContent("Reactor Max Recv Messages", "Defines maximum amount of messages in the receive queue");
            m_ReactorMaximumSentMessagesLabel = TextUtility.TextContent("Reactor Max Sent Messages", "Defines maximum message count in sent queue");

            m_MaxBufferedPacketsLabel = TextUtility.TextContent("Max Buffered Packets", "The maximum number of packets that can be buffered by a NetworkConnection for each channel. This corresponds to the 'ChannelOption.MaxPendingBuffers' channel option.");
            m_AllowFragmentationLabel = TextUtility.TextContent("Packet Fragmentation", "Enable to allow NetworkConnection instances to fragment packets that are larger than the maxPacketSize, up to a maximum size of 64K.\n\nThis can cause delays when sending large packets.");
            m_UseWebSocketsLabel = TextUtility.TextContent("Use WebSockets", "This makes the server listen for connections using WebSockets. This allows WebGL clients to connect to the server.");
            m_UseSimulatorLabel = TextUtility.TextContent("Use Network Simulator", "This simulates network latency and packet loss on clients. Useful for testing under internet-like conditions");
            m_LatencyLabel = TextUtility.TextContent("Simulated Average Latency", "The amount of delay in milliseconds to add to network packets");
            m_PacketLossPercentageLabel = TextUtility.TextContent("Simulated Packet Loss", "The percentage of packets that should be dropped");
            m_MatchHostLabel = TextUtility.TextContent("MatchMaker Host URI", "The hostname of the matchmaking server.\n\nThe default is mm.unet.unity3d.com, which will connect a client to the nearest data center geographically.");
            m_MatchPortLabel = TextUtility.TextContent("MatchMaker Port", "The port of the matchmaking service.");
            m_MatchNameLabel = TextUtility.TextContent("Match Name", "The name that will be used when creating a match in MatchMaker.");
            m_MatchSizeLabel = TextUtility.TextContent("Maximum Match Size", "The maximum size for the match. This value is compared to the maximum size specified in the service configuration at multiplayer.unity3d.com and the lower of the two is enforced. It must be greater than 1. This is typically used to override the match size for various game modes.");
            m_NetworkAddressLabel = TextUtility.TextContent("Network Address", "The network address currently in use.");
            m_NetworkPortLabel = TextUtility.TextContent("Network Port", "The network port currently in use.");
            m_ServerBindToIPLabel = TextUtility.TextContent("Server Bind to IP", "Enable to bind the server to a specific IP address.");
            m_ServerBindAddressLabel = TextUtility.TextContent("Server Bind Address Label", "IP to bind the server to, when Server Bind to IP is enabled.");
            m_MaxDelayLabel = TextUtility.TextContent("Max Delay", "The maximum delay before sending packets on connections.");
            m_PlayerPrefabLabel = TextUtility.TextContent("Player Prefab", "The default prefab to be used to create player objects on the server.");
            m_AutoCreatePlayerLabel = TextUtility.TextContent("Auto Create Player", "Enable to automatically create player objects on connect and on Scene change.");
            m_PlayerSpawnMethodLabel = TextUtility.TextContent("Player Spawn Method", "How to determine which NetworkStartPosition to spawn players at, from all NetworkStartPositions in the Scene.\n\nRandom chooses a random NetworkStartPosition.\n\nRound Robin chooses the next NetworkStartPosition on a round-robin basis.");
            m_AdvancedConfigurationLabel = TextUtility.TextContent("Advanced Configuration", "Enable to view and edit advanced settings.");

            // top-level properties
            m_DontDestroyOnLoadProperty = serializedObject.FindProperty("m_DontDestroyOnLoad");
            m_RunInBackgroundProperty = serializedObject.FindProperty("m_RunInBackground");
            m_ScriptCRCCheckProperty = serializedObject.FindProperty("m_ScriptCRCCheck");
            m_LogLevelProperty = serializedObject.FindProperty("m_LogLevel");

            // network foldout properties
            m_NetworkAddressProperty = serializedObject.FindProperty("m_NetworkAddress");
            m_NetworkPortProperty = serializedObject.FindProperty("m_NetworkPort");
            m_ServerBindToIPProperty = serializedObject.FindProperty("m_ServerBindToIP");
            m_ServerBindAddressProperty = serializedObject.FindProperty("m_ServerBindAddress");
            m_MaxDelayProperty = serializedObject.FindProperty("m_MaxDelay");
            m_MaxBufferedPacketsProperty = serializedObject.FindProperty("m_MaxBufferedPackets");
            m_AllowFragmentationProperty = serializedObject.FindProperty("m_AllowFragmentation");
            m_MatchHostProperty =  serializedObject.FindProperty("m_MatchHost");
            m_MatchPortProperty =  serializedObject.FindProperty("m_MatchPort");
            m_MatchNameProperty =  serializedObject.FindProperty("matchName");
            m_MatchSizeProperty =  serializedObject.FindProperty("matchSize");

            // spawn foldout properties
            m_PlayerPrefabProperty = serializedObject.FindProperty("m_PlayerPrefab");
            m_AutoCreatePlayerProperty = serializedObject.FindProperty("m_AutoCreatePlayer");
            m_PlayerSpawnMethodProperty = serializedObject.FindProperty("m_PlayerSpawnMethod");
            m_SpawnListProperty = serializedObject.FindProperty("m_SpawnPrefabs");

            m_SpawnList = new ReorderableList(serializedObject, m_SpawnListProperty);
            m_SpawnList.drawHeaderCallback = DrawHeader;
            m_SpawnList.drawElementCallback = DrawChild;
            m_SpawnList.onReorderCallback = Changed;
            m_SpawnList.onRemoveCallback = RemoveButton;
            m_SpawnList.onChangedCallback = Changed;
            m_SpawnList.onReorderCallback = Changed;
            m_SpawnList.onAddCallback = AddButton;
            m_SpawnList.elementHeight = 16; // this uses a 16x16 icon. other sizes make it stretch.

            // network configuration
            m_CustomConfigProperty = serializedObject.FindProperty("m_CustomConfig");
            m_ChannelListProperty = serializedObject.FindProperty("m_Channels");
            m_ChannelList = new ReorderableList(serializedObject, m_ChannelListProperty);
            m_ChannelList.drawHeaderCallback = ChannelDrawHeader;
            m_ChannelList.drawElementCallback = ChannelDrawChild;
            m_ChannelList.onReorderCallback = ChannelChanged;
            m_ChannelList.onAddDropdownCallback = ChannelAddButton;
            m_ChannelList.onRemoveCallback = ChannelRemoveButton;
            m_ChannelList.onChangedCallback = ChannelChanged;
            m_ChannelList.onReorderCallback = ChannelChanged;
            m_ChannelList.onAddCallback = ChannelChanged;

            // Network Simulator
            m_UseWebSocketsProperty = serializedObject.FindProperty("m_UseWebSockets");
            m_UseSimulatorProperty = serializedObject.FindProperty("m_UseSimulator");
            m_SimulatedLatencyProperty = serializedObject.FindProperty("m_SimulatedLatency");
            m_PacketLossPercentageProperty = serializedObject.FindProperty("m_PacketLossPercentage");
        }

        static void ShowPropertySuffix(GUIContent content, SerializedProperty prop, string suffix)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(prop, content);
            GUILayout.Label(suffix, EditorStyles.miniLabel, GUILayout.Width(64));
            EditorGUILayout.EndHorizontal();
        }

        protected void ShowSimulatorInfo()
        {
            EditorGUILayout.PropertyField(m_UseSimulatorProperty, m_UseSimulatorLabel);

            if (m_UseSimulatorProperty.boolValue)
            {
                EditorGUI.indentLevel += 1;

                if (Application.isPlaying && m_NetworkManager.client != null)
                {
                    // read only at runtime
                    EditorGUILayout.LabelField(m_LatencyLabel, new GUIContent(m_NetworkManager.simulatedLatency + " milliseconds"));
                    EditorGUILayout.LabelField(m_PacketLossPercentageLabel, new GUIContent(m_NetworkManager.packetLossPercentage + "%"));
                }
                else
                {
                    // Latency
                    int oldLatency = m_NetworkManager.simulatedLatency;
                    EditorGUILayout.BeginHorizontal();
                    int newLatency = EditorGUILayout.IntSlider(m_LatencyLabel, oldLatency, 1, 400);
                    GUILayout.Label("millsec", EditorStyles.miniLabel, GUILayout.Width(64));
                    EditorGUILayout.EndHorizontal();
                    if (newLatency != oldLatency)
                    {
                        m_SimulatedLatencyProperty.intValue = newLatency;
                    }

                    // Packet Loss
                    float oldPacketLoss = m_NetworkManager.packetLossPercentage;
                    EditorGUILayout.BeginHorizontal();
                    float newPacketLoss = EditorGUILayout.Slider(m_PacketLossPercentageLabel, oldPacketLoss, 0f, 20f);
                    GUILayout.Label("%", EditorStyles.miniLabel, GUILayout.Width(64));
                    EditorGUILayout.EndHorizontal();
                    if (newPacketLoss != oldPacketLoss)
                    {
                        m_PacketLossPercentageProperty.floatValue = newPacketLoss;
                    }
                }

                EditorGUI.indentLevel -= 1;
            }
        }

        protected void ShowConfigInfo()
        {
            bool oldCustomConfig = m_NetworkManager.customConfig;
            EditorGUILayout.PropertyField(m_CustomConfigProperty, m_AdvancedConfigurationLabel);

            // Populate default channels first time a custom config is created.
            if (m_CustomConfigProperty.boolValue)
            {
                if (!oldCustomConfig)
                {
                    if (m_NetworkManager.channels.Count == 0)
                    {
                        m_NetworkManager.channels.Add(QosType.ReliableSequenced);
                        m_NetworkManager.channels.Add(QosType.Unreliable);
                        m_NetworkManager.customConfig = true;
                        m_CustomConfigProperty.serializedObject.Update();
                        m_ChannelList.serializedProperty.serializedObject.Update();
                    }
                }
            }

            if (m_NetworkManager.customConfig)
            {
                EditorGUI.indentLevel += 1;
                var maxConn = serializedObject.FindProperty("m_MaxConnections");
                ShowPropertySuffix(m_MaxConnectionsLabel, maxConn, "connections");

                m_ChannelList.DoLayoutList();

                maxConn.isExpanded = EditorGUILayout.Foldout(maxConn.isExpanded, "Timeouts");
                if (maxConn.isExpanded)
                {
                    EditorGUI.indentLevel += 1;
                    var minUpdateTimeout = serializedObject.FindProperty("m_ConnectionConfig.m_MinUpdateTimeout");
                    var connectTimeout = serializedObject.FindProperty("m_ConnectionConfig.m_ConnectTimeout");
                    var disconnectTimeout = serializedObject.FindProperty("m_ConnectionConfig.m_DisconnectTimeout");
                    var pingTimeout = serializedObject.FindProperty("m_ConnectionConfig.m_PingTimeout");

                    ShowPropertySuffix(m_MinUpdateTimeoutLabel, minUpdateTimeout, "millisec");
                    ShowPropertySuffix(m_ConnectTimeoutLabel, connectTimeout, "millisec");
                    ShowPropertySuffix(m_DisconnectTimeoutLabel, disconnectTimeout, "millisec");
                    ShowPropertySuffix(m_PingTimeoutLabel, pingTimeout, "millisec");
                    EditorGUI.indentLevel -= 1;
                }

                var threadAwakeTimeout = serializedObject.FindProperty("m_GlobalConfig.m_ThreadAwakeTimeout");
                threadAwakeTimeout.isExpanded = EditorGUILayout.Foldout(threadAwakeTimeout.isExpanded, "Global Config");
                if (threadAwakeTimeout.isExpanded)
                {
                    EditorGUI.indentLevel += 1;
                    var reactorModel = serializedObject.FindProperty("m_GlobalConfig.m_ReactorModel");
                    var reactorMaximumReceivedMessages = serializedObject.FindProperty("m_GlobalConfig.m_ReactorMaximumReceivedMessages");
                    var reactorMaximumSentMessages = serializedObject.FindProperty("m_GlobalConfig.m_ReactorMaximumSentMessages");

                    ShowPropertySuffix(m_ThreadAwakeTimeoutLabel, threadAwakeTimeout, "millisec");
                    EditorGUILayout.PropertyField(reactorModel, m_ReactorModelLabel);
                    ShowPropertySuffix(m_ReactorMaximumReceivedMessagesLabel, reactorMaximumReceivedMessages, "messages");
                    ShowPropertySuffix(m_ReactorMaximumSentMessagesLabel, reactorMaximumSentMessages, "messages");
                    EditorGUI.indentLevel -= 1;
                }

                EditorGUI.indentLevel -= 1;
            }
        }

        protected void ShowSpawnInfo()
        {
            m_PlayerPrefabProperty.isExpanded = EditorGUILayout.Foldout(m_PlayerPrefabProperty.isExpanded, m_ShowSpawnLabel);
            if (!m_PlayerPrefabProperty.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel += 1;

            //The NetworkLobbyManager doesnt use playerPrefab, it has its own player prefab slots, so dont show this
            if (!typeof(NetworkLobbyManager).IsAssignableFrom(m_NetworkManager.GetType()))
            {
                EditorGUILayout.PropertyField(m_PlayerPrefabProperty, m_PlayerPrefabLabel);
            }

            EditorGUILayout.PropertyField(m_AutoCreatePlayerProperty, m_AutoCreatePlayerLabel);
            EditorGUILayout.PropertyField(m_PlayerSpawnMethodProperty, m_PlayerSpawnMethodLabel);


            EditorGUI.BeginChangeCheck();
            m_SpawnList.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.indentLevel -= 1;
        }

        protected SceneAsset GetSceneObject(string sceneObjectName)
        {
            if (string.IsNullOrEmpty(sceneObjectName))
            {
                return null;
            }

            foreach (var editorScene in EditorBuildSettings.scenes)
            {
                var sceneNameWithoutExtension = Path.GetFileNameWithoutExtension(editorScene.path);
                if (sceneNameWithoutExtension == sceneObjectName)
                {
                    return AssetDatabase.LoadAssetAtPath(editorScene.path, typeof(SceneAsset)) as SceneAsset;
                }
            }
            if (LogFilter.logWarn) { Debug.LogWarning("Scene [" + sceneObjectName + "] cannot be used with networking. Add this scene to the 'Scenes in the Build' in build settings."); }
            return null;
        }

        protected void ShowNetworkInfo()
        {
            m_NetworkAddressProperty.isExpanded = EditorGUILayout.Foldout(m_NetworkAddressProperty.isExpanded, m_ShowNetworkLabel);
            if (!m_NetworkAddressProperty.isExpanded)
            {
                return;
            }
            EditorGUI.indentLevel += 1;

            if (EditorGUILayout.PropertyField(m_UseWebSocketsProperty, m_UseWebSocketsLabel))
            {
                NetworkServer.useWebSockets = m_NetworkManager.useWebSockets;
            }

            EditorGUILayout.PropertyField(m_NetworkAddressProperty, m_NetworkAddressLabel);
            EditorGUILayout.PropertyField(m_NetworkPortProperty, m_NetworkPortLabel);
            EditorGUILayout.PropertyField(m_ServerBindToIPProperty, m_ServerBindToIPLabel);
            if (m_NetworkManager.serverBindToIP)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(m_ServerBindAddressProperty, m_ServerBindAddressLabel);
                EditorGUI.indentLevel -= 1;
            }
            EditorGUILayout.PropertyField(m_ScriptCRCCheckProperty, m_ScriptCRCCheckLabel);
            EditorGUILayout.PropertyField(m_MaxDelayProperty, m_MaxDelayLabel);
            EditorGUILayout.PropertyField(m_MaxBufferedPacketsProperty, m_MaxBufferedPacketsLabel);
            EditorGUILayout.PropertyField(m_AllowFragmentationProperty, m_AllowFragmentationLabel);
            EditorGUILayout.PropertyField(m_MatchHostProperty, m_MatchHostLabel);
            EditorGUILayout.PropertyField(m_MatchPortProperty, m_MatchPortLabel);
            EditorGUILayout.PropertyField(m_MatchNameProperty, m_MatchNameLabel);
            EditorGUILayout.PropertyField(m_MatchSizeProperty, m_MatchSizeLabel);

            EditorGUI.indentLevel -= 1;
        }

        protected void ShowScenes()
        {
            var offlineObj = GetSceneObject(m_NetworkManager.offlineScene);
            var newOfflineScene = EditorGUILayout.ObjectField(m_OfflineSceneLabel, offlineObj, typeof(SceneAsset), false);
            if (newOfflineScene == null)
            {
                var prop = serializedObject.FindProperty("m_OfflineScene");
                prop.stringValue = "";
                EditorUtility.SetDirty(target);
            }
            else
            {
                if (newOfflineScene.name != m_NetworkManager.offlineScene)
                {
                    var sceneObj = GetSceneObject(newOfflineScene.name);
                    if (sceneObj == null)
                    {
                        Debug.LogWarning("The scene " + newOfflineScene.name + " cannot be used. To use this scene add it to the build settings for the project");
                    }
                    else
                    {
                        var prop = serializedObject.FindProperty("m_OfflineScene");
                        prop.stringValue = newOfflineScene.name;
                        EditorUtility.SetDirty(target);
                    }
                }
            }

            var onlineObj = GetSceneObject(m_NetworkManager.onlineScene);
            var newOnlineScene = EditorGUILayout.ObjectField(m_OnlineSceneLabel, onlineObj, typeof(SceneAsset), false);
            if (newOnlineScene == null)
            {
                var prop = serializedObject.FindProperty("m_OnlineScene");
                prop.stringValue = "";
                EditorUtility.SetDirty(target);
            }
            else
            {
                if (newOnlineScene.name != m_NetworkManager.onlineScene)
                {
                    var sceneObj = GetSceneObject(newOnlineScene.name);
                    if (sceneObj == null)
                    {
                        Debug.LogWarning("The scene " + newOnlineScene.name + " cannot be used. To use this scene add it to the build settings for the project");
                    }
                    else
                    {
                        var prop = serializedObject.FindProperty("m_OnlineScene");
                        prop.stringValue = newOnlineScene.name;
                        EditorUtility.SetDirty(target);
                    }
                }
            }
        }

        protected void ShowDerivedProperties(Type baseType, Type superType)
        {
            bool first = true;

            SerializedProperty property = serializedObject.GetIterator();
            bool expanded = true;
            while (property.NextVisible(expanded))
            {
                // ignore properties from base class.
                var f = baseType.GetField(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var p = baseType.GetProperty(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (f == null && superType != null)
                {
                    f = superType.GetField(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }

                if (p == null && superType != null)
                {
                    p = superType.GetProperty(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }

                if (f == null && p == null)
                {
                    if (first)
                    {
                        first = false;
                        EditorGUI.BeginChangeCheck();
                        serializedObject.Update();

                        EditorGUILayout.Separator();
                    }
                    EditorGUILayout.PropertyField(property, true);
                    expanded = false;
                }
            }
            if (!first)
            {
                serializedObject.ApplyModifiedProperties();
                EditorGUI.EndChangeCheck();
            }
        }

        public override void OnInspectorGUI()
        {
            if (m_DontDestroyOnLoadProperty == null || m_DontDestroyOnLoadLabel == null)
                m_Initialized = false;

            Init();

            serializedObject.Update();
            EditorGUILayout.PropertyField(m_DontDestroyOnLoadProperty, m_DontDestroyOnLoadLabel);
            EditorGUILayout.PropertyField(m_RunInBackgroundProperty , m_RunInBackgroundLabel);

            if (EditorGUILayout.PropertyField(m_LogLevelProperty))
            {
                LogFilter.currentLogLevel = (int)m_NetworkManager.logLevel;
            }

            ShowScenes();
            ShowNetworkInfo();
            ShowSpawnInfo();
            ShowConfigInfo();
            ShowSimulatorInfo();
            serializedObject.ApplyModifiedProperties();

            ShowDerivedProperties(typeof(NetworkManager), null);
        }

        static void DrawHeader(Rect headerRect)
        {
            GUI.Label(headerRect, "Registered Spawnable Prefabs:");
        }

        internal void DrawChild(Rect r, int index, bool isActive, bool isFocused)
        {
            SerializedProperty prefab = m_SpawnListProperty.GetArrayElementAtIndex(index);
            GameObject go = (GameObject)prefab.objectReferenceValue;

            GUIContent label;
            if (go == null)
            {
                label = TextUtility.TextContent("Empty", "Drag a prefab with a NetworkIdentity here");
            }
            else
            {
                var uv = go.GetComponent<NetworkIdentity>();
                if (uv != null)
                {
                    label = new GUIContent(go.name, "AssetId: [" + uv.assetId + "]");
                }
                else
                {
                    label = new GUIContent(go.name, "No Network Identity");
                }
            }

            var newGameObject = (GameObject)EditorGUI.ObjectField(r, label, go, typeof(GameObject), false);

            if (newGameObject != go)
            {
                if (newGameObject != null && !newGameObject.GetComponent<NetworkIdentity>())
                {
                    if (LogFilter.logError) { Debug.LogError("Prefab " + newGameObject + " cannot be added as spawnable as it doesn't have a NetworkIdentity."); }
                    return;
                }
                prefab.objectReferenceValue = newGameObject;
            }
        }

        internal void Changed(ReorderableList list)
        {
            EditorUtility.SetDirty(target);
        }

        internal void AddButton(ReorderableList list)
        {
            m_SpawnListProperty.arraySize += 1;
            list.index = m_SpawnListProperty.arraySize - 1;

            var obj = m_SpawnListProperty.GetArrayElementAtIndex(m_SpawnListProperty.arraySize - 1);
            if (obj.objectReferenceValue != null)
                obj.objectReferenceValue = null;

            m_SpawnList.index = m_SpawnList.count - 1;

            Changed(list);
        }

        internal void RemoveButton(ReorderableList list)
        {
            m_SpawnListProperty.DeleteArrayElementAtIndex(m_SpawnList.index);
            if (list.index >= m_SpawnListProperty.arraySize)
            {
                list.index = m_SpawnListProperty.arraySize - 1;
            }
        }

        // List widget functions

        static void ChannelDrawHeader(Rect headerRect)
        {
            GUI.Label(headerRect, "Qos Channels:");
        }

        internal void ChannelDrawChild(Rect r, int index, bool isActive, bool isFocused)
        {
            QosType qos = (QosType)m_ChannelListProperty.GetArrayElementAtIndex(index).enumValueIndex;
            QosType newValue = (QosType)EditorGUI.EnumPopup(r, "Channel #" + index, qos);
            if (newValue != qos)
            {
                var obj = m_ChannelListProperty.GetArrayElementAtIndex(index);
                obj.enumValueIndex = (int)newValue;
            }
        }

        internal void ChannelChanged(ReorderableList list)
        {
            EditorUtility.SetDirty(target);
        }

        internal void ChannelAddButton(Rect rect, ReorderableList list)
        {
            m_ChannelListProperty.arraySize += 1;
            var obj = m_ChannelListProperty.GetArrayElementAtIndex(m_ChannelListProperty.arraySize - 1);
            obj.enumValueIndex = (int)QosType.ReliableSequenced;
            list.index = m_ChannelListProperty.arraySize - 1;
        }

        internal void ChannelRemoveButton(ReorderableList list)
        {
            if (m_NetworkManager.channels.Count == 1)
            {
                if (LogFilter.logError) { Debug.LogError("Cannot remove channel. There must be at least one QoS channel."); }
                return;
            }
            m_ChannelListProperty.DeleteArrayElementAtIndex(m_ChannelList.index);
            if (list.index >= m_ChannelListProperty.arraySize - 1)
            {
                list.index = m_ChannelListProperty.arraySize - 1;
            }
        }
    }
}
#endif //ENABLE_UNET
