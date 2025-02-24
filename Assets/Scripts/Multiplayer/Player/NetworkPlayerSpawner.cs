using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace Multiplayer
{
    public class NetworkPlayerSpawner : NetworkBehaviour
    {
        [SerializeField]
        NetworkPlayer HMDPrefab;
        [SerializeField]
        NetworkPlayer ZEDTrackerPrefab;
        [SerializeField]
        NetworkPlayer ServerPrefab;
        [SerializeField]
        NetworkPlayer TabletPrefab;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                var rolesToPlayers = new Dictionary<Role, NetworkPlayer>
                {
                    { Role.HMD, HMDPrefab },
                    { Role.ZEDTracker, ZEDTrackerPrefab },
                    { Role.Server, ServerPrefab },
                    { Role.Tablet, TabletPrefab }
                };

                var player = Instantiate(rolesToPlayers[NetworkRoleManager.Instance.localRole]);
                player.GetComponent<NetworkObject>().Spawn();
            }

            NetworkObject.Despawn(true);
        }
    }
}
