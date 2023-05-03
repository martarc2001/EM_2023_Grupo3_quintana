using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerAttributes : NetworkBehaviour
{
    //Vida
    const int MAX_HP = 100;
    NetworkVariable<int> HP = new NetworkVariable<int>(MAX_HP);
    [SerializeField] private GameObject healthBar;

    //Nombre y sprite  
    string playerName;
    [SerializeField] private GameObject AkaiKazePrefab;
    [SerializeField] private GameObject OniPrefab;
    [SerializeField] private GameObject HuntressPrefab;



    void Start()
    {
        //El owner del objeto avisa al servidor del nombre que ha escogido
        if (IsOwner)
        {
            string nameInInputText = GameObject.Find("UI").GetComponent<UIHandler>().playerName;
            ChangeInitialSettingsServerRpc(nameInInputText);
            GetSettingsFromPreviousPlayersServerRpc();

        }
    }


    [ServerRpc]
    void ChangeInitialSettingsServerRpc(string nameInInputText)
    {
        playerName = nameInInputText;    
        
        
        ChangeInitialSettingsClientRpc(playerName);        
    }


    [ClientRpc]
    void ChangeInitialSettingsClientRpc(string playerName)
    {
        transform.GetChild(0).Find("HUD").Find("Name").Find("HealthBar").GetComponent<TextMeshPro>().text = playerName; //Changing the name on prefab only
        
    }


    [ServerRpc]
    void GetSettingsFromPreviousPlayersServerRpc() 
    {
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList) //Por cada cliente, coge su respectivo Player Attributes para poder asociar sus variables a animator y nombre
        {
            
            string name = client.PlayerObject.GetComponentInChildren<PlayerAttributes>().playerName;
            client.PlayerObject.GetComponentInChildren<PlayerAttributes>().ChangeInitialSettingsClientRpc(name);
        }
    }




}






