using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{

    public int maxPlayers;
    public Lobby joinedLobby;
    private float heartBeatTimer;
    public GameObject leftLobbyMessage;
    private const string JOIN_CODE="RelayJoinCode";

    public static LobbyManager Instance { get; private set; }

    
    private void Awake()
    {
       
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeUnityAuthentication();
    }

    void Update()
    {
        HandleLobbyHeartbeat();
     
    }

    private void HandleLobbyHeartbeat()
    {
        if (IsLobbyHost())
        {
            heartBeatTimer -= Time.deltaTime;
            if (heartBeatTimer <= 0f)
            {
                float heartBeatTimerMax = 15f;
                heartBeatTimer = heartBeatTimerMax;

                LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private bool IsLobbyHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }
    private async void InitializeUnityAuthentication()
    {
        //Si ya ha sido inicializado no lo volvemos a inicializar
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            //Si ejecutamos diferentes builds en el mismo ordenador todas tendrán el mismo Id
            //Cambiamos el profile para poder testear en diferentes builds
            InitializationOptions initializationOptions = new InitializationOptions();
            initializationOptions.SetProfile(Random.Range(0, 10000).ToString());

            await UnityServices.InitializeAsync(initializationOptions);

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private async Task<Allocation> AllocateRelay()
    {
        // Al devolver una tarea, la función AllocateRelay() permite que el código que la llama
        // continúe ejecutándose mientras se espera a que se complete la asignación del servidor
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            //Crea una asignacion a un servidor, el parámetro de entrada es el numero de personas que 
            //se espera que se conecten al servidor
            return allocation;
        }catch(RelayServiceException e)
        {
            Debug.Log(e);
            return default;
        }


    }

    private async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        try
        {
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return relayJoinCode;

        }catch(RelayServiceException e)
        {
            Debug.Log(e);
            return default;
        }
    }

    private async Task<JoinAllocation> JoinRelay (string joinCode)
    {
        try
        {
            JoinAllocation jAllocation=await RelayService.Instance.JoinAllocationAsync(joinCode);

            return jAllocation;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return default;
        }
    }
    public async void CreateLobby(string lobbyName,bool isPrivate)
    {
        try
        {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
            });

            if (isPrivate) {
                InfoLobby.Instance.ShowInfo(lobbyName, joinedLobby.LobbyCode);
            }

            //Levantamos el Allocation
            //El jugador se conecta al servicio de Relay y te conecta con otros jugadores
            Allocation allocation=await AllocateRelay();
            string relayJoinCode = await GetRelayJoinCode(allocation);

            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { JOIN_CODE,new DataObject(DataObject.VisibilityOptions.Member,relayJoinCode) }
                }
            });

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation,"dtls"));

            UIHandler.Instance.InstantiateHost();
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
       
    }

    public async void QuickJoin()
    {
        
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots,"0",QueryFilter.OpOptions.GT)
                    
                    //Un filtro para que devuelva las lobbies que tienen al menos un hueco
                },

            };
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);//nos devuelve la info de las lobbies creadas

            if (queryResponse.Results.Count != 0)
            {
                joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

                string relayJoinCode = joinedLobby.Data[JOIN_CODE].Value;
                JoinAllocation joinAllocation= await JoinRelay(relayJoinCode);

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));


                //Instanciamos un cliente 
                //NetworkManager.Singleton.StartClient();
                UIHandler.Instance.InstantiateClient();
                LobbyJoinUI.Instance.gameObject.SetActive(false);
            }
            else
            {
                LobbyJoinUI.Instance.ShowIssue();
                           }

        
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    public async void JoinWithCode(string lobbyCode)
    {
      
        try
        {
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

            //Instanciamos un cliente 
            //NetworkManager.Singleton.StartClient();

          

            string relayJoinCode = joinedLobby.Data[JOIN_CODE].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));


            UIHandler.Instance.InstantiateClient();
                LobbyJoinUI.Instance.gameObject.SetActive(false);
         
               
                
           

        }
        catch(LobbyServiceException e)
        {
            LobbyJoinUI.Instance.ShowIssue();
            Debug.Log(e);
        }
    }

    public async void DeleteLobby()
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
                joinedLobby = null;
            }
            catch(LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public Lobby GetLobby()
    {
        return joinedLobby;
    }

   public async void LeaveLobby()
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joinedLobby = null;

            }catch(LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }


}
