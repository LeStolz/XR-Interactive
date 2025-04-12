using Unity.Netcode;
using UnityEngine;

public class Tile : NetworkBehaviour
{
    [Rpc(SendTo.Owner)]
    public void SetupRpc(Vector3 pos, Vector3 rot, string tileID, bool freeze)
    {
        transform.SetPositionAndRotation(pos, Quaternion.Euler(rot));
        gameObject.GetComponent<Rigidbody>().constraints = freeze ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
        gameObject.name = tileID;
    }
}
