using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
   
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button quickJoinButton;
    [SerializeField]public GameObject lobbyUI;
    [SerializeField] public GameObject createLobbyUI;
    [SerializeField] public GameObject joinLobbyUI;

    private void Awake()
    {
       

        createLobbyButton.onClick.AddListener(() => {
            lobbyUI.SetActive(false);
            createLobbyUI.SetActive(true);
        });

        quickJoinButton.onClick.AddListener(()=> {
          
            lobbyUI.SetActive(false);
            joinLobbyUI.SetActive(true);

        });
    }
}
