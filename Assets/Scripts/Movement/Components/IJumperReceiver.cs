namespace Movement.Components
{
	public interface IJumperReceiver : IRecevier
	{
		public enum JumpStage
		{
			Jumping,
			Landing
		}

		public void JumpServerRpc(JumpStage stage);
	}
}
