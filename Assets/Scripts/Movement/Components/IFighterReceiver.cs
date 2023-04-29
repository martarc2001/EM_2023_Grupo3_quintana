namespace Movement.Components
{
    public interface IFighterReceiver : IRecevier
    {
        public void Attack1ServerRpc();
        public void Attack2ServerRpc();
        public void TakeHit();
        public void DieServerRpc();
    }
}