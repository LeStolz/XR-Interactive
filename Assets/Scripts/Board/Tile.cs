using Unity.Netcode;
using UnityEngine;

public class Tile : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        var rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.None;
    }

    [Rpc(SendTo.Everyone)]
    public void SetTileIDRpc(string tileID)
    {
        gameObject.name = tileID;
    }
}
