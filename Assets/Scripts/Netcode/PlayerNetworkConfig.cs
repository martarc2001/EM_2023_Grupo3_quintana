using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Netcode
{
    public class PlayerNetworkConfig : NetworkBehaviour
    {
        public GameObject characterPrefab;
        public NetworkVariable<int> life;
        public NetworkVariable<bool> dead;//destroyed
        public NetworkVariable<bool> reset;
        public ulong following;
        public string charName;

        public ConnectedPlayers players;
        public NetworkVariable<bool> serverDespawned;
        public NetworkVariable<FixedString32Bytes> serverCharName;
        public static PlayerNetworkConfig Instance { get; private set; }

        private void Awake() { Instance = this; }
        public override void OnNetworkSpawn()
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
            dead.OnValueChanged += OnDeadValueChanged;
            life.OnValueChanged += OnLifeValueChanged;
            reset = new NetworkVariable<bool>(false);
            following = OwnerClientId;
            serverDespawned = new NetworkVariable<bool>(false);

            players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();

            ChangeMaxPlayerServerRpc();

            if (!IsOwner) return;
            string prefabName = GameObject.Find("UI").GetComponent<UIHandler>().playerSprite;
            charName = prefabName;
           
            Spawning();

            Invoke("ShowReadyPlayers", 1.0f);
            ConnectedPlayers.Instance.readyPlayers.OnValueChanged += (oldVal, newVal) =>
            {
                LobbyWaiting.Instance.waitingText.text = "Waiting for players " + newVal.ToString() + "/" + LobbyManager.Instance.maxPlayers + " ready";
            };


        }

       

            #region Lobby
            [ServerRpc(RequireOwnership = false)]
        void ChangeMaxPlayerServerRpc() { ChangeMaxPlayerClientRpc(LobbyManager.Instance.maxPlayers); }

        [ClientRpc]
        void ChangeMaxPlayerClientRpc(int players) { LobbyManager.Instance.maxPlayers = players; }

        void ShowReadyPlayers() { LobbyWaiting.Instance.waitingText.text = "Waiting for players " + ConnectedPlayers.Instance.readyPlayers.Value + "/" + LobbyManager.Instance.maxPlayers + " ready"; }

        #endregion

        #region Creating Prefab
        public void Spawning()
        {
           
            ChangeCharacterServerRpc(OwnerClientId, charName);
            InstantiateOnConnectedPlayersListServerRpc();

        }

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
            characterPrefab = Instantiate(prefab, GameObject.Find("SpawnPoints").transform.GetChild((int)OwnerClientId).transform);
            characterPrefab.GetComponent<NetworkObject>().SpawnWithOwnership(id);
            characterPrefab.transform.SetParent(transform, false);

        }
        [ClientRpc]
       public void resetplayerClientRpc(bool value)
        {
            print("hii");
            reset.Value = value;
         //   life.Value = 100;
            print("resetplayeer"+reset.Value);
        }

        [ServerRpc]
        public void InstantiateOnConnectedPlayersListServerRpc()
        {
            serverCharName = new NetworkVariable<FixedString32Bytes>(charName);

            players.allPlayers.Add(this);
            players.alivePlayers.Value += 1;

            players.player1 = this;

            print("player nuevo! n de players: " + players.alivePlayers.Value);
            dead.Value = false;

            life.Value = 100;


        }

        #endregion

        #region Life values and getting killed
        private void OnLifeValueChanged(int oldValue, int newValue)
        {
            try
            {
                var healthBarToEdit = transform.GetChild(0).Find("HUD").Find("HealthBar");
                healthBarToEdit.Find("Green").GetComponent<Image>().fillAmount = (float)newValue / 100f;
            }
            catch (Exception ex) { } //Deletion of prefab children can affect these lines, do not remove try-catch
            finally
            {
                var healthBarToEditOnInterface = GameObject.Find("Canvas - HUD").transform.GetChild((int)OwnerClientId).Find("HealthBar");
                healthBarToEditOnInterface.Find("Green").GetComponent<Image>().fillAmount = (float)newValue / 100f;
            }

        }

        [ClientRpc]
        public void UpdateHealthBarClientRpc(float fillAmount)
        {
            transform.GetChild(0).Find("HUD").Find("HealthBar").Find("Green").GetComponent<Image>().fillAmount = fillAmount;
        }

        [ServerRpc(RequireOwnership = false)]
        public void checkLifeServerRpc()
        {
            
            life.Value -= 20;

            if (life.Value <= 0)
            {
                
                dead.Value = true;
                players.alivePlayers.Value -= 1;

                if (players.alivePlayers.Value == 1)
                {
                    ConnectedPlayers.Instance.gameStarted = false;
                    //para calcular quién ha ganado se mira qué jugador queda con mayor vida y se coloca en un networkvariable
                    players.calculateWinner();
                    StartCoroutine(CheckWinCoroutine());

                }
            }

        }

      
      


        /// <summary>
        /// Used to change the camera to other player prefab when getting killed, also destroying its prefab
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        private void OnDeadValueChanged(bool oldValue, bool newValue)
        {
            //Changing the camera to other player
            NetworkObject deadPrefab = GetComponent<NetworkObject>();
            ICinemachineCamera virtualCamera = CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera;

            bool isKilled = deadPrefab.OwnerClientId == NetworkManager.Singleton.LocalClientId; //Only enters if condition if you own the dead prefab, necessary since everybody has this prefab owned or not
            bool isFollower = deadPrefab.OwnerClientId == NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkConfig>().following; //A prefab can also enter if they were following the one that got killed
            if (isKilled || isFollower)
            {
                GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("PlayerPrefab");
                List<GameObject> otherPlayerObjects = playerObjects.Where(obj => obj.GetComponent<NetworkObject>().OwnerClientId != NetworkManager.Singleton.LocalClientId).ToList(); //This list contains the rest of the prefabs, not the one that just died

                if (otherPlayerObjects.Count > 0&& !reset.Value)
                {
                    //Camera follow other player
                    GameObject selectedPrefab = otherPlayerObjects[UnityEngine.Random.Range(0, otherPlayerObjects.Count)];
                    virtualCamera.Follow = selectedPrefab.transform;
                    print("follow mal:" + virtualCamera.Follow);

                    //Changing the following property to the one theyre following (used in case the one that got killed was the one you were following)
                    following = selectedPrefab.transform.parent.GetComponent<NetworkObject>().OwnerClientId;
                }
                
            }
            if (!reset.Value)
            {

          

            //HUD Interface
            var background = GameObject.Find("Canvas - HUD").transform.GetChild((int)OwnerClientId).Find("BG");
            background.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 0.35f);
            var sprite = GameObject.Find("Canvas - HUD").transform.GetChild((int)OwnerClientId).Find("Sprite");
            sprite.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 0.5f);
                print("cambiando a gris");
            }
            //Deleting character prefab
            DestroyCharacter();
        }

        public void DestroyCharacter()
        {
            if (IsServer)
            {

            
            for (var i = this.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(this.transform.GetChild(i).gameObject);
            }
            }
        }

        IEnumerator CheckWinCoroutine()
        {
            yield return new WaitForSeconds(3.0f);
            CheckWinClientRpc(false);
        }
        IEnumerator RestartCoroutine()
        {
            players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();
            yield return new WaitForSeconds(5.0f);
           players.RestartServerRpc();
            

        }
       

        [ClientRpc]
        public void CheckWinClientRpc(bool tie)
        {
            players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();
            //tie es true cuando ha quedado mas de un personaje vivo
            if (IsServer)
            {
              
                StartCoroutine(RestartCoroutine());
            }
         
            if (tie)
            {
                players.showEmpate();
            }
            else
            {
                //showwinner tiene que ser clientrpc porque también se llama cuando acaba el contador, que se hace en el server
                players.showWinnerClientRpc();

                if (GameObject.Find("InputSystem").GetComponent<Systems.InputSystem>().Character != null)
                {
                    print("has ganado");
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

        #region Handling Disconnection

        //cuando alguien se desconecta se llama a este metodo
        private void Singleton_OnClientDisconnectCallback(ulong clientId)
        {
            LobbyManager.Instance.LeaveLobby();

            //si el que se ha desconectado es el host
            if (!ConnectedPlayers.Instance.gameStarted) return;
            if (clientId == NetworkManager.ServerClientId) { players.showError(); }

            else//si se ha desconectado un cliente
            {
                if (IsOwner)
                {
                    //Showing disconnection on HUD Interface
                    showDisconnectionOnInterfaceClientRpc(clientId);

                    if (IsServer)//si el que está ejecutando el método es el host, se comprueba si ha quedado mas de uno vivo
                    {
                        try
                        {
                            //solo se calcula el ganador cuando la partida ha empezado
                            if (ConnectedPlayers.Instance.gameStarted) { StartCoroutine(wait()); }
                        }
                        catch (System.Exception ex) { print(ex); }
                    }

                }
            }
            base.OnNetworkDespawn();
        }

        [ClientRpc]
        private void showDisconnectionOnInterfaceClientRpc(ulong clientId)
        {
            var interfaceOfDisconnectedPlayer = GameObject.Find("Canvas - HUD").transform.GetChild((int)clientId);
            interfaceOfDisconnectedPlayer.Find("Disconnected").gameObject.SetActive(true);
        }

        IEnumerator wait()
        {
            //espera un segundo para que se actualice el connecteclientslist - cuando hay uno vivo se hacen los métodos de calcular, mostrar el ganador, y mostrar a los clientes quien gana y quien pierde
            yield return new WaitForSeconds(1.0f);

            if (NetworkManager.Singleton.ConnectedClientsList.Count == 1)
            {
                players.calculateWinner();
                StartCoroutine(CheckWinCoroutine());//corrutina que espera unos segundos y muestra quien ha ganado

            }

        }

        #endregion
    }
}

