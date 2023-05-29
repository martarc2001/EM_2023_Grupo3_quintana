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
            if (!IsOwner) return; //Cuando spawnee, solo hará cosas respecto al personaje el propio poseedor
            
            //FighterMovement ~ el personaje
            FighterMovement fighterMovement = GetComponent<FighterMovement>();
            InputSystem.Instance.Character = fighterMovement;

            ICinemachineCamera virtualCamera = CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera;
            virtualCamera.Follow = transform;
            print("follow inicial:" + virtualCamera.Follow);
        }

        public void checkLife()
        {
            //todos los clientes         

            this.gameObject.GetComponentInParent<Netcode.PlayerNetworkConfig>().reset.Value = false;
            this.gameObject.GetComponentInParent<Netcode.PlayerNetworkConfig>().checkLifeServerRpc();
           
        }
    }
}