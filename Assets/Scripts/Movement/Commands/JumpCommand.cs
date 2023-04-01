using Movement.Components;

namespace Movement.Commands
{
    class JumpCommand : AMoveCommand
    {
        public JumpCommand(IJumperReceiver client) : base(client)
        {
        }

        public override void Execute()
        {
            ((IJumperReceiver)Client).Jump(IJumperReceiver.JumpStage.Jumping);
        }
    }
}
