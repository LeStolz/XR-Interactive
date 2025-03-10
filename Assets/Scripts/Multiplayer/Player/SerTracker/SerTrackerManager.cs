
using UnityEngine;

namespace Multiplayer
{
    class SerTrackerManager : NetworkPlayer
    {
        [SerializeField]
        GameObject[] objectsToEnableOnSpawn;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();


            if (IsOwner)
            {
                NetworkRoleManager.Instance.MRInteractionSetup.SetActive(false);

                foreach (var obj in objectsToEnableOnSpawn)
                {
                    if (obj.TryGetComponent(out Camera camera))
                    {
                        camera.enabled = true;
                    }
                    else
                    {
                        obj.SetActive(true);
                    }
                }

                NetworkRoleManager.Instance.TableUI.SetActive(false);
            }
        }
    }
}