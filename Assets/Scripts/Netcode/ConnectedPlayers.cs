using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ConnectedPlayers : NetworkBehaviour
{
    public NetworkVariable<int> alivePlayers;

    public GameObject imgGanar;
    public GameObject imgPerder;
    void Start()
    {
        
    }
    private void Awake()
    {
        imgPerder = GameObject.Find("NewCanvas6");
        imgGanar = GameObject.Find("NewCanvasganado");

        imgGanar.SetActive(false);
        imgPerder.SetActive(false);
        alivePlayers = new NetworkVariable<int>(0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void showGanar()
    {
        imgGanar.SetActive(true);
    }
    public void showPerder()
    {
        imgPerder.SetActive(true);
    }
}
