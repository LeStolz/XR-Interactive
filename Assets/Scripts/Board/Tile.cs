using Unity.Netcode;
using UnityEngine;

public class Tile : NetworkBehaviour
{
    [Rpc(SendTo.Everyone)]
    public void SetupRpc(Vector3 pos, Vector3 rot, string tileID, bool freeze)
    {
        gameObject.GetComponent<Rigidbody>().constraints = freeze ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
        transform.SetPositionAndRotation(pos, Quaternion.Euler(rot));
        gameObject.name = tileID;
    }
}
