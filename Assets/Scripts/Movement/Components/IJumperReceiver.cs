namespace Movement.Components
{
	public interface IJumperReceiver : IRecevier
	{
		public enum JumpStage
		{
			Jumping,
			Landing
		}

		public void Jump(JumpStage stage);
	}
}
