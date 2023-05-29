using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreateUI : MonoBehaviour
{
  
    [SerializeField] private Button publicLobbyButton;
    [SerializeField] private Button privateLobbyButton;
    [SerializeField] private TMP_InputField lobbyName;
    [SerializeField] private GameObject infoLobby;

    [SerializeField] private GameObject returnLobbyUI;
    [SerializeField] private Button returnLobbyButton;

    

private void Awake()
    {

        publicLobbyButton.onClick.AddListener(()=> {
            LobbyManager.Instance.CreateLobby(lobbyName.text,false);          
            gameObject.SetActive(false);
           
        });

        privateLobbyButton.onClick.AddListener(()=> {
            LobbyManager.Instance.CreateLobby(lobbyName.text, true);
            infoLobby.SetActive(true);
            gameObject.SetActive(false);
            


        });

        returnLobbyButton.onClick.AddListener(() => {
            returnLobbyUI.SetActive(true);
            gameObject.SetActive(false);
        });
    }

    public void ChangeNumPlayers(int value)
    {
        if (value == 0)
        {
            LobbyManager.Instance.maxPlayers = 2;
        }
        if (value == 1)
        {
            LobbyManager.Instance.maxPlayers = 3;
        }
        if (value == 2)
        {
            LobbyManager.Instance.maxPlayers = 4;
        }
    }

  
}
