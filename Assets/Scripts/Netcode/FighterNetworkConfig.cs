using Cinemachine;
using Movement.Components;
using Systems;
using Unity.Netcode;

namespace Netcode
{
    public class FighterNetworkConfig : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return; // Only the owner can access this method
            
            //FighterMovement ~ el personaje
            FighterMovement fighterMovement = GetComponent<FighterMovement>();
            InputSystem.Instance.Character = fighterMovement;

            ICinemachineCamera virtualCamera = CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera;
            virtualCamera.Follow = transform;
  
        }

        public void checkLife()
        {
            //todos los clientes         
            this.gameObject.GetComponentInParent<Netcode.PlayerNetworkConfig>().checkLifeServerRpc();
           
        }
    }
}