using UI;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using System.Collections;

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
            var players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();
            //METODO EN EL QUE SE RECORREN LOS JUGADORES Y SE PONE UN FLAG O ALGO EN EL CASO DE QUE HAYAN MUERTO
            life.Value -=10;
            print("vida: " + life.Value);
            if (life.Value <= 0)
            {
                players.alivePlayers.Value -= 1;
                destroyed.Value = true;
                DestroyCharacter();
            }

           if (players.alivePlayers.Value == 1)
            {
                print("win! ");
                StartCoroutine(Order());
            }
   
        }

        IEnumerator Order()
        {
            print("corrutina de ganar---");
            yield return new WaitForSeconds(3.0f);
           checkWinClientRpc(false);
        }


        [ServerRpc]
        public void DestroyCharacterServerRpc()
        {
            print("E");
            var players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();
             

                for (var i = this.transform.childCount - 1; i >= 0; i--)
                {

                    Destroy(this.transform.GetChild(i).gameObject);
                }
            
            
        }
        public void DestroyCharacter()
        {
            destroyed.Value = true;
            print("ESS");
            var players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();


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
