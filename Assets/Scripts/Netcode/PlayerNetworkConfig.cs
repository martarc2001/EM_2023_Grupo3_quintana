using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI;
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
        public NetworkVariable<int> playerNum;
        public ulong following;
       
            //public NetworkVariable<bool> isReady=new NetworkVariable<bool>(false);
      

        public ConnectedPlayers players;
        public NetworkVariable<bool> serverDespawned;

        public static PlayerNetworkConfig Instance { get; private set; }

        private void Awake() { Instance = this; 
                }
        public override void OnNetworkSpawn()
        {
            
            NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
           
           


            dead.OnValueChanged += OnDeadValueChanged;
            life.OnValueChanged += OnLifeValueChanged;
            
            playerNum = new NetworkVariable<int>(0);

            
            following = OwnerClientId;
            serverDespawned = new NetworkVariable<bool>(false);

            players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();

            ChangeMaxPlayerServerRpc();

            if (!IsOwner) return;

            Spawning();

            Invoke("ShowReadyPlayers", 1.0f);
            ConnectedPlayers.Instance.readyPlayers.OnValueChanged += (oldVal, newVal) =>
            {
                LobbyWaiting.Instance.waitingText.text = "Waiting for players " + newVal.ToString() + "/" + LobbyManager.Instance.maxPlayers + " ready";
            };


        }

        public override void OnDestroy()
        {
            if (IsClient)
            {
                Debug.Log("IsClient");
            }
            if (IsOwner)
            {
                Debug.Log("IsOwner");
            }

            if (IsServer)
            {
                Debug.Log("IsServer");
            }
            if (IsLocalPlayer)
            {
                Debug.Log("Islocalplayer");
            }
            if (IsHost)
            {
                Debug.Log("IsHost");
            }
          


        
         




        }


        private void Update()
        {
      
           
        }

        #region Lobby

        [ClientRpc]
        void ShowReadyPlayersButtonClientRpc()
        {
            
                GameObject instance = Instantiate(LobbyManager.Instance.leftLobbyMessage);
                Destroy(instance, 1f);
                LobbyWaiting.Instance.readyButton.gameObject.SetActive(true);
          
        }



        [ServerRpc(RequireOwnership = false)]
        void ChangeMaxPlayerServerRpc() { ChangeMaxPlayerClientRpc(LobbyManager.Instance.maxPlayers); }

        [ClientRpc]
        void ChangeMaxPlayerClientRpc(int players) { LobbyManager.Instance.maxPlayers = players; }

        void ShowReadyPlayers() { LobbyWaiting.Instance.waitingText.text = "Waiting for players " + ConnectedPlayers.Instance.readyPlayers.Value + "/" + LobbyManager.Instance.maxPlayers + " ready"; }

        #endregion

        #region Creating Prefab
        public void Spawning()
        {
            InstantiateOnConnectedPlayersListServerRpc();

            string prefabName = GameObject.Find("UI").GetComponent<UIHandler>().playerSprite;
            ChangeCharacterServerRpc(OwnerClientId, prefabName);

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
            characterPrefab = Instantiate(prefab, GameObject.Find("SpawnPoints").transform.GetChild(playerNum.Value).transform);
            characterPrefab.GetComponent<NetworkObject>().SpawnWithOwnership(id);
            characterPrefab.transform.SetParent(transform, false);

        }

        [ServerRpc]
        public void InstantiateOnConnectedPlayersListServerRpc()
        {
            players.allPlayers.Add(this);
            players.alivePlayers.Value += 1;
            players.player1 = this;

            dead.Value = false;

            playerNum.Value = SetPlayerNum();
            players.d_clientIdRefersToPlayerNum.Add(OwnerClientId, playerNum.Value);
            life.Value = 100;
        }

        public int SetPlayerNum()
        {
            Transform HUD = GameObject.Find("Canvas - HUD").transform;
            for (int i = 0; i < HUD.childCount; i++)
            {
                if (HUD.GetChild(i).gameObject.activeSelf)
                {
                    if (HUD.GetChild(i).Find("Disconnected").gameObject.activeSelf)
                    {
                        return i;
                    }
                }
                else return i;

            }
            return HUD.childCount;
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
            catch { } //Deletion of prefab children can affect these lines, do not remove try-catch
            finally
            {
                changeLifeInterfaceServerRpc(OwnerClientId, newValue);

            }

        }

        [ServerRpc(RequireOwnership = false)]
        public void changeLifeInterfaceServerRpc(ulong id, int newValue)
        {
            int numPlayerToUpdate = players.d_clientIdRefersToPlayerNum[id];
            changeLifeInterfaceClientRpc(newValue, numPlayerToUpdate);
        }

        [ClientRpc]
        public void changeLifeInterfaceClientRpc(int newValue, int numHealthBarToUpdate)
        {
            var healthBarToEditOnInterface = GameObject.Find("Canvas - HUD").transform.GetChild(numHealthBarToUpdate).Find("HealthBar");
            healthBarToEditOnInterface.Find("Green").GetComponent<Image>().fillAmount = (float)newValue / 100f;
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

        #endregion

        #region Getting killed
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

                if (otherPlayerObjects.Count > 0)
                {
                    //Camera follow other player
                    GameObject selectedPrefab = otherPlayerObjects[UnityEngine.Random.Range(0, otherPlayerObjects.Count)];
                    virtualCamera.Follow = selectedPrefab.transform;

                    //Changing the following property to the one theyre following (used in case the one that got killed was the one you were following)
                    following = selectedPrefab.transform.parent.GetComponent<NetworkObject>().OwnerClientId;
                }

            }

            //HUD Interface
            changeInterfaceWhenKilledServerRpc(OwnerClientId);

            //Deleting character prefab
            DestroyCharacter();
        }

        [ServerRpc(RequireOwnership = false)]
        public void changeInterfaceWhenKilledServerRpc(ulong id)
        {
            int numPlayerToUpdate = players.d_clientIdRefersToPlayerNum[id];
            changeInterfaceWhenKilledClientRpc(numPlayerToUpdate);
        }

        [ClientRpc]
        public void changeInterfaceWhenKilledClientRpc(int bannerToUpdate)
        {
            var background = GameObject.Find("Canvas - HUD").transform.GetChild(bannerToUpdate).Find("BG");
            background.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 0.35f);
            var sprite = GameObject.Find("Canvas - HUD").transform.GetChild(bannerToUpdate).Find("Sprite");
            sprite.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 0.5f);
        }

        #endregion

        #region Winning or losing
        public void DestroyCharacter()
        {
            for (var i = this.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(this.transform.GetChild(i).gameObject);
            }
        }

        IEnumerator CheckWinCoroutine()
        {
            yield return new WaitForSeconds(3.0f);
            CheckWinClientRpc(false);
        }


        [ClientRpc]
        public void CheckWinClientRpc(bool tie)
        {
            players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();
            //tie es true cuando ha quedado mas de un personaje vivo

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

            if (IsClient && !IsServer)
            {
                Debug.Log("IsClient");
                Debug.Log("Se fue del lobbyyyyyy");
                LobbyManager.Instance.LeaveLobby();
            }
            if (IsServer) {
              
               
                ConnectedPlayers.Instance.readyPlayers.Value = 0;
                LobbyWaiting.Instance.readyButton.gameObject.SetActive(true);
                GameObject instance=Instantiate(LobbyManager.Instance.leftLobbyMessage);
                Destroy(instance, 1f);
                try
                {
                    ShowReadyPlayersButtonClientRpc();
                }
                catch
                {
                    Debug.Log("No quedan clientes");
                }
              
            }
          
           
            //Showing disconnection on HUD Interface
            if (IsOwner)//We want only to enter once through this method
            {
                showDisconnectionOnInterfaceServerRpc(clientId);

                if (IsServer)
                {
                    players.alivePlayers.Value -= 1;
                    Debug.Log("ALIVEPLAYERS: " + players.alivePlayers.Value);
                }

            }

            //si el que se ha desconectado es el host
            if (!ConnectedPlayers.Instance.gameStarted) return;
            if (clientId == NetworkManager.ServerClientId) { players.showError(); }

            else//si se ha desconectado un cliente
            {
                if (IsOwner)
                {
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

        [ServerRpc]
        private void showDisconnectionOnInterfaceServerRpc(ulong clientId)
        {
            //Since it has been deleted due to disconnected, we cannot get playerNum straigh away
            //Dictionary is used to store that playerNum assigned to that client
            int disconnectedPlayerNum = players.d_clientIdRefersToPlayerNum[clientId];
            showDisconnectionOnInterfaceClientRpc(disconnectedPlayerNum);

        }
        [ClientRpc]
        private void showDisconnectionOnInterfaceClientRpc(int num)
        {
            var interfaceOfDisconnectedPlayer = GameObject.Find("Canvas - HUD").transform.GetChild(num);
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

