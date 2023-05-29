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

    public int maxPlayers;//Numero maximo de jugadores establecido por el host del lobby
    public Lobby joinedLobby;//La lobby a la que te has unido/la que has creado
    private float heartBeatTimer;//Cada cierto tiempo se ejecuta un código para indicar que el lobby sigue activo
    public GameObject leftLobbyMessage;//prefab que instanciamos cuando alguien se sale del lobby
    private const string JOIN_CODE="RelayJoinCode";//El codigo para unirse al Relay
    
    public static LobbyManager Instance { get; private set; }
    //Instancia estatica para acceder desde otras clases
    
    private void Awake()
    {
       
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeUnityAuthentication();//Inicializar UnityServices y acceso
    }

    void Update()
    {
        HandleLobbyHeartbeat();//Para que el lobby reciba logica que ejecutar cada x tiempo
     
    }

    private void HandleLobbyHeartbeat()//Si el lobby no hace nada en 30s se cierra por ello se crea esta función
    {
        if (IsLobbyHost())//Si es el host del lobby
        {
            heartBeatTimer -= Time.deltaTime;//Restamos al timer
            if (heartBeatTimer <= 0f)//Cuando sea 0
            {
                float heartBeatTimerMax = 15f;//Lo reiniciamos
                heartBeatTimer = heartBeatTimerMax;

                LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);//Envía un mensaje al servidor para indicar que el lobby está activo
            }
        }
    }

    public bool IsLobbyHost()//Metodo que te devuelve si eres o no el host de la lobby
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

            await UnityServices.InitializeAsync(initializationOptions);//Inicializamos UnityServices

            await AuthenticationService.Instance.SignInAnonymouslyAsync();//Iniciamos sesion en la aplicación anonimamente, el ususario
            //no necesita iniciar sesión para acceder
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
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);//Te proporciona un codigo para unirte
            //con otros usuarios sin necesidad de conocer las Ips
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
            JoinAllocation jAllocation=await RelayService.Instance.JoinAllocationAsync(joinCode);//Se usa para unirse a un lobby determinado
            //identificado por un código creado anteriormente

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
            //Creamos una lobby, le metemos el nombre, numero de jugadores e indicamos si es publica o privada
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
            });

            
            if (isPrivate) {//Si es privada
                InfoLobby.Instance.ShowInfo(lobbyName, joinedLobby.LobbyCode);//Le mostramos al host el codig
            }

            //Levantamos el Allocation
            //El jugador se conecta al servicio de Relay y te conecta con otros jugadores
            Allocation allocation=await AllocateRelay();
            string relayJoinCode = await GetRelayJoinCode(allocation);

            //Usamos esto para "conectar" el código del Relay con los diferentes jugadores que se unan a la lobby
            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { JOIN_CODE,new DataObject(DataObject.VisibilityOptions.Member,relayJoinCode) }
                }
            });


            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation,"dtls"));
            //Esta linea de codigo accede al componente UnityTransport que se utiliza para gestionar la conexión en red 
            //Se crea un nuevo relayServerData que configura los datos del servidor relay pasando como parámetros de entrada
            //La asignación al servidor relay y el protocolo de seguridad de transporte que se usará

            //Y tras esto, instanciamos el host
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

            if (queryResponse.Results.Count != 0)//si hay alguna lobby con huecos vacios
            {
                joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();//Te unes

                string relayJoinCode = joinedLobby.Data[JOIN_CODE].Value;//Accedemos al código del server Relay
                JoinAllocation joinAllocation= await JoinRelay(relayJoinCode);//Nos unimos a él

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
                //establecemos el RelayServerData


                //Instanciamos un cliente 
                UIHandler.Instance.InstantiateClient();
                LobbyJoinUI.Instance.gameObject.SetActive(false);//Ocultamos el menu de elejir
            }
            else
            {
                LobbyJoinUI.Instance.ShowIssue();//Si no hay lobbies disponibles lo decimos
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
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);//Si es una lobby privada nos unimos con un código


            //Accedemos al código del server Relay
            string relayJoinCode = joinedLobby.Data[JOIN_CODE].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));


            UIHandler.Instance.InstantiateClient();
                LobbyJoinUI.Instance.gameObject.SetActive(false);
         
               
                
           

        }
        catch(LobbyServiceException e)
        {
            LobbyJoinUI.Instance.ShowIssue();//Si da un error se muestra
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


    public async void KickPlayer(string playerId)
    {
        if (IsLobbyHost())
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
               
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

}
