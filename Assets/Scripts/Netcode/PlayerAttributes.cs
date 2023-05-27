using Cinemachine;
using Netcode;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAttributes : NetworkBehaviour
{

    //Nombre y sprite  
    string playerName;
    [SerializeField] private GameObject AkaiKazePrefab;
    [SerializeField] private GameObject OniPrefab;
    [SerializeField] private GameObject HuntressPrefab;

    //HUD Interface Colors
    Color red = new Color(1, 0, 0, 0.35f);
    Color green = new Color(0, 1, 0, 0.35f);
    Color blue = new Color(0, 0, 1, 0.35f);


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
        transform.GetChild(0).Find("HUD").Find("Name").GetComponent<TextMeshPro>().text = playerName; //Changing the name on prefab only

    }

    [ClientRpc]
    void ChangeInitialSettingsClientRpc(string playerName, int thisClientID)
    {
        //Changing the name on prefab
        transform.GetChild(0).Find("HUD").Find("Name").GetComponent<TextMeshPro>().text = playerName;


        //Changing name and interface appearance
        var otherPlayerInterface = GameObject.Find("Canvas - HUD").transform.GetChild(thisClientID);
        string otherPlayerSelectedSkin = transform.GetChild(0).gameObject.name.Replace("(Clone)", ""); //When instancing the prefab it shows up as "Huntress(Clone)", removing "(Clone)" for it to be easier to read
        otherPlayerInterface.gameObject.SetActive(true);
        otherPlayerInterface.Find("Disconnected").gameObject.SetActive(false);//Deactivating it in case someone in that position previously disconnected
        otherPlayerInterface.transform.Find("Name").GetComponent<TMPro.TextMeshProUGUI>().text = playerName;

        switch (otherPlayerSelectedSkin)
        {
            case "Huntress":
                otherPlayerInterface.transform.Find("BG").gameObject.GetComponent<Image>().color = green;
                otherPlayerInterface.transform.Find("Sprite").gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("Huntress_HUD");
                break;
            case "Oni":
                otherPlayerInterface.transform.Find("BG").gameObject.GetComponent<Image>().color = blue;
                otherPlayerInterface.transform.Find("Sprite").gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("Oni_HUD");
                break;
            default://"AkaiKaze"
                otherPlayerInterface.transform.Find("BG").gameObject.GetComponent<Image>().color = red;
                otherPlayerInterface.transform.Find("Sprite").gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("AkaiKaze_HUD");
                break;
        }

    }


    [ServerRpc]
    void GetSettingsFromPreviousPlayersServerRpc()
    {
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList) //Por cada cliente, coge su respectivo Player Attributes para poder asociar sus variables a animator y nombre
        {
            string name = client.PlayerObject.GetComponentInChildren<PlayerAttributes>().playerName;
            int playerNum = client.PlayerObject.GetComponent<PlayerNetworkConfig>().playerNum.Value;
            client.PlayerObject.GetComponentInChildren<PlayerAttributes>().ChangeInitialSettingsClientRpc(name, playerNum);
        }
    }





}






