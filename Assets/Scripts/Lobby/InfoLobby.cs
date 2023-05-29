using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Services.Lobbies.Models;

public class InfoLobby : MonoBehaviour
{
    public TextMeshProUGUI lobbyName;
    public TextMeshProUGUI lobbyCode;
    private Lobby lobbyJoined;

    public static InfoLobby Instance { get; private set; }

    void Start()
    {
        Instance = this;
      
    }

    public void ShowInfo(string name, string code)
    {
       
        lobbyName.text = "Lobby Name: "+name;
        lobbyCode.text = "Lobby Code: " + code;

    }

}
