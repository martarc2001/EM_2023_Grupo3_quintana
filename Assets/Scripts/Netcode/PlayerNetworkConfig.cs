using UI;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using System.Collections;
using Cinemachine;

namespace Netcode
{
    public class PlayerNetworkConfig : NetworkBehaviour
    {
        public NetworkVariable<int> life;
        public GameObject characterPrefab;
        public NetworkVariable<bool> destroyed;
        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            //InstantiateCharacterServerRpc(OwnerClientId);

            string prefabName = GameObject.Find("UI").GetComponent<UIHandler>().playerSprite;
            ChangeCharacterServerRpc(OwnerClientId, prefabName);
        }

       


        [ServerRpc]
        public void InstantiateCharacterServerRpc(ulong id)
        {

   
            GameObject characterGameObject = Instantiate(characterPrefab);
            characterGameObject.GetComponent<NetworkObject>().SpawnWithOwnership(id); //Mirar SpawnAsPlayerObject -- NO USAR, INESTABLE
            //characterGameObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(id); //Si spawneamos as�, el propio jugador es PlayerPrefab pero el resto de clientes los recibe como HuntressPrefab
            characterGameObject.transform.SetParent(transform, false);
            

            //contador

        }
        
        
        [ServerRpc(RequireOwnership = false)]
        public void checkLifeServerRpc()
        {
           
            life.Value -=50;
            print("vida: " + life.Value);

            if (life.Value <= 0)
            {

                var players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();

                players.alivePlayers.Value -= 1;
                destroyed.Value = true;

                //BUSCAR UNO VIVO Y PASARLE LA TRANSFORMADA A DESTROYCHARACTER
                PlayerNetworkConfig randomPlayer;
                do
                {
                  
                    randomPlayer = players.allPlayers[Random.Range(0, players.allPlayers.Count)];

                } while (randomPlayer.life.Value <= 0);

               
                Transform a = randomPlayer.GetComponentInChildren<Netcode.FighterNetworkConfig>().transform;
                DestroyCharacter(a);


                if (players.alivePlayers.Value == 1)
                {
                print("win! ");
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


 
        public void DestroyCharacter(Transform t )
        {
            destroyed.Value = true;
            print("ESS");
            var players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();
            try { 
            ICinemachineCamera virtualCamera = CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera;
            virtualCamera.Follow = t;
            }catch(System.Exception ex)
            {
                print(ex);
            }
            for (var i = this.transform.childCount - 1; i >= 0; i--)
            {
                print("child");
                Destroy(this.transform.GetChild(i).gameObject);
            }
           


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
            var players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();
            players.alivePlayers.Value += 1;

            GameObject.Find("Players").GetComponent<ConnectedPlayers>().player1 = this;

            print("player nuevo! n de players: "+players.alivePlayers.Value);
            destroyed.Value = false;


           
            life.Value = 100;
            
            
            players.allPlayers.Add(this);

        }

        [ClientRpc]
        public void checkWinClientRpc(bool tie)
        {
            print(this);
            print(destroyed.Value);
            var players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();
            //tie es true cuando ha quedado mas de un personaje vivo

            if (tie)
            {
                players.showEmpate();
            }
            else
            {

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
