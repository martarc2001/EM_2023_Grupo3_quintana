using Movement.Components;

namespace Movement.Commands
{
    public class WalkRightCommand : AMoveCommand
    {
        public WalkRightCommand(IMoveableReceiver client) : base(client)
        {
        }

        public override void Execute()
        {
            ((IMoveableReceiver)Client).MoveServerRpc(IMoveableReceiver.Direction.Right);
        }
    }
}
