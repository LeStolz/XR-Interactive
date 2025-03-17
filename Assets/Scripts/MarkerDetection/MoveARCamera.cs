using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MoveARCamera : MonoBehaviour
{
    public Transform ARCamera;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<PhotonView>().IsMine)
        {
            transform.position = ARCamera.position;
            transform.eulerAngles = ARCamera.eulerAngles;
        }

    }
}
