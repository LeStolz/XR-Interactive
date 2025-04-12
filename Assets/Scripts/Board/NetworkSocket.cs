using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Main
{
    class NetworkSocket : XRSocketInteractor
    {
        GameObject socket;

        protected override void Awake()
        {
            base.Awake();
            transform.localScale = BoardGameManager.Instance.TilePrefabs[0].transform.localScale;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (socket != null)
            {
                Destroy(socket);
                socket = null;
            }
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactableObject == null)
            {
                return;
            }

            base.OnSelectEntered(args);
            AttachTileToSocket(args.interactableObject);

            socket = Instantiate(
                BoardGameManager.Instance.SocketPrefab,
                transform.position + Vector3.up * transform.localScale.x,
                Quaternion.identity
            );

            StartCoroutine(AttachTileToSocketIE(args.interactableObject.transform.gameObject));
        }

        IEnumerator AttachTileToSocketIE(GameObject tile)
        {
            yield return new WaitUntil(
                () => Vector3.Distance(transform.position, tile.transform.position) < 0.1f,
                new TimeSpan(0, 0, 1), () => Debug.Log("Timed out")
            );

            BoardGameManager.Instance.AttachTileToSocketRpc(tile.name);
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            if (args.interactableObject == null)
            {
                return;
            }

            base.OnSelectExited(args);

            transform.localRotation = Quaternion.identity;

            if (socket != null)
            {
                Destroy(socket);
                socket = null;
            }

            BoardGameManager.Instance.DetachTileFromSocket(args.interactableObject.transform.gameObject.name);
        }

        void AttachTileToSocket(IXRSelectInteractable interactableObject)
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