using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class ConnectedPlayers : NetworkBehaviour
{
    public NetworkVariable<int> alivePlayers;
    public NetworkVariable<bool> end;
    public Netcode.PlayerNetworkConfig player1;

    public List<Netcode.PlayerNetworkConfig> allPlayers;
    public GameObject imgGanar;
    public GameObject imgPerder;
    public GameObject imgEmpate;
    public float seconds;
    public bool start;

    void Start()
    {
        
    }
    private int sortplayers(Netcode.PlayerNetworkConfig p1, Netcode.PlayerNetworkConfig p2)
    {
        return p1.life.Value.CompareTo(p2.life.Value);
    }
    private void Awake()
    {
        allPlayers = new List<Netcode.PlayerNetworkConfig>();
        seconds = 5;

        imgEmpate = GameObject.Find("empate");
        imgPerder = GameObject.Find("NewCanvas6");
        imgGanar = GameObject.Find("NewCanvasganado");
        end.Value = false;

        imgGanar.SetActive(false);
        imgPerder.SetActive(false);
        imgEmpate.SetActive(false);
        alivePlayers = new NetworkVariable<int>(0);

    }

    // Update is called once per frame
    void Update()
    {

        counterServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void counterServerRpc()
    {
        if (end.Value == false)
        {
            if (seconds > 0)
            {
                seconds -= Time.deltaTime;
                print(seconds);
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
        int winningLife = allPlayers[allPlayers.Count-1].life.Value;
        int loosingLife = allPlayers[0].life.Value;
        print("ganador:"+ winningLife);

        //EMPATE
        if (winningLife == loosingLife)
        {
            print("A");
          player1.checkWinClientRpc(true);
        }
        else
        {

          
                //SI TIENE MENOS VIDA QUE EL GANADOR

                if (player1.life.Value != winningLife)
                {
                    print(player1.life.Value+" personaje:  "+player1);
                    alivePlayers.Value -= 1;
                    print(alivePlayers.Value);
                
                
                       
                        player1.DestroyCharacter();

                     print(player1);
                     print(player1.destroyed.Value);
                    

                //SI TIENE LA MISMA VIDA PERO NO ES EL GANADOR =VARIOS GANADORES
                //if (p.life.Value == winningLife && p != allPlayers[allPlayers.Count - 1])
                // {
                // }
            }

            //mostrar si han ganado o no
            player1.checkWinClientRpc(false);
        }
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
