using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ConnectedPlayers : NetworkBehaviour
{
    public NetworkVariable<int> alivePlayers;

    // Start is called before the first frame update
    void Start()
    {
        
    }
    private void Awake()
    {
        alivePlayers = new NetworkVariable<int>(0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
