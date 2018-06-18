using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Node(graphType: typeof(TestNodeGraph))]
[CreateAssetMenu(menuName = "Scriptable Objects/Test Object")]
public class TestObject : ScriptableObject
{
	[System.Serializable]
	public class TestClass
	{
		public TestObject otherTestObject;
	}

	[SerializeField] List<TestClass> testClasses;
	[SerializeField] TestScriptableObject otherNode;
}