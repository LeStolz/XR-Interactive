using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Main
{
    class Socket : NetworkBehaviour
    {
        GameObject socket;

        void Awake()
        {
            transform.localScale = BoardGameManager.Instance.TilePrefabs[0].transform.localScale;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (socket != null)
            {
                if (socket.GetComponent<NetworkObject>().IsSpawned)
                {
                    socket.GetComponent<NetworkObject>().Despawn(true);
                }
                else
                {
                    Destroy(socket);
                }
                socket = null;
            }
        }

        public void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactableObject == null)
            {
                return;
            }

            AttachTileToSocket(args.interactableObject);

            if (IsServer)
            {
                socket = Instantiate(
                    BoardGameManager.Instance.SocketPrefab,
                    transform.position + Vector3.up * transform.localScale.x,
                    Quaternion.identity
                );
                socket.GetComponent<NetworkObject>().Spawn(true);

                StartCoroutine(AttachTileToSocketIE(args.interactableObject.transform.gameObject));
            }
        }

        IEnumerator AttachTileToSocketIE(GameObject tile)
        {
            yield return new WaitUntil(
                () => Vector3.Distance(transform.position, tile.transform.position) < 0.1f,
                new TimeSpan(0, 0, 1), () => Debug.Log("Timed out")
            );

            BoardGameManager.Instance.AttachTileToSocket(tile);
        }

        public void OnSelectExited(SelectExitEventArgs args)
        {
            if (args.interactableObject == null)
            {
                return;
            }

            transform.localRotation = Quaternion.identity;

            if (IsServer)
            {
                if (socket != null)
                {
                    socket.GetComponent<NetworkObject>().Despawn(true);
                    socket = null;
                }

                BoardGameManager.Instance.DetachTileFromSocket(args.interactableObject.transform.gameObject);
            }
        }

        private void AttachTileToSocket(IXRSelectInteractable interactableObject)
        {
            Vector3 eulerAngles = interactableObject.transform.eulerAngles;

            eulerAngles.x = Mathf.Round(eulerAngles.x / 90f) * 90f;
            eulerAngles.y = Mathf.Round(eulerAngles.y / 90f) * 90f;
            eulerAngles.z = Mathf.Round(eulerAngles.z / 90f) * 90f;

            Quaternion normalizedRotation = Quaternion.Euler(eulerAngles);
            transform.localRotation = normalizedRotation;
        }
    }
}