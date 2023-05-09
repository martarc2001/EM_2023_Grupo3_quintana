using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using TMPro;

public class ConnectedPlayers : NetworkBehaviour
{
    public NetworkVariable<int> readyPlayers = new NetworkVariable<int>();

    public NetworkVariable<int> alivePlayers;
    public NetworkVariable<bool> end;
    public Netcode.PlayerNetworkConfig player1;

    public List<Netcode.PlayerNetworkConfig> allPlayers;
    public GameObject imgGanar;
    public GameObject imgPerder;
    public GameObject imgEmpate;
    public float seconds;
    public bool gameStarted = false;
    public TextMeshProUGUI TimerTxt;
    public GameObject Timer;

    public static ConnectedPlayers Instance { get; private set; }
  
    private int sortplayers(Netcode.PlayerNetworkConfig p1, Netcode.PlayerNetworkConfig p2)
    {
        return p1.life.Value.CompareTo(p2.life.Value);
    }
    private void Awake()
    {
        Instance = this;
        allPlayers = new List<Netcode.PlayerNetworkConfig>();
        seconds = 16;
        
       
        end.Value = false;

        alivePlayers = new NetworkVariable<int>(0);

    }

    // Update is called once per frame
    void Update()
    {

        
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


        allPlayers.Sort(sortplayers);
        int winningLife = allPlayers[allPlayers.Count - 1].life.Value;
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

                    allPlayers[i].DestroyCharacter(allPlayers[allPlayers.Count - 1].GetComponentInChildren<Netcode.FighterNetworkConfig>().transform);

                }
            }

            //mostrar si han ganado o no
            StartCoroutine(Order());
        }
    }

    IEnumerator Order()
    {
      
        yield return new WaitForSeconds(3.0f);
        player1.checkWinClientRpc(false);
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

    [ServerRpc]
    public void StartGameServerRpc()
    {
        gameStarted = true;
        LobbyWaiting.Instance.gameWillStart.SetActive(false);
        LobbyManager.Instance.DeleteLobby();
        Timer.SetActive(true);
      //Reset position
        StartGameClientRpc();
    }


    [ClientRpc]
   void StartGameClientRpc()
    {
        gameStarted = true;
        Timer.SetActive(true);
        LobbyWaiting.Instance.gameWillStart.SetActive(false);
        
    }


}
