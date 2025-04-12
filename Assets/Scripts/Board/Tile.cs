using Unity.Netcode;
using UnityEngine;

public class Tile : NetworkBehaviour
{
    [Rpc(SendTo.Everyone)]
    public void SetupRpc(Vector3 pos, Vector3 rot, string tileID, bool freeze)
    {
        transform.position = pos;
        transform.rotation = Quaternion.Euler(rot);
        gameObject.name = tileID;
        gameObject.GetComponent<Rigidbody>().constraints = freeze ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
    }
}
