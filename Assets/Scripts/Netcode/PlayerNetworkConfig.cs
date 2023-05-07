using UI;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using System.Collections;
using Cinemachine;
using TMPro;

namespace Netcode
{
    public class PlayerNetworkConfig : NetworkBehaviour
    {
        public NetworkVariable<int> life;
        public GameObject characterPrefab;
        public NetworkVariable<bool> destroyed;
        public ConnectedPlayers players;
        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            //InstantiateCharacterServerRpc(OwnerClientId);

            string prefabName = GameObject.Find("UI").GetComponent<UIHandler>().playerSprite;
            ChangeCharacterServerRpc(OwnerClientId, prefabName);
            players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();

        }




        [ServerRpc]
        public void InstantiateCharacterServerRpc(ulong id)
        {

   
            GameObject characterGameObject = Instantiate(characterPrefab);
            characterGameObject.GetComponent<NetworkObject>().SpawnWithOwnership(id); //Mirar SpawnAsPlayerObject -- NO USAR, INESTABLE
            //characterGameObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(id); //Si spawneamos as�, el propio jugador es PlayerPrefab pero el resto de clientes los recibe como HuntressPrefab
            characterGameObject.transform.SetParent(transform, false);


        }
        
        
        [ServerRpc (RequireOwnership =false)]
        public void checkLifeServerRpc()
        {
           
            life.Value -=50;
            print("vida: " + life.Value);

            if (life.Value <= 0)
            {

                var players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();

                players.alivePlayers.Value -= 1;
               
                //BUSCAR UNO VIVO Y PASARLE LA TRANSFORMADA A DESTROYCHARACTER
            
                DestroyCharacter(false);


                if (players.alivePlayers.Value == 1)
                {
                    
                    print("win! ");
                    //para calcular quién ha ganado se mira qué jugador queda con mayor vida y se coloca en un networkvariable
                    players.calculateWinner();
                    StartCoroutine(Order());
                   

                }  
            }



   
        }

        IEnumerator Order()
        {
           
            print("corrutina de ganar---");
            yield return new WaitForSeconds(3.0f);
            
            checkWinClientRpc(false);
              
        }


 
        public void DestroyCharacter(bool timeout)
        {

            print("ESS");
           
           
            try {


            
            for (var i = this.transform.childCount - 1; i >= 0; i--)
            {
                print("child");
                Destroy(this.transform.GetChild(i).gameObject);
            }
            //alomejor corruitna aqui?
                moveCameraClientRpc(timeout);
            }
            catch(System.Exception ex)
            {
                print(ex);
            }
          
           
        } 
        [ClientRpc]
       public void moveCameraClientRpc(bool timeout)
        {
            //METODO QUE MUEVE LA CÁMARA A NIVEL DE CLIENTE
            Transform a;
           
            //SI HAY QUE MOVER LA CÁMARA PORQUE SE HA ACABADO EL TIEMPO= TODOS SIGUEN AL GANADOR
            if (timeout)
            {
                a = players.allPlayers[players.allPlayers.Count - 1].GetComponentInChildren<Netcode.FighterNetworkConfig>().transform;
                ICinemachineCamera virtualCamera = CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera;
                virtualCamera.Follow = a;
                print(a);
                print(virtualCamera);
            }
            else
            {
                //SI HAY QUE MOVERLA PORQUE HAN MATADO A UN PERSONAJE, SE MIRA QUE PERSONAJE ES Y SE CAMBIA A UNO RANDOM
                if (GameObject.Find("InputSystem").GetComponent<Systems.InputSystem>().Character == null)
                {

                    PlayerNetworkConfig randomPlayer;
                    do
                    {

                        randomPlayer = players.allPlayers[Random.Range(0, players.allPlayers.Count)];

                    } while (randomPlayer.life.Value <= 0);
                  a = randomPlayer.GetComponentInChildren<Netcode.FighterNetworkConfig>().transform;
                    ICinemachineCamera virtualCamera = CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera;
                    virtualCamera.Follow = a;
                    print(a);
                    print(virtualCamera);
                }

            }
            //DESTROYED SSIRVE PARA SABER SI YA SE HABÍA CAMBIADO LA CÁMARA ANTES
               
                
             //   destroyed.Value = true;
 

        }

        [ServerRpc]
        public void ChangeCharacterServerRpc(ulong id, string prefabName) 
        {

            string prefabPath = prefabName;
            GameObject prefab = Resources.Load<GameObject>(prefabPath);

            
            GameObject characterGameObject = Instantiate(prefab);
            //GameObject HUD = Instantiate(Resources.Load<GameObject>("HUD"));

            characterGameObject.GetComponent<NetworkObject>().SpawnWithOwnership(id);

            characterGameObject.transform.SetParent(transform, false);
            //HUD.transform.SetParent(characterGameObject.transform, false);

            players.alivePlayers.Value += 1;

              players.player1 = this;

            print("player nuevo! n de players: "+players.alivePlayers.Value);
            destroyed.Value = false;


           
            life.Value = 100;
            
            
            players.allPlayers.Add(this);
    
            print(GameObject.Find("Players").GetComponent<ConnectedPlayers>().player1);
            print(GameObject.Find("Players").GetComponent<ConnectedPlayers>().player1.life.Value);
        }

        [ClientRpc]
        public void checkWinClientRpc(bool tie)
        {

            //tie es true cuando ha quedado mas de un personaje vivo

            if (tie)
            {
                players.showEmpate();
            }
            else
            {

                players.showWinnerClientRpc();
                // WinText.text = "¡" + winnerName + " wins!";
                if (GameObject.Find("InputSystem").GetComponent<Systems.InputSystem>().Character != null)
                {
           

                    print("has gabnado");

                    players.showGanar();
                }
                else
                {
                    print("has perdido");
                    players.showPerder();
                }
            }

        }


    }
}
