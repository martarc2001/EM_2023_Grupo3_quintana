using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Netcode;

public class ConnectedPlayers : NetworkBehaviour
{
    public NetworkVariable<int> alivePlayers;
    public NetworkVariable<bool> end;
    public Netcode.PlayerNetworkConfig player1;

    public List<PlayerNetworkConfig> allPlayers;
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
        
        imgEmpate = GameObject.Find("empate");
        imgPerder = GameObject.Find("NewCanvas6");
        imgGanar = GameObject.Find("NewCanvasganado");
        end.Value = false;

        imgGanar.SetActive(false);
        imgPerder.SetActive(false);
        imgEmpate.SetActive(false);
        alivePlayers = new NetworkVariable<int>(0);

    }


    private void FixedUpdate()
    {

        if (allPlayers.Count > 1)
        {
            counterServerRpc();
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


        allPlayers.Sort(sortplayers);
        int winningLife = allPlayers[allPlayers.Count - 1].life.Value;
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
                    allPlayers[i].DestroyCharacter();

                }
            }

            //mostrar si han ganado o no
            StartCoroutine(Order());
        }
    }

    IEnumerator Order()
    {      
        yield return new WaitForSeconds(3.0f);
        player1.CheckWinClientRpc(false);
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
}
