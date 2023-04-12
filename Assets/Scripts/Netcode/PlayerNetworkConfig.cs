using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace Netcode
{
    public class PlayerNetworkConfig : NetworkBehaviour
    {
        public GameObject characterPrefab;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            InstantiateCharacterServerRpc(OwnerClientId);
        }
    
        [ServerRpc]
        public void InstantiateCharacterServerRpc(ulong id)
        {
            GameObject characterGameObject = Instantiate(characterPrefab);
            characterGameObject.GetComponent<NetworkObject>().SpawnWithOwnership(id); //Mirar SpawnAsPlayerObject
            characterGameObject.transform.SetParent(transform, false);
        }
    }
}

//https://docs-multiplayer.unity3d.com/netcode/current/basics/networkobject/index.html#creating-a-playerobject
//https://docs-multiplayer.unity3d.com/netcode/current/basics/networkobject/index.html#ownership

//Netcode for GameObjects is server-authoritative,
//which means the server controls (the only system authorized) spawning and despawning NetworkObjects.


/*
SpawnWithOwnership spawns an object with ownership assigned to the server.
This means that the server will be the first owner of the object and can
then transfer ownership to a client if necessary.

SpawnAsPlayerObject spawns an object with ownership assigned to a specific player.
This means that the player specified will be the first owner of the object.
//If the player already had a Prefab instance assigned,
//then the client owns the NetworkObject of that Prefab instance
//unless there's additional server-side specific user code that removes or changes the ownership.


In other words, SpawnWithOwnership is more general and allows for ownership transfer,
while SpawnAsPlayerObject is more specific and assigns ownership to a particular player right away.
*/