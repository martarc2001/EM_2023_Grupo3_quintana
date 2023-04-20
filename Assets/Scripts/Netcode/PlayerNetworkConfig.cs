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
        public NetworkVariable<bool> destroyed;
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

            GameObject.Find("Players").GetComponent<ConnectedPlayers>().player1 = this;

            print("player nuevo! n de players: "+players.alivePlayers.Value);
            destroyed.Value = false;

            if (players.alivePlayers.Value > 1)
            {
                life.Value = 2;
            }
            else {
                life.Value = 100;
            }

   
            GameObject characterGameObject = Instantiate(characterPrefab);
            characterGameObject.GetComponent<NetworkObject>().SpawnWithOwnership(id);
            characterGameObject.transform.SetParent(transform, false);
            

            //contador
         
            players.allPlayers.Add(this);
     
          
           
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
                destroyed.Value = true;
                DestroyCharacter();
            }

           if (players.alivePlayers.Value == 1)
            {
                print("win! ");
              checkWinClientRpc(false);
            }
   
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

                if (destroyed.Value == false)
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