using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace DataDesigner
{
	public interface IReflectedPropertyView
	{
		UnityEngine.Object PropertyOwner { get; }
		string ReflectedPropertyName { get; }
		Type ReflectedPropertyType { get; }
	}
}