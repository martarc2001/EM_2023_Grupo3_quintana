using Cinemachine;
using Netcode;
using System;
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
    public string playerName;
    [SerializeField] private GameObject AkaiKazePrefab;
    [SerializeField] private GameObject OniPrefab;
    [SerializeField] private GameObject HuntressPrefab;
    public string charaSkin;
    //HUD Interface Colors
    Color red = new Color(1, 0, 0, 0.35f);
    Color green = new Color(0, 1, 0, 0.35f);
    Color blue = new Color(0, 0, 1, 0.35f);
    Color white = new Color(1, 1, 1, 1);

    void Start()
    {
        //Owner of object tells server about choosen name
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
    public void ChangeInitialSettingsClientRpc(string playerName, int thisClientID)
    {
        try //Protection for second rounds
        {
            //Changing the name on player HUD prefab
            transform.GetChild(0).Find("HUD").Find("Name").GetComponent<TextMeshPro>().text = playerName;
            string otherPlayerSelectedSkin = transform.GetChild(0).gameObject.name.Replace("(Clone)", ""); //When instancing the prefab it shows up as "Huntress(Clone)", removing "(Clone)" for it to be easier to read

            //Changing name on interface and interface appearance
            var otherPlayerInterface = GameObject.Find("Canvas - HUD").transform.GetChild(thisClientID);
            otherPlayerInterface.Find("Disconnected").gameObject.SetActive(false);//Deactivating it in case someone in that position previously disconnected
            charaSkin = otherPlayerSelectedSkin;
            otherPlayerInterface.gameObject.SetActive(true);//Activating interface
            otherPlayerInterface.transform.Find("Name").GetComponent<TMPro.TextMeshProUGUI>().text = playerName;


            switch (otherPlayerSelectedSkin)
            {
                case "Huntress":
                    otherPlayerInterface.transform.Find("BG").gameObject.GetComponent<Image>().color = green;
                    otherPlayerInterface.transform.Find("Sprite").gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("Huntress_HUD");

                    //If we come from a previous game, if this character was dead its interface got changed to black.
                    //We change it back to its original colors in case we are not playing the first game
                    otherPlayerInterface.transform.Find("Sprite").gameObject.GetComponent<Image>().color = white;
                    break;
                case "Oni":
                    otherPlayerInterface.transform.Find("BG").gameObject.GetComponent<Image>().color = blue;
                    otherPlayerInterface.transform.Find("Sprite").gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("Oni_HUD");
                    otherPlayerInterface.transform.Find("Sprite").gameObject.GetComponent<Image>().color = white;
                    break;
                default://"AkaiKaze"
                    otherPlayerInterface.transform.Find("BG").gameObject.GetComponent<Image>().color = red;
                    otherPlayerInterface.transform.Find("Sprite").gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("AkaiKaze_HUD");
                    otherPlayerInterface.transform.Find("Sprite").gameObject.GetComponent<Image>().color = white;
                    break;
            }


        }
        catch (Exception ex) { print("Excepcion en changeInitialSettings:" + ex); }
    }





    [ServerRpc]
    public void GetSettingsFromPreviousPlayersServerRpc()
    {
        //Per each client, we take its respective Player Attributes in order to asociate its variables to animator and name
        //This is needed since it wasnt shown data from previously connected players,
        //only players connected after our own connection
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            string name = client.PlayerObject.GetComponentInChildren<PlayerAttributes>().playerName;
            int playerNum = client.PlayerObject.GetComponent<PlayerNetworkConfig>().playerNum.Value;
            client.PlayerObject.GetComponentInChildren<PlayerAttributes>().ChangeInitialSettingsClientRpc(name, playerNum);
        }
    }





}






