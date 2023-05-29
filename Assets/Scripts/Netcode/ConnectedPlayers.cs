using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;
using UnityEngine.InputSystem;
using Netcode;
using static UnityEngine.InputSystem.HID.HID;
using UI;
using UnityEngine.UI;

public class ConnectedPlayers : NetworkBehaviour
{

    public NetworkVariable<int> readyPlayers = new NetworkVariable<int>();

    public NetworkVariable<int> alivePlayers;
    public NetworkVariable<bool> end;
    NetworkVariable<FixedString32Bytes> winnerName;

    public Netcode.PlayerNetworkConfig player1;
    public GameObject error;
    public GameObject winner;
    public TextMeshProUGUI WinText;
    public List<Netcode.PlayerNetworkConfig> allPlayers;
    public GameObject leftLobbyMessage;
    public Dictionary<ulong, int> d_clientIdRefersToPlayerNum = new Dictionary<ulong, int>();

    public GameObject imgGanar;
    public GameObject imgPerder;
    public GameObject imgEmpate;
    public GameObject fightSign;
    public float seconds;
    public bool gameStarted = false;
    public TextMeshProUGUI TimerTxt;
    public GameObject Timer;
    [SerializeField] public List<Vector3> spawnPositionList;

    public static ConnectedPlayers Instance { get; private set; }

    private int sortplayers(Netcode.PlayerNetworkConfig p1, Netcode.PlayerNetworkConfig p2)
    {
        return p1.life.Value.CompareTo(p2.life.Value);
    }
    private void Awake()
    {
        Instance = this;
        allPlayers = new List<Netcode.PlayerNetworkConfig>();
        seconds = 30;


        WinText = winner.GetComponent<TextMeshProUGUI>();
        WinText.text = "";

        end.Value = false;

        error.SetActive(false);
        imgGanar.SetActive(false);
        imgPerder.SetActive(false);
        imgEmpate.SetActive(false);
        alivePlayers = new NetworkVariable<int>(0);
        winnerName = new NetworkVariable<FixedString32Bytes>("");

    }

    #region Timer
    private void FixedUpdate()
    {

        if (gameStarted)
        {
            if (IsServer)
            {
                counterServerRpc();
            }
        }
    }


    [ServerRpc]
    public void counterServerRpc()
    {
        if (end.Value == false)
        {
            if (seconds > 0)
            {
                seconds -= Time.deltaTime;
                updateTimer(seconds);


            }
            else
            {
                gameStarted = false;
                end.Value = true;
                print("fin");
                endServerRpc();
            }
        }
    }

    void updateTimer(float currentTime)
    {
        currentTime += 1;
        float minutes = Mathf.FloorToInt(currentTime / 60);
        float seconds = Mathf.FloorToInt(currentTime % 60);
        TimerTxt.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        updateTimerClientRpc(currentTime, minutes, seconds);
    }

    [ClientRpc]
    void updateTimerClientRpc(float currentTime, float minutes, float seconds)
    {
        TimerTxt.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    [ServerRpc]
    public void endServerRpc()
    {

        player1 = GameObject.Find("Player(Clone)").GetComponent<Netcode.PlayerNetworkConfig>();
        Netcode.PlayerNetworkConfig playerWin = calculateWinner();

        int winningLife = playerWin.life.Value;
        int loosingLife = allPlayers[0].life.Value;
        print("ganador:" + winningLife);

        //EMPATE
        if (winningLife == loosingLife)
        {

            player1.CheckWinClientRpc(true);
        }
        else
        {

            for (int i = 0; i <= allPlayers.Count - 1; i++)
            {
                //SI TIENE MENOS VIDA QUE EL GANADOR
                if (allPlayers[i].life.Value != winningLife)
                {
                    alivePlayers.Value -= 1;

                    allPlayers[i].dead.Value = true;

                }
            }

            //mostrar si han ganado o no
            StartCoroutine(Order());

        }
    }
    #endregion

    #region Checking and showing win
    IEnumerator Order()
    {

        player1 = GameObject.Find("Player(Clone)").GetComponent<Netcode.PlayerNetworkConfig>();
        yield return new WaitForSeconds(3.0f);
        player1.CheckWinClientRpc(false);
    }
    //metodo que calcula el ganador
    public Netcode.PlayerNetworkConfig calculateWinner()
    {
        if (IsServer)
        {
            gameStarted = false;


            allPlayers.Clear();
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                allPlayers.Add(client.PlayerObject.GetComponent<Netcode.PlayerNetworkConfig>());
            }
            allPlayers.Sort(sortplayers);
            Netcode.PlayerNetworkConfig winner = allPlayers[allPlayers.Count - 1];
            winnerName.Value = winner.GetComponentInChildren<TextMeshPro>().text;
            return winner;
        }
        return null;

    }

    [ServerRpc]
    public void RestartServerRpc()
    {
        
        winnerName = new NetworkVariable<FixedString32Bytes>("");
        error.SetActive(false);
        seconds = 30;
        readyPlayers.Value = 0;
        Timer.SetActive(false);
        gameStarted = false;
        alivePlayers.Value = NetworkManager.Singleton.ConnectedClientsList.Count;
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {

            var player = client.PlayerObject.GetComponent<PlayerNetworkConfig>();
            var config = client.PlayerObject.GetComponent<PlayerAttributes>();

           

            player.life.Value = 100;
     

            if (player.dead.Value)
            {
          
                player.dead.Value = false;


                string prefabName = config.charaSkin;
                GameObject characterPrefab = player.characterPrefab;
                GameObject prefab = Resources.Load<GameObject>(prefabName);
                characterPrefab = Instantiate(prefab, GameObject.Find("SpawnPoints").transform.GetChild((int)OwnerClientId).transform);
                characterPrefab.GetComponent<NetworkObject>().SpawnWithOwnership(client.ClientId);
                player.characterPrefab = characterPrefab;
                characterPrefab.transform.SetParent(client.PlayerObject.transform, false);

             

            }
            restartClientRpc(client.ClientId);
            config.ChangeInitialSettingsClientRpc(config.playerName, (int)client.ClientId);
            
        }
        if (IsOwner)
        {
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList) //Por cada cliente, coge su respectivo Player Attributes para poder asociar sus variables a animator y nombre
            {
                string name = client.PlayerObject.GetComponentInChildren<PlayerAttributes>().playerName;
                client.PlayerObject.GetComponentInChildren<PlayerAttributes>().ChangeInitialSettingsClientRpc(name, (int)client.ClientId);
                Debug.Log("Cliente: " + client.ClientId);
            }
        }

    }

    //CHARACTERPREFAB, PREFABNAME, PLAYERNAME
    [ClientRpc]
    public void restartClientRpc(ulong id)
    {
     
        LobbyWaiting.Instance.gameObject.SetActive(true);
        LobbyWaiting.Instance.readyButton.gameObject.SetActive(true);
        Timer.SetActive(false);
        imgGanar.SetActive(false);
        imgPerder.SetActive(false);
        imgEmpate.SetActive(false);

        error.SetActive(false);
        WinText.text = " ";
        


    }


    public void showGanar() { imgGanar.SetActive(true); }
    public void showPerder() { imgPerder.SetActive(true); }
    public void showEmpate() { imgEmpate.SetActive(true); }

    //metodo que muestra a todos los clientes el ganador
    [ClientRpc]
    public void showWinnerClientRpc() {

       
         WinText.text = "¡" + winnerName.Value.ToString() + " wins!";
        winner.SetActive(true);
    }
    #endregion

    #region From lobby to game
    [ServerRpc(RequireOwnership = false)]
    public void ShowReadyPlayersServerRpc()
    {
        readyPlayers.Value++;
        LobbyWaiting.Instance.waitingText.text = "Waiting for players " + readyPlayers.Value + "/" + LobbyManager.Instance.maxPlayers + " ready";

        if (readyPlayers.Value == LobbyManager.Instance.maxPlayers) { WaitCountdown(); }
    }



    void WaitCountdown()
    {
        LobbyWaiting.Instance.gameObject.SetActive(false);
        LobbyWaiting.Instance.gameWillStart.SetActive(true);
        GameObject.Find("TimerStartGame").GetComponent<TimerScript>().restart();
       
        WaitCountdownClientRpc();
    }

    [ClientRpc]
    void WaitCountdownClientRpc()
    {
      
        LobbyWaiting.Instance.gameObject.SetActive(false);
        LobbyWaiting.Instance.gameWillStart.SetActive(true);
        GameObject.Find("Canvas").GetComponentInChildren<TimerScript>().startTimer();
        GameObject.Find("TimerStartGame").GetComponent<TimerScript>().restart();
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc()
    {
        gameStarted = true;
        LobbyWaiting.Instance.gameWillStart.SetActive(false);
        //LobbyManager.Instance.DeleteLobby();
        Timer.SetActive(true);
        fightSign.SetActive(true);
        Invoke("HideFightSign", 1.5f);
        end.Value = false;
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            //SetGameStartPosition((int)client.ClientId);
            SetGameStartPosition((int)client.ClientId, client.PlayerObject.GetComponent<PlayerNetworkConfig>().playerNum.Value);
            print((int)client.ClientId);
   
        }

        StartGameClientRpc();
    }

    [ClientRpc]
    void StartGameClientRpc()
    {
        gameStarted = true;
        Timer.SetActive(true);
        fightSign.SetActive(true);
        LobbyWaiting.Instance.gameWillStart.SetActive(false);
    }

    public void SetGameStartPosition(int idClient, int pos)
    {
        allPlayers[idClient].transform.GetChild(0).transform.position = spawnPositionList[pos];
    }
    #endregion

    #region Fight sign
    void HideFightSign()
    {
        fightSign.SetActive(false);
        HideFightSignClientRpc();
    }

    [ClientRpc]
    void HideFightSignClientRpc() { fightSign.SetActive(false); }
    #endregion

    public void showError() { error.SetActive(true); } //This error shows up if server/host disconnects

}
