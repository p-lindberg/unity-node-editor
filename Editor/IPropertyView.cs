using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DataDesigner
{
	public interface IPropertyView
	{
		SerializedProperty ViewProperty { get; }
	}
}