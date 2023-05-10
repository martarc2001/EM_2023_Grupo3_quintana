using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI;
using Unity.Netcode;
using UnityEngine;

namespace Netcode
{
    public class PlayerNetworkConfig : NetworkBehaviour
    {
        public NetworkVariable<int> life;
        public NetworkVariable<bool> dead;

        ConnectedPlayers players;
        private void Start()
        {
            players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();
        }

        public override void OnNetworkSpawn()
        {
            dead.OnValueChanged += OnDeadValueChanged;

            if (!IsOwner) return;
            //InstantiateCharacterServerRpc(OwnerClientId);

            SetSpawnPositionServerRpc((int)OwnerClientId);

            string prefabName = GameObject.Find("UI").GetComponent<UIHandler>().playerSprite;
            ChangeCharacterServerRpc(OwnerClientId, prefabName);

            InstantiateOnConnectedPlayersListServerRpc();


        }


        #region Creating Prefab

        [ServerRpc]
        public void SetSpawnPositionServerRpc(int thisClientId)
        {
            Vector3 spawnPosition = GameObject.Find("SpawnPoints").transform.GetChild(thisClientId).transform.position;
            transform.SetPositionAndRotation(spawnPosition, transform.rotation);
        }

        [ServerRpc]
        public void ChangeCharacterServerRpc(ulong id, string prefabName)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            GameObject characterGameObject = Instantiate(prefab);
            characterGameObject.GetComponent<NetworkObject>().SpawnWithOwnership(id);
            characterGameObject.transform.SetParent(transform, false);

        }

        [ServerRpc]
        public void InstantiateOnConnectedPlayersListServerRpc()
        {
            var players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();
            players.alivePlayers.Value += 1;

            players.player1 = this;

            print("player nuevo! n de players: " + players.alivePlayers.Value);
            dead.Value = false;

            life.Value = 100;

            players.allPlayers.Add(this);


        }

        #endregion

        #region Life values and getting killed
        [ServerRpc(RequireOwnership = false)]
        public void checkLifeServerRpc()
        {

            life.Value -= 50;

            if (life.Value <= 0)
            {
                dead.Value = true;
                players.alivePlayers.Value -= 1;
                if (players.alivePlayers.Value == 1) { StartCoroutine(CheckWinCoroutine()); }
            }

        }

        

        /// <summary>
        /// Used to change the camera to other player prefab when getting killed, also destroying its prefab
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        private void OnDeadValueChanged(bool oldValue, bool newValue)
        {
            NetworkObject deadPrefab = GetComponent<NetworkObject>();
            if (deadPrefab.OwnerClientId == NetworkManager.Singleton.LocalClientId) //Only enters if condition if you own the dead prefab, necessary since everybody has this prefab owned or not
            {

                GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("PlayerPrefab");
                List<GameObject> otherPlayerObjects = playerObjects.Where(obj => obj.GetComponent<NetworkObject>().OwnerClientId != NetworkManager.Singleton.LocalClientId).ToList(); //This list contains the rest of the prefabs, not the one that just died
                                
                if (otherPlayerObjects.Count > 0)
                {                    
                    GameObject selectedPrefab = otherPlayerObjects[Random.Range(0, otherPlayerObjects.Count)];
                    
                    ICinemachineCamera virtualCamera = CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera;
                    virtualCamera.Follow = selectedPrefab.transform;
                }

            }
            DestroyCharacter();
        }

        public void DestroyCharacter()
        {
            Destroy(this.transform.GetChild(0).gameObject);
        }


        IEnumerator CheckWinCoroutine()
        {
            yield return new WaitForSeconds(3.0f);
            CheckWinClientRpc(false);
        }



        [ClientRpc]
        public void CheckWinClientRpc(bool tie)
        {
            print(this);
            print(dead.Value);
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

        #endregion
    }
}
