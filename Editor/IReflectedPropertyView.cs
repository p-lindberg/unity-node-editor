using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DataDesigner
{
	public interface IReflectedPropertyView
	{
		UnityEngine.Object PropertyOwner { get; }
		string ReflectedPropertyName { get; }
	}
}