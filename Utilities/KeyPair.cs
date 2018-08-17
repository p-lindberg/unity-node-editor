namespace DataDesigner
{
	public struct KeyPair<S, T>
	{
		public readonly S first;
		public readonly T second;

		public KeyPair(S first, T second)
		{
			this.first = first;
			this.second = second;
		}

		public override int GetHashCode()
		{
			return first.GetHashCode() ^ second.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is KeyPair<S,T>)
				return ((KeyPair<S,T>)obj).first.Equals(first) && ((KeyPair<S,T>)obj).second.Equals(second);

			return false;
		}
	}

	public static class KeyPair
	{
		public static KeyPair<S, T> From<S,T>(S first, T second)
		{
			return new KeyPair<S, T>(first, second);
		}
	}
}