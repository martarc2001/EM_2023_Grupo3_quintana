using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace Netcode
{
    public class PlayerNetworkConfig : NetworkBehaviour
    {
        public NetworkVariable<int> life;
        public GameObject characterPrefab;
      

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            InstantiateCharacterServerRpc(OwnerClientId);
        }

        [ServerRpc]
        public void InstantiateCharacterServerRpc(ulong id)
        {
            var players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();
            players.alivePlayers.Value += 1;
            print("player nuevo! n de players: "+players.alivePlayers.Value);      
            life.Value = 100;
            GameObject characterGameObject = Instantiate(characterPrefab);
            characterGameObject.GetComponent<NetworkObject>().SpawnWithOwnership(id);
            characterGameObject.transform.SetParent(transform, false);
        }
        
        
        [ServerRpc(RequireOwnership = false)]
        public void checkLifeServerRpc()
        {
            var players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();
            //METODO EN EL QUE SE RECORREN LOS JUGADORES Y SE PONE UN FLAG O ALGO EN EL CASO DE QUE HAYAN MUERTO
            life.Value = 0;
            if (life.Value <= 0)
            {
                players.alivePlayers.Value -= 1;
                
                for (var i = this.transform.childCount - 1; i >= 0; i--)
                {
                  
                   Destroy(this.transform.GetChild(i).gameObject);
                }
            
            }
 
           

           if (players.alivePlayers.Value == 1)
            {
                print("win! ");
                checkWinClientRpc();
            }
   
        }


        [ClientRpc]
        public void checkWinClientRpc()
        {
            
           
            if (GameObject.Find("InputSystem").GetComponent<Systems.InputSystem>().Character != null)
            {
                print("has gabnado");
            

            }
            else
            {
                print("has perdido");
           
            }

        }
    }
}