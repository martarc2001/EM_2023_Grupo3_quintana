using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIHandler : MonoBehaviour
    {
        public GameObject debugPanel;
        public Button hostButton;
        public Button clientButton;
        public Button serverButton;
        public bool hostSelection;

        [SerializeField] public GameObject characterSelectionPanel;
        [SerializeField] public Button initiatonButton;

        //Getting name from text box
        [SerializeField] private TMP_InputField inputTextBox;
        public string playerName = "";

        //Sprite selection
        [SerializeField] private Button akaiKazeButton;
        [SerializeField] private Button oniButton;
        [SerializeField] private Button huntressButton;
        public string playerSprite;

        private void Start()
        {
            hostButton.onClick.AddListener(OnHostButtonClicked);
            clientButton.onClick.AddListener(OnClientButtonClicked);
            serverButton.onClick.AddListener(OnServerButtonClicked);

            //Name selection
            inputTextBox.onValueChanged.AddListener(characterNameSelected);

            //Sprite selection
            akaiKazeButton.onClick.AddListener(() => characterSpriteSelected("Akai Kaze"));
            oniButton.onClick.AddListener(() => characterSpriteSelected("Oni"));
            huntressButton.onClick.AddListener(() => characterSpriteSelected("Huntress"));

            initiatonButton.onClick.AddListener(PlayerInitiation);


        }

        private void OnHostButtonClicked()
        {
            hostSelection = true;

            characterSelectionPanel.SetActive(true);
            debugPanel.SetActive(false);

            //NetworkManager.Singleton.StartHost();
        }

        private void OnClientButtonClicked()
        {
            hostSelection = false;

            characterSelectionPanel.SetActive(true);
            debugPanel.SetActive(false);

            //NetworkManager.Singleton.StartClient();
        }

        private void OnServerButtonClicked()
        {
            NetworkManager.Singleton.StartServer();
            debugPanel.SetActive(false);
        }

        private void characterNameSelected(string text)
        {
            playerName = text;
            if (playerName != "" && playerSprite != "") { initiatonButton.gameObject.SetActive(true); }
        }

        private void characterSpriteSelected(string buttonPressed)
        {
            playerSprite = buttonPressed;
            if (playerName != "" && playerSprite !="") { initiatonButton.gameObject.SetActive(true); }
        }

        private void PlayerInitiation()
        {
            if(hostSelection) NetworkManager.Singleton.StartHost();
            else NetworkManager.Singleton.StartClient();

            characterSelectionPanel.SetActive(false);

        }
    }
}