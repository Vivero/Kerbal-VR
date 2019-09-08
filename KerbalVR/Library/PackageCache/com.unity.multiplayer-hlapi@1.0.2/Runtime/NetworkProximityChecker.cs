using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Networking
{
    /// <summary>
    /// Component that controls visibility of networked objects for players.
    /// <para>Any object with this component on it will not be visible to players more than a (configurable) distance away.</para>
    /// </summary>
    [AddComponentMenu("Network/NetworkProximityChecker")]
    [RequireComponent(typeof(NetworkIdentity))]
    [Obsolete("The high level API classes are deprecated and will be removed in the future.")]
    public class NetworkProximityChecker : NetworkBehaviour
    {
        /// <summary>
        /// Enumeration of methods to use to check proximity.
        /// </summary>
        public enum CheckMethod
        {
            /// <summary>
            /// Use 3D physics to determine proximity.
            /// </summary>
            Physics3D,
            /// <summary>
            /// Use 2D physics to determine proximity.
            /// </summary>
            Physics2D
        };

        /// <summary>
        /// The maximim range that objects will be visible at.
        /// </summary>
        [TooltipAttribute("The maximum range that objects will be visible at.")]
        public int visRange = 10;

        /// <summary>
        /// How often (in seconds) that this object should update the set of players that can see it.
        /// </summary>
        [TooltipAttribute("How often (in seconds) that this object should update the set of players that can see it.")]
        public float visUpdateInterval = 1.0f; // in seconds

        /// <summary>
        /// Which method to use for checking proximity of players.
        /// </summary>
        [TooltipAttribute("Which method to use for checking proximity of players.\n\nPhysics3D uses 3D physics to determine proximity.\n\nPhysics2D uses 2D physics to determine proximity.")]
        public CheckMethod checkMethod = CheckMethod.Physics3D;

        /// <summary>
        /// Flag to force this object to be hidden for players.
        /// <para>If this object is a player object, it will not be hidden for that player.</para>
        /// </summary>
        [TooltipAttribute("Enable to force this object to be hidden from players.")]
        public bool forceHidden = false;

        float m_VisUpdateTime;

        void Update()
        {
            if (!NetworkServer.active)
                return;

            if (Time.time - m_VisUpdateTime > visUpdateInterval)
            {
                GetComponent<NetworkIdentity>().RebuildObservers(false);
                m_VisUpdateTime = Time.time;
            }
        }

        // called when a new player enters
        public override bool OnCheckObserver(NetworkConnection newObserver)
        {
            if (forceHidden)
                return false;

            // this cant use newObserver.playerControllers[0]. must iterate to find a valid player.
            GameObject player = null;
            for (int i = 0; i < newObserver.playerControllers.Count; i++)
            {
                var p = newObserver.playerControllers[i];
                if (p != null && p.gameObject != null)
                {
                    player = p.gameObject;
                    break;
                }
            }
            if (player == null)
                return false;

            var pos = player.transform.position;
            return (pos - transform.position).magnitude < visRange;
        }

        public override bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initial)
        {
            if (forceHidden)
            {
                // ensure player can still see themself
                var uv = GetComponent<NetworkIdentity>();
                if (uv.connectionToClient != null)
                {
                    observers.Add(uv.connectionToClient);
                }
                return true;
            }

            // find players within range
            switch (checkMethod)
            {
                case CheckMethod.Physics3D:
                {
                    var hits = Physics.OverlapSphere(transform.position, visRange);
                    for (int i = 0; i < hits.Length; i++)
                    {
                        var hit = hits[i];
                        // (if an object has a connectionToClient, it is a player)
                        var uv = hit.GetComponent<NetworkIdentity>();
                        if (uv != null && uv.connectionToClient != null)
                        {
                            observers.Add(uv.connectionToClient);
                        }
                    }
                    return true;
                }

                case CheckMethod.Physics2D:
                {
                    var hits = Physics2D.OverlapCircleAll(transform.position, visRange);
                    for (int i = 0; i < hits.Length; i++)
                    {
                        var hit = hits[i];
                        // (if an object has a connectionToClient, it is a player)
                        var uv = hit.GetComponent<NetworkIdentity>();
                        if (uv != null && uv.connectionToClient != null)
                        {
                            observers.Add(uv.connectionToClient);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        // called hiding and showing objects on the host
        public override void OnSetLocalVisibility(bool vis)
        {
            SetVis(gameObject, vis);
        }

        static void SetVis(GameObject go, bool vis)
        {
            foreach (var r in go.GetComponents<Renderer>())
            {
                r.enabled = vis;
            }
            for (int i = 0; i < go.transform.childCount; i++)
            {
                var t = go.transform.GetChild(i);
                SetVis(t.gameObject, vis);
            }
        }
    }
}
