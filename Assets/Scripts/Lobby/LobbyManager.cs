using System.Collections;
using System.Collections.Generic;
using UI;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{

    public int maxPlayers;
    public Lobby joinedLobby;
    private float heartBeatTimer;
    public GameObject leftLobbyMessage;

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

            //Levantamos el Host al crear la sala
            //NetworkManager.Singleton.StartHost();
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
