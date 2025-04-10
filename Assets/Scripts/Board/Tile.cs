using Unity.Netcode;

public class Tile : NetworkBehaviour
{
    [Rpc(SendTo.Everyone)]
    public void SetTileIDRpc(string tileID)
    {
        gameObject.name = tileID;
    }
}
