using UnityEngine;

namespace Main
{
    class TabletManager : NetworkPlayer
    {
        [SerializeField]
        GameObject cam;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                NetworkGameManager.Instance.MRInteractionSetup.SetActive(false);
                NetworkGameManager.Instance.TableUI.SetActive(false);
                cam.SetActive(true);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsOwner)
            {
                NetworkGameManager.Instance.MRInteractionSetup.SetActive(true);
                NetworkGameManager.Instance.TableUI.SetActive(true);
                cam.SetActive(false);
            }
        }
    }
}