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
        NetworkObject ZEDTrackerPrefab;
        [SerializeField]
        NetworkObject ServerPrefab;
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
                    { Role.ZEDTracker, ZEDTrackerPrefab },
                    { Role.Server, ServerPrefab },
                    { Role.Tablet, TabletPrefab }
                };

            var player = Instantiate(rolesToPlayers[role]);
            player.SpawnWithOwnership(clientId);

            Destroy(gameObject);
        }
    }
}
