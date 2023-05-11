using UI;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using System.Collections;
using Cinemachine;
using TMPro;
using UnityEngine.UI;
using Unity.Networking.Transport;
using System.Collections.Generic;

namespace Netcode
{
    public class PlayerNetworkConfig : NetworkBehaviour
    {

        public NetworkVariable<int> life;
        public GameObject characterPrefab;
        public NetworkVariable<bool> destroyed;
        public ConnectedPlayers players;
        public NetworkVariable<bool> serverDespawned;
        private bool despawned;

        public static PlayerNetworkConfig Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }
        public override void OnNetworkSpawn()
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
            despawned = false;
            serverDespawned = new NetworkVariable<bool>(false);
            life.OnValueChanged += OnLifeValueChanged;
            players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();
          
           
            ChangeMaxPlayerServerRpc();
            
            if (!IsOwner) return;
            //InstantiateCharacterServerRpc(OwnerClientId);
            Spawning();

            Invoke("ShowReadyPlayers", 1.0f);

            ConnectedPlayers.Instance.readyPlayers.OnValueChanged += (oldVal, newVal) =>
            {
                LobbyWaiting.Instance.waitingText.text = "Waiting for players " + newVal.ToString() + "/"+LobbyManager.Instance.maxPlayers + " ready";
            };


        }

        [ServerRpc]
        void ChangeMaxPlayerServerRpc() {
            ChangeMaxPlayerClientRpc(LobbyManager.Instance.maxPlayers);
        }


        [ClientRpc]
        void ChangeMaxPlayerClientRpc(int players)
        {
            LobbyManager.Instance.maxPlayers = players;
        }

        void ShowReadyPlayers()
        {
            LobbyWaiting.Instance.waitingText.text = "Waiting for players " + ConnectedPlayers.Instance.readyPlayers.Value + "/" + LobbyManager.Instance.maxPlayers + " ready";
        }

        public void Spawning()
        {
            string prefabName = GameObject.Find("UI").GetComponent<UIHandler>().playerSprite;
            ChangeCharacterServerRpc(OwnerClientId, prefabName);
        }
        //cuando alguien se desconecta se llama a este metodo
        private void Singleton_OnClientDisconnectCallback(ulong clientId)
        {

            players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();
            //si el que se ha desconectado es el host
            if (clientId == NetworkManager.ServerClientId)
            {
                players.showError();
            }
            else//si se ha desconectado un cliente
            {
                if (IsServer)//si el que está ejecutando el método es el host, se comprueba si ha quedado mas de uno vivo
                {
                    try {

                        StartCoroutine(wait());
                    }
                    catch { }
                }
            }
            base.OnNetworkDespawn();
        }
        IEnumerator wait()
        {
            //espera un segundo para que se actualice el connecteclientslist - cuando hay uno vivo se hacen los métodos de calcular, mostrar el ganador, y mostrar a los clientes quien gana y quien pierde
            yield return new WaitForSeconds(1.0f);

            if (NetworkManager.Singleton.ConnectedClientsList.Count == 1)
            {
                players.calculateWinner();
                StartCoroutine(Order());//corrutina que espera unos segundos y muestra quien ha ganado

            }

        }

        public void Update()
        {
            if (despawned == true)
            {
                serverDespawned.Value = despawned;
            }

        }



        [ServerRpc]
        public void InstantiateCharacterServerRpc(ulong id)
        {
            GameObject characterGameObject = Instantiate(characterPrefab);
            characterGameObject.GetComponent<NetworkObject>().SpawnWithOwnership(id); //Mirar SpawnAsPlayerObject -- NO USAR, INESTABLE
            //characterGameObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(id); //Si spawneamos as�, el propio jugador es PlayerPrefab pero el resto de clientes los recibe como HuntressPrefab
            characterGameObject.transform.SetParent(transform, false);


        }


        [ServerRpc(RequireOwnership = false)]
        public void checkLifeServerRpc()
        {
            life.Value -= 20;

            if (life.Value <= 0)
            {



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


        private void OnLifeValueChanged(int oldValue, int newValue)
        {
            var healthBarToEdit = transform.GetChild(0).Find("HUD").Find("HealthBar");
            healthBarToEdit.Find("Green").GetComponent<Image>().fillAmount = (float)newValue / 100f;

            var healthBarToEditOnInterface = GameObject.Find("Canvas - HUD").transform.GetChild((int)OwnerClientId).Find("HealthBar");
            healthBarToEditOnInterface.Find("Green").GetComponent<Image>().fillAmount = (float)newValue / 100f;
        }




        [ClientRpc]
        public void UpdateHealthBarClientRpc(float fillAmount)
        {
            transform.GetChild(0).Find("HUD").Find("HealthBar").Find("Green").GetComponent<Image>().fillAmount = fillAmount;
        }

    

    IEnumerator Order()
    {

        print("corrutina de ganar---");
        yield return new WaitForSeconds(3.0f);

        checkWinClientRpc(false);

    }



    public void DestroyCharacter(bool timeout)
    {

        try
        {

            for (var i = this.transform.childCount - 1; i >= 0; i--)
            {
                print("child");
                Destroy(this.transform.GetChild(i).gameObject);
            }
            //alomejor corruitna aqui?
            //moveCameraClientRpc(timeout);
        }
        catch (System.Exception ex)
        {
            print(ex);
        }

    }


    [ServerRpc]
    public void ChangeCharacterServerRpc(ulong id, string prefabName)
    {
        GameObject prefab = Resources.Load<GameObject>(prefabName);
        characterPrefab = Instantiate(prefab, GameObject.Find("SpawnPoints").transform.GetChild((int)OwnerClientId).transform);
        characterPrefab.GetComponent<NetworkObject>().SpawnWithOwnership(id);
        characterPrefab.transform.SetParent(transform, false);


        players.allPlayers.Add(this);
        players.alivePlayers.Value += 1;

        players.player1 = this;

        print("player nuevo! n de players: " + players.alivePlayers.Value);
        destroyed.Value = false;



        life.Value = 100;

        print(GameObject.Find("Players").GetComponent<ConnectedPlayers>().player1);
        print(GameObject.Find("Players").GetComponent<ConnectedPlayers>().player1.life.Value);
    }





    [ClientRpc]
    public void checkWinClientRpc(bool tie)
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

