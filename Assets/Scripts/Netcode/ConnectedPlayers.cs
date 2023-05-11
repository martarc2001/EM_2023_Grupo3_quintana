using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;

public class ConnectedPlayers : NetworkBehaviour
{
    public NetworkVariable<int> alivePlayers;
    public NetworkVariable<bool> end;
    NetworkVariable<FixedString32Bytes> winnerName;

    public Netcode.PlayerNetworkConfig player1;
    public GameObject error;
    public GameObject winner;
    public TextMeshProUGUI WinText;
    public List<Netcode.PlayerNetworkConfig> allPlayers;
 
    public GameObject imgGanar;
    public GameObject imgPerder;
    public GameObject imgEmpate;
    public float seconds;
    public bool start;

    

  
    private int sortplayers(Netcode.PlayerNetworkConfig p1, Netcode.PlayerNetworkConfig p2)
    {
        return p1.life.Value.CompareTo(p2.life.Value);
    }
    private void Awake()
    {
        allPlayers = new List<Netcode.PlayerNetworkConfig>();
        seconds = 30;

       
        WinText = winner.GetComponent<TextMeshProUGUI>();
        WinText.text = "";
   
        imgEmpate = GameObject.Find("empate");
        imgPerder = GameObject.Find("NewCanvas6");
        imgGanar = GameObject.Find("NewCanvasganado");
        end.Value = false;

        error.SetActive(false);
        imgGanar.SetActive(false);
        imgPerder.SetActive(false);
        imgEmpate.SetActive(false);
        alivePlayers = new NetworkVariable<int>(0);
        winnerName = new NetworkVariable<FixedString32Bytes>();

    }

    // Update is called once per frame
    void Update()
    {

     
    }

    private void FixedUpdate()
    {

        if (IsServer)
        {
            if (NetworkManager.Singleton.ConnectedClientsList.Count > 1)
            {
                counterServerRpc();
            }
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void counterServerRpc()
    {
        if (end.Value == false)
        {
            if (seconds > 0)
            {
                seconds -= Time.deltaTime;
               
              
            }
            else
            {
                end.Value = true;
                print("fin");
                endServerRpc();
            }
        }     
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
}
