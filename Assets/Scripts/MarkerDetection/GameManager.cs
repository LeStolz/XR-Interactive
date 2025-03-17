using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public partial class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance = null;

    [Header("User")]
    public bool isAudience;
    public Player playerManager;

    [Header("Hololens")]
    public Transform ARPlaySpace;
    public GameObject ARCamera;
    public GameObject VirtualTrackingCamera;
    public GameObject playerPrefab;
    public float trackMarkerDuration = 3000f;
    public GameObject OpenCVMarkerTrackingModule;
    public GameObject OpenCVMarker;
    public bool viewAssist;

    private bool init = false;
    private void Update()
    {
        UpdateHololens();
        
    }

}