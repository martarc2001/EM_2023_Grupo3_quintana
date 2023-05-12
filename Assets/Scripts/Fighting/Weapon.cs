using Movement.Components;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace Fighting
{
    public class Weapon : NetworkBehaviour
    {
        public Animator effectsPrefab;
        private static readonly int Hit03 = Animator.StringToHash("hit03");

        private void OnCollisionEnter2D(Collision2D collision)
        {
            print("the collision is collisioning");
            GameObject otherObject = collision.gameObject;
            // Debug.Log($"Sword collision with {otherObject.name}");

            //Instancia la animación del efecto de corte hit03 en el 1er punto de contacto
            Animator effect = Instantiate(effectsPrefab); 
            effect.transform.position = collision.GetContact(0).point;
            effect.SetTrigger(Hit03);
            effectsClientRpc(effect.transform.position);
            // TODO: Review if this is the best way to do this
            IFighterReceiver enemy = otherObject.GetComponent<IFighterReceiver>();
            if(enemy != null )
                enemy.TakeHit();
        }

        [ClientRpc]
        private void effectsClientRpc(Vector3 pos)
        {
            Animator effect = Instantiate(effectsPrefab);
            effect.transform.position = pos;
            effect.SetTrigger(Hit03);
            //Debug.Log("chiribitas :3");
        }
    }
}
