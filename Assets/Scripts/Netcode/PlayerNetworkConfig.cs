using UI;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using System.Collections;
using Cinemachine;
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
       
        public static PlayerNetworkConfig Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }
        public override void OnNetworkSpawn()
        {
            life.OnValueChanged += OnLifeValueChanged;

            if (!IsOwner) return;
            //InstantiateCharacterServerRpc(OwnerClientId);
            Spawning();

            Invoke("ShowReadyPlayers", 1.0f);

            ConnectedPlayers.Instance.readyPlayers.OnValueChanged += (oldVal, newVal) =>
            {
                LobbyWaiting.Instance.waitingText.text = "Waiting for players " + ConnectedPlayers.Instance.readyPlayers.Value + "/4 ready";
            };
          

        }

        void ShowReadyPlayers() {
            LobbyWaiting.Instance.waitingText.text = "Waiting for players " + ConnectedPlayers.Instance.readyPlayers.Value + "/4 ready";
        }

        public void Spawning()
        {
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
            life.Value -= 20;

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

        public void DestroyCharacter(Transform t)
        {
            destroyed.Value = true;
            print("ESS");
            var players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();
            try
            {
                ICinemachineCamera virtualCamera = CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera;
                virtualCamera.Follow = t;
            }
            catch (System.Exception ex)
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
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            characterPrefab = Instantiate(prefab, GameObject.Find("SpawnPoints").transform.GetChild((int)OwnerClientId).transform);
            characterPrefab.GetComponent<NetworkObject>().SpawnWithOwnership(id);
            characterPrefab.transform.SetParent(transform, false);


            var players = GameObject.Find("Players").GetComponent<ConnectedPlayers>();
            players.alivePlayers.Value += 1;

            players.player1 = this;

            print("player nuevo! n de players: " + players.alivePlayers.Value);
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
