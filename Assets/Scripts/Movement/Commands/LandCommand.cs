using Movement.Components;

namespace Movement.Commands
{
    class LandCommand : AMoveCommand
    {
        public LandCommand(IJumperReceiver client) : base(client)
        {
        }

        public override void Execute()
        {
            ((IJumperReceiver)Client).Jump(IJumperReceiver.JumpStage.Landing);
        }
    }
}
