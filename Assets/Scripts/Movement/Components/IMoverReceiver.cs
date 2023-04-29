namespace Movement.Components
{
	public interface IMoveableReceiver : IRecevier
	{
		public enum Direction
		{
			None,
			Right,
			Left,
		}

		public void MoveServerRpc(Direction direction);
	}
}
