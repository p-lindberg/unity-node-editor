using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DataDesigner
{
	[CreateAssetMenu(menuName = "TestNodeGraph")]
	public class TestNodeGraph : ScriptableObject
	{
		public TestObjectBase rootNode;
		public TestObjectBase secondaryRoot;
	}
}