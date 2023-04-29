using Movement.Components;

namespace Movement.Commands
{
    public class WalkLeftCommand : AMoveCommand
    {
        public WalkLeftCommand(IMoveableReceiver client) : base(client)
        {
        }

        public override void Execute()
        {
            ((IMoveableReceiver)Client).MoveServerRpc(IMoveableReceiver.Direction.Left);
        }
    }
}
