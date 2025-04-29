using UnityEngine;

namespace Main
{
    class TabletManager : NetworkPlayer
    {
        [SerializeField]
        GameObject cam;
        [SerializeField]
        GameObject UI;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                NetworkGameManager.Instance.MRInteractionSetup.SetActive(false);
                NetworkGameManager.Instance.TableUI.SetActive(false);
                cam.SetActive(true);
                UI.SetActive(true);
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
                UI.SetActive(false);
            }
        }

        public void Toggle2ndRow(bool visible)
        {
            if (IsOwner)
            {
                foreach (Transform transform in BoardGameManager.Instance.transform)
                {
                    if (
                        transform.TryGetComponent(out Tile tile) &&
                        transform.position.y >
                            BoardGameManager.Instance.AnswerBoardOrigin.transform.position.y +
                            transform.localScale.y
                    )
                    {
                        tile.ToggleVisibility(visible);
                    }
                }
            }
        }
    }
}