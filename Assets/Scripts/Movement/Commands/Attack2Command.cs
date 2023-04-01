using Movement.Components;

namespace Movement.Commands
{
    public class Attack2Command : AFightCommand
    {
        public Attack2Command(IFighterReceiver receiver) : base(receiver)
        {
        }

        public override void Execute()
        {
            Client.Attack2();
        }
    }
}