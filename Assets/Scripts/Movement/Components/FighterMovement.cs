using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Serialization;


namespace Movement.Components
{
    [RequireComponent(typeof(Rigidbody2D)),
     RequireComponent(typeof(Animator)),
     RequireComponent(typeof(NetworkObject))]
    public sealed class FighterMovement : NetworkBehaviour, IMoveableReceiver, IJumperReceiver, IFighterReceiver
    {
        public float speed = 1.0f;
        public float jumpAmount = 1.0f;

        private Rigidbody2D _rigidbody2D;
        private Animator _animator;
        private NetworkAnimator _networkAnimator;
        private Transform _feet;
        private LayerMask _floor;

        ///Nuevo
        //public HealthBar barraDeVida;
        ///

        private Vector3 _direction = Vector3.zero;
        private bool _grounded = true;

        private Netcode.FighterNetworkConfig player;

        private static readonly int AnimatorSpeed = Animator.StringToHash("speed");
        private static readonly int AnimatorVSpeed = Animator.StringToHash("vspeed");
        private static readonly int AnimatorGrounded = Animator.StringToHash("grounded");
        private static readonly int AnimatorAttack1 = Animator.StringToHash("attack1");
        private static readonly int AnimatorAttack2 = Animator.StringToHash("attack2");
        private static readonly int AnimatorHit = Animator.StringToHash("hit");
        private static readonly int AnimatorDie = Animator.StringToHash("die");


        //private NetworkVariable<int> vida;


        //Para optimizar el flip del personaje:
        private Vector3 dcha = new Vector3(1, 1, 1);
        private Vector3 izq = new Vector3(-1, 1, 1);

        //private NetworkVariable<int> vida;


        void Start()
        {
            player = GetComponent<Netcode.FighterNetworkConfig>();
            print(player);
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _networkAnimator = GetComponent<NetworkAnimator>();

            _feet = transform.Find("Feet");
            _floor = LayerMask.GetMask("Floor");

            //vida = new NetworkVariable<int>(100);
            //barraDeVida = GetComponent<HealthBar>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        void Update()
        {
            if (!IsServer) return;
            UpdateAnimations();
          
        }

   
        public void UpdateAnimations()
        {
            _grounded = Physics2D.OverlapCircle(_feet.position, 0.1f, _floor);
            _animator.SetFloat(AnimatorSpeed, this._direction.magnitude);
            _animator.SetFloat(AnimatorVSpeed, this._rigidbody2D.velocity.y);
            _animator.SetBool(AnimatorGrounded, this._grounded);

          
        }

       

        void FixedUpdate()
        {
            _rigidbody2D.velocity = new Vector2(_direction.x, _rigidbody2D.velocity.y);
        }



        [ServerRpc]
        public void MoveServerRpc(IMoveableReceiver.Direction direction)
        {
            if (direction == IMoveableReceiver.Direction.None)
            {
                this._direction = Vector3.zero;
                return;
            }

            bool lookingRight = direction == IMoveableReceiver.Direction.Right;
            _direction = (lookingRight ? 1f : -1f) * speed * Vector3.right;

            FlipCharacterClientRpc(lookingRight);
        }

        [ClientRpc]
        public void FlipCharacterClientRpc(bool lookingRight)
        {
            /*
            //localScale positivo: sprite mira a la izq
            //localScale negativo: sprite mira a la dcha
            transform.localScale = new Vector3(lookingRight ? 1 : -1, 1, 1);

            if (lookingRight) { transform.Find("HUD").localScale = new Vector3(1, 1, 1); }
            else { transform.Find("HUD").localScale = new Vector3(-1, 1, 1); }
            */

            transform.localScale = lookingRight ? dcha : izq;
            transform.Find("HUD").localScale = lookingRight ? dcha : izq;

        }


        [ServerRpc]
        public void JumpServerRpc(IJumperReceiver.JumpStage stage)
        {
          
            
            switch (stage)
            {
                case IJumperReceiver.JumpStage.Jumping:
                    if (_grounded)
                    {
                        float jumpForce = Mathf.Sqrt(jumpAmount * -2.0f * (Physics2D.gravity.y * _rigidbody2D.gravityScale));
                        _rigidbody2D.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                    }
                    break;
                case IJumperReceiver.JumpStage.Landing:
                    break;
            }
         
        }

      [ServerRpc]
        public void Attack1ServerRpc()
        {
            _networkAnimator.SetTrigger(AnimatorAttack1); //AnimatorAttack1 es Animator.StringToHash("attack1"); cacheado  
            Debug.Log("Attack1");
        }

        [ServerRpc]
        public void Attack2ServerRpc()
        {
            _networkAnimator.SetTrigger(AnimatorAttack2);
            Debug.Log("Attack2");
        }

        //Este metodo no es serverRPC porque al llamar a los ataques desde el servidor, también ejecuta el OnCollider de Weapon y en caso de que colisione, llamaría a TakeHit
        public void TakeHit()
        {
        
            if (IsOwner)
            {
               
                _networkAnimator.SetTrigger(AnimatorHit);
              
                player.checkLife();
            }
               

        }

        [ServerRpc]
        public void DieServerRpc()
        {
            _networkAnimator.SetTrigger(AnimatorDie);
            Debug.Log("Takehit");
        }

       
    }



}