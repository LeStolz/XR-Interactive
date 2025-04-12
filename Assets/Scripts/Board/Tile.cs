using Unity.Netcode;
using UnityEngine;

public class Tile : NetworkBehaviour
{
    [Rpc(SendTo.Everyone)]
    public void SetTileIDRpc(string tileID)
    {
        gameObject.name = tileID;
    }

    [Rpc(SendTo.Owner)]
    public void SetTileConstraintsRpc(bool freeze)
    {
        if (freeze)
        {
            gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        }
        else
        {
            gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        }
    }
}
