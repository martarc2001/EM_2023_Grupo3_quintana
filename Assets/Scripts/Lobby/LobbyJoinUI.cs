using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyJoinUI : MonoBehaviour
{
    [SerializeField] private Button quickJoin;
    [SerializeField] private Button joinByCode;
    [SerializeField] TMP_InputField enterLobbyCode;
    [SerializeField] GameObject noLobbiesFound;

    [SerializeField] private GameObject returnLobbyUI;
    [SerializeField] private Button returnButton;

   

public static LobbyJoinUI Instance { get; private set; }
    void Awake() {

        Instance = this;

        quickJoin.onClick.AddListener(() => {
           
            LobbyManager.Instance.QuickJoin();
        
      
        });

        joinByCode.onClick.AddListener(() => {
            if (enterLobbyCode.text != "")
            {
                LobbyManager.Instance.JoinWithCode(enterLobbyCode.text);
            }
            else
            {
                ShowIssue();
            }

        });

        returnButton.onClick.AddListener(() =>
        {
            returnLobbyUI.SetActive(true);
            gameObject.SetActive(false);
        });
    }

   public void ShowIssue()
    {
        noLobbiesFound.SetActive(true);
        Invoke("DisguiseIssue", 2f);
    }

    private void DisguiseIssue()
    {
        noLobbiesFound.SetActive(false);
    }
}
