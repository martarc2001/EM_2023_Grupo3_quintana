using Netcode;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyWaiting : MonoBehaviour
{

   
    [SerializeField] public TextMeshProUGUI waitingText;
    [SerializeField] public Button readyButton;
    [SerializeField] public GameObject gameWillStart;
  
    

    public static LobbyWaiting Instance { get; private set; }
    void Awake()
    {
     
      
        Instance = this;
        readyButton.onClick.AddListener(() => {

          
             
            ConnectedPlayers.Instance.ShowReadyPlayersServerRpc(); });
        readyButton.onClick.AddListener(() => readyButton.gameObject.SetActive(false));
    }

 
}
