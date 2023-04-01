using Movement.Components;

namespace Movement.Commands
{
    public abstract class AFightCommand : ICommand
    {
        protected readonly IFighterReceiver Client;

        protected AFightCommand(IFighterReceiver receiver)
        {
            Client = receiver;
        }

        public abstract void Execute();
    }
}