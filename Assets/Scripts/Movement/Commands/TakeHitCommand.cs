using Movement.Components;

namespace Movement.Commands
{
    public class TakeHitCommand : AFightCommand
    {
        public TakeHitCommand(IFighterReceiver receiver) : base(receiver)
        {
        }

        public override void Execute()
        {
            Client.TakeHit();
        }
    }
}