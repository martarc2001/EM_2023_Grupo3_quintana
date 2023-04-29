using Movement.Components;

namespace Movement.Commands
{
    public class StopCommand : AMoveCommand
    {
        public StopCommand(IMoveableReceiver client) : base(client)
        {
        }

        public override void Execute()
        {
            ((IMoveableReceiver)Client).MoveServerRpc(IMoveableReceiver.Direction.None);
        }
    }
}
