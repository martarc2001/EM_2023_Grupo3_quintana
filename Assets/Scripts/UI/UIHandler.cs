using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIHandler : MonoBehaviour
    {

        //public Image vida;

        public GameObject lobbyPanelHost;
        public GameObject lobbyPanelClient;

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



        public static UIHandler Instance { get; private set; }


        private void Start()
        {

            Instance=this;
            //hostButton.onClick.AddListener(OnHostButtonClicked);
            //clientButton.onClick.AddListener(OnClientButtonClicked);
            

            //Name selection
            inputTextBox.onValueChanged.AddListener(characterNameSelected);

            //Sprite selection
            akaiKazeButton.onClick.AddListener(() => characterSpriteSelected("Akai Kaze"));
            oniButton.onClick.AddListener(() => characterSpriteSelected("Oni"));
            huntressButton.onClick.AddListener(() => characterSpriteSelected("Huntress"));

            initiatonButton.onClick.AddListener(PlayerInitiation);


        }

        public void InstantiateHost()
        {
            hostSelection = true;

            characterSelectionPanel.SetActive(true);
            lobbyPanelHost.SetActive(false);
            lobbyPanelClient.SetActive(false);
            //NetworkManager.Singleton.StartHost();
        }

        public void InstantiateClient()
        {
            hostSelection = false;

            characterSelectionPanel.SetActive(true);
            lobbyPanelHost.SetActive(false);
            lobbyPanelClient.SetActive(false);
            //NetworkManager.Singleton.StartClient();
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

        private void UpdateLife()
        {

        }
    }
}