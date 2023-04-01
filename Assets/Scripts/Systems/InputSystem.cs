using System.Collections.Generic;
using Movement.Commands;
using Movement.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Systems
{
    public class InputSystem : MonoBehaviour
    {
        private static InputSystem _instance;
        public static InputSystem Instance => _instance;

        [SerializeField] private FighterMovement _character;
        public FighterMovement Character
        {
            get => _character;
            set
            {
                _character = value;
                SetCharacter(_character);
            }
        }

        public InputAction Move;
        public InputAction Jump;
        public InputAction Attack1;
        public InputAction Attack2;

        private Dictionary<string, ICommand> _commands;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
            
            if (_character)
            {
                SetCharacter(_character);
            }
        }

        public void SetCharacter(FighterMovement character)
        {
            _commands = new Dictionary<string, ICommand> {
                { "stop", new StopCommand(character) },
                { "walk-left", new WalkLeftCommand(character) },
                { "walk-right", new WalkRightCommand(character) },
                { "jump", new JumpCommand(character) },
                { "land", new LandCommand(character) },
                { "attack1", new Attack1Command(character) },
                { "attack2", new Attack2Command(character) }
            };

            Move.performed += OnMove;
            Move.Enable();

            Jump.performed += OnJump;
            Jump.Enable();

            Attack1.started += context =>
            {
                _commands["attack1"].Execute();
            };
            Attack1.Enable();

            Attack2.started += context =>
            {
                _commands["attack2"].Execute();
            };
            Attack2.Enable();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            float value = context.ReadValue<float>();

            // Debug.Log($"OnMove called {context.action}");

            if (value == 0f)
            {
                _commands["stop"].Execute();
            }
            else if (value == 1f)
            {
                _commands["walk-right"].Execute();
            }
            else
            {
                _commands["walk-left"].Execute();
            }
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            float value = context.ReadValue<float>();

            // Debug.Log($"OnJump called {context.ReadValue<float>()}");

            if (value == 0f)
            {
                _commands["land"].Execute();
            }
            else
            {
                _commands["jump"].Execute();
            }
        }
    }
}