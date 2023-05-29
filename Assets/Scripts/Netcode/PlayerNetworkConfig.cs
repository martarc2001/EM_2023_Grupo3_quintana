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
        public NetworkVariable<int> playerNum;
        public ulong following;
        public NetworkVariable<bool> reset;
        public string charName;
        public ConnectedPlayers players;
        public NetworkVariable<bool> serverDespawned;
        public NetworkVariable<FixedString32Bytes> serverCharName;
        public static PlayerNetworkConfig Instance { get; private set; }

        private void Awake() { Instance = this; 
                }
        public override void OnNetworkSpawn()
        {
            //when the player spawns in the game
            NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;

            dead.OnValueChanged += OnDeadValueChanged;
            life.OnValueChanged += OnLifeValueChanged;
            
            playerNum = new NetworkVariable<int>(0);

            reset = new NetworkVariable<bool>(false);
            //id of the character followed by the player camera
            following = OwnerClientId;
            serverDespawned = new NetworkVariable<bool>(false);
            //reference to the script "connectedplayers", used to manage server logic
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
            ChangeCharacterServerRpc(OwnerClientId, charName);

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
            serverCharName = new NetworkVariable<FixedString32Bytes>(charName);

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
        //serverrpc that activates when a player is hurt. It changes its life and checks if the game should end 

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
                    //calculate the winner and wait for a few seconds, then execute the checkwin method
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

                if (otherPlayerObjects.Count > 0 && newValue == true)
                {
                    //Camera follow other player
                    GameObject selectedPrefab = otherPlayerObjects[UnityEngine.Random.Range(0, otherPlayerObjects.Count)];
                    virtualCamera.Follow = selectedPrefab.transform;


                    //Changing the following property to the one theyre following (used in case the one that got killed was the one you were following)
                    following = selectedPrefab.transform.parent.GetComponent<NetworkObject>().OwnerClientId;
                }

            }
            if (newValue == true)
            {

                //HUD Interface
                changeInterfaceWhenKilledServerRpc(OwnerClientId);

                //Deleting character prefab
                DestroyCharacter();
            }
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
        //detroys the prefab and ui of the character
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
        //waits for 3 seconds because it needs time to detect wich character was destroyed
        IEnumerator CheckWinCoroutine()
        {
            yield return new WaitForSeconds(3.0f);
            CheckWinClientRpc(false);
        }
        //waits for five seconds so the players can see the you win/you loose message for a bit before restarting the game
        IEnumerator RestartCoroutine()
        {
            players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();
            yield return new WaitForSeconds(5.0f);
            players.RestartServerRpc();


        }
        //checks wich player has won and wich one has won. It shows a message on screen as well as the name of the winner

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
                ///showwinner has to be a clientrpc even if its called by a clientrpc already because it's also called by endserver, wich is a serverrpc method
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


        //method called when someone disconnects
        private void Singleton_OnClientDisconnectCallback(ulong clientId)
        {
            if (IsLocalPlayer)
            {
                LobbyManager.Instance.LeaveLobby();
            }

            if (IsServer)
            {
                ConnectedPlayers.Instance.readyPlayers.Value = 0;
                LobbyWaiting.Instance.readyButton.gameObject.SetActive(true);
                GameObject instance = Instantiate(LobbyManager.Instance.leftLobbyMessage);
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

            //if the person disconnected is the host, it shows an error to all the clients. Only if the game has started though.

            if (clientId == NetworkManager.ServerClientId) { players.showError(); }

            else
            {
                if (IsOwner)
                {
                    if (IsServer)
                    {
                        try
                        {
                            //if the game has started, it checks if there's only one player left with a coroutine.
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
        {//waits for one second to make sure the connectedclientslist has been updated correctly. If it has only one player left, it calculates the winner.
            yield return new WaitForSeconds(1.0f);

            if (NetworkManager.Singleton.ConnectedClientsList.Count == 1)
            {
                players.calculateWinner();
                StartCoroutine(CheckWinCoroutine());//shows the winner
            }

        }

        #endregion
    }
}

