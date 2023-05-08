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

    [SerializeField] private GameObject returnLobbyUI;
    [SerializeField] private Button returnButton;

    void Awake() {
        quickJoin.onClick.AddListener(() => {
            gameObject.SetActive(false);
            LobbyManager.Instance.QuickJoin();
        
      
        });

        joinByCode.onClick.AddListener(() => {
            gameObject.SetActive(false);
            LobbyManager.Instance.JoinWithCode(enterLobbyCode.text);

        });

        returnButton.onClick.AddListener(() =>
        {
            returnLobbyUI.SetActive(true);
            gameObject.SetActive(false);
        });
    }
}
