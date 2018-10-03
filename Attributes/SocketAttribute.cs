using System;

namespace DataDesigner
{
	[AttributeUsage(AttributeTargets.Class)]
	public class SocketAttribute : Attribute
	{
		public SocketAttribute()
		{
		}
	}
}