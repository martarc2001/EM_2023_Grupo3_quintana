using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;
using UnityEngine.InputSystem;
using Netcode;

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
    public GameObject lostp1;
    public GameObject lostp2;
    public GameObject lostp3;
    public GameObject lostp4;
    public GameObject imgGanar;
    public GameObject imgPerder;
    public GameObject imgEmpate;
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

        lostp1 = GameObject.Find("Panel1");
        lostp2 = GameObject.Find("Panel2");
        lostp3 = GameObject.Find("Panel3");
        lostp4 = GameObject.Find("Panel4");
        hideShowError(lostp1, false);
        hideShowError(lostp2, false);
        hideShowError(lostp3, false);
        hideShowError(lostp4, false);


    }

    // Update is called once per frame
    void Update()
    {

     
    }
     void hideShowError(GameObject g, bool active)
    {
        g.SetActive(active);
        for (var i = g.transform.childCount - 1; i >= 0; i--)
        {
            g.transform.GetChild(i).gameObject.SetActive(active);
        }
    }

    public void ClientDisconnectedMessage(ulong clientId)
    {
        //buscamos qué mensaje de error hay que activar
        int clientpos = 0;
        foreach (NetworkClient myclient in NetworkManager.Singleton.ConnectedClientsList)
        {
            clientpos++;
            if (clientId == myclient.ClientId)
            {
                switch (clientpos)
                {
                    case 0:
                        hideShowError(lostp1, true);
                        break;
                    case 1:
                        hideShowError(lostp2, true);
                        break;
                    case 2:
                        hideShowError(lostp3, true);
                        break;
                    case 3:
                        hideShowError(lostp4, true);
                        break;
                }
                clientpos = 0;
                break;
            }
        }
    }

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
        updateTimerClientRpc(currentTime,minutes,seconds);
    }

    [ClientRpc]
    void updateTimerClientRpc(float currentTime,float minutes,float seconds)
    {
        TimerTxt.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    [ServerRpc]
    public void endServerRpc()
    {

        player1= GameObject.Find("Player(Clone)").GetComponent<Netcode.PlayerNetworkConfig>();
        Netcode.PlayerNetworkConfig playerWin = calculateWinner();

        int winningLife = playerWin.life.Value;
        int loosingLife = allPlayers[0].life.Value;
        print("ganador:" + winningLife);

        //EMPATE
        if (winningLife == loosingLife)
        {
            print("A");
            player1.checkWinClientRpc(true);
        }
        else
        {

            for (int i = 0; i <= allPlayers.Count - 1; i++)
            {
                print(allPlayers[i].life.Value);
                //SI TIENE MENOS VIDA QUE EL GANADOR

                if (allPlayers[i].life.Value != winningLife)
                {
                    print(allPlayers[i].life.Value + " personaje:  " + allPlayers[0]);
                    alivePlayers.Value -= 1;

                    allPlayers[i].DestroyCharacter(true);

                }
            }
    
            //mostrar si han ganado o no
            StartCoroutine(Order());
           
        }
    }

 IEnumerator Order()
    {
     
        player1 = GameObject.Find("Player(Clone)").GetComponent<Netcode.PlayerNetworkConfig>();
        yield return new WaitForSeconds(3.0f);
        player1.checkWinClientRpc(false);
    }
    //metodo que calcula el ganador
    public Netcode.PlayerNetworkConfig calculateWinner()
    {
        if (IsServer)
        {

       
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


    public void showGanar()
    {
        imgGanar.SetActive(true);
    }
    public void showPerder()
    {
        imgPerder.SetActive(true);
    }
    public void showEmpate()
    {
        imgEmpate.SetActive(true);
    }
   
    //metodo que muestra a todos los clientes el ganador
    [ClientRpc]
    public void showWinnerClientRpc()
    {

        WinText.text = "¡"+winnerName.Value.ToString()+" wins!";
       
    }
    public void showError()
    {
        error.SetActive(true);
    }


    [ServerRpc(RequireOwnership =false)]
    public void ShowReadyPlayersServerRpc()
    {
        readyPlayers.Value++;
        LobbyWaiting.Instance.waitingText.text = "Waiting for players " + readyPlayers.Value + "/4 ready";

        if (readyPlayers.Value == 4)
        {
            WaitCountdown();
            
        }
    }

    void WaitCountdown()
    {
        LobbyWaiting.Instance.gameObject.SetActive(false);
        LobbyWaiting.Instance.gameWillStart.SetActive(true);
        WaitCountdownClientRpc();
    }

    [ClientRpc]
    void WaitCountdownClientRpc()
    {
        LobbyWaiting.Instance.gameObject.SetActive(false);
        LobbyWaiting.Instance.gameWillStart.SetActive(true);
    }

    [ServerRpc(RequireOwnership =false)]
    public void StartGameServerRpc()
    {
        gameStarted = true;
        LobbyWaiting.Instance.gameWillStart.SetActive(false);
        LobbyManager.Instance.DeleteLobby();
        Timer.SetActive(true);

        foreach(NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            SetGameStartPosition((int)client.ClientId);
            print((int)client.ClientId);

        }
            
        StartGameClientRpc();
    }


    [ClientRpc]
   void StartGameClientRpc()
    {
        gameStarted = true;
        Timer.SetActive(true);
        LobbyWaiting.Instance.gameWillStart.SetActive(false);
    }
    public void SetGameStartPosition(int idClient)
    {
   allPlayers[idClient].transform.GetChild(0).transform.position = spawnPositionList[idClient];
    }






}
