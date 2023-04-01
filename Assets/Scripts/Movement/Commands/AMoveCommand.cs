using Movement.Components;

namespace Movement.Commands
{
    public abstract class AMoveCommand : ICommand
    {
        protected readonly IRecevier Client;

        protected AMoveCommand(IRecevier client)
        {
            Client = client;

        }

        public abstract void Execute();
    }
}
