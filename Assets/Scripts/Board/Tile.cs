using Unity.Netcode;
using UnityEngine;

public class Tile : NetworkBehaviour
{
    Vector3 startPos;
    Vector3 startRot;

    [Rpc(SendTo.Everyone)]
    public void SetupRpc(Vector3 pos, Vector3 rot, string tileID, bool freeze)
    {
        transform.SetPositionAndRotation(pos, Quaternion.Euler(rot));
        gameObject.GetComponent<Rigidbody>().constraints = freeze ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
        gameObject.name = tileID;
        startPos = pos;
        startRot = rot;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            transform.position = startPos;
            transform.rotation = Quaternion.Euler(startRot);
        }
    }
}
