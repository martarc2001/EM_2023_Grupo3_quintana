using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreateUI : MonoBehaviour
{
    //[SerializeField] private GameObject lobby;
    //private LobbyManager lobbyManager;
    [SerializeField] private Button publicLobbyButton;
    [SerializeField] private Button privateLobbyButton;
    [SerializeField] private TMP_InputField lobbyName;
    [SerializeField] private GameObject infoLobby;


    private void Awake()
    {

        //lobbyManager = lobby.GetComponent<LobbyManager>();
        publicLobbyButton.onClick.AddListener(()=> {
            LobbyManager.Instance.CreateLobby(lobbyName.text,false);          
            gameObject.SetActive(false);
           
        });

        privateLobbyButton.onClick.AddListener(()=> {
            LobbyManager.Instance.CreateLobby(lobbyName.text, true);
            infoLobby.SetActive(true);
            gameObject.SetActive(false);
            


        });
    }

  
}
