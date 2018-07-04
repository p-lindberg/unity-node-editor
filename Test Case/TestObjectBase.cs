using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Test Object Base")]
public class TestObjectBase : ScriptableObject
{
	[System.Serializable]
	public class NestedClass
	{
		public TestObjectBase otherNode1;
		public TestObjectBase otherNode2;
		public TestObjectBase otherNode3;
		public TestObjectBase otherNode4;
	}

	public NestedClass nestedClass;
	public List<NestedClass> nestedClassList;
	public TestObjectBase otherNode;
}