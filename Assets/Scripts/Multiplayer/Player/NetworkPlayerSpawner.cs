using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace Multiplayer
{
    public class NetworkPlayerSpawner : NetworkBehaviour
    {
        [SerializeField]
        NetworkObject HMDPrefab;
        [SerializeField]
        NetworkObject SerTrackerPrefab;
        [SerializeField]
        NetworkObject ZEDPrefab;
        [SerializeField]
        NetworkObject TabletPrefab;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                SpawnPlayerRpc(
                    (int)NetworkRoleManager.Instance.localRole,
                    NetworkManager.Singleton.LocalClientId
                );
            }
        }

        [Rpc(SendTo.Server)]
        void SpawnPlayerRpc(int roleId, ulong clientId)
        {
            var role = (Role)roleId;
            var rolesToPlayers = new Dictionary<Role, NetworkObject>
                {
                    { Role.HMD, HMDPrefab },
                    { Role.SerTracker, SerTrackerPrefab },
                    { Role.ZED, ZEDPrefab },
                    { Role.Tablet, TabletPrefab }
                };

            var player = Instantiate(rolesToPlayers[role]);
            player.SpawnWithOwnership(clientId);

            Destroy(gameObject);
        }
    }
}
