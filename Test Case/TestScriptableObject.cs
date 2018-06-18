using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Node(graphType: typeof(TestNodeGraph))]
[CreateAssetMenu(menuName = "Scriptable Objects/Test Scriptable Object")]
public class TestScriptableObject : ScriptableObject
{
	[System.Serializable]
	public class NodeList
	{
		public List<TestScriptableObject> nodes;
	}
	//[SerializeField] int a;
	//[SerializeField] string b;
	//[SerializeField] GameObject c;
	[SerializeField] List<NodeList> d;
	[SerializeField] List<NodeList> e;
}