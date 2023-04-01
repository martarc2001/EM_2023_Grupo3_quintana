using Movement.Components;

namespace Movement.Commands
{
    public class Attack1Command : AFightCommand
    {
        public Attack1Command(IFighterReceiver receiver) : base(receiver)
        {
        }

        public override void Execute()
        {
            Client.Attack1();
        }
    }
}