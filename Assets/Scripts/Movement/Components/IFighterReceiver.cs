namespace Movement.Components
{
    public interface IFighterReceiver : IRecevier
    {
        public void Attack1();
        public void Attack2();
        public void TakeHit();
        public void Die();
    }
}