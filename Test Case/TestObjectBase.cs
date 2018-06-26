using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Node(graphType: typeof(TestNodeGraph))]
[CreateAssetMenu(menuName = "Scriptable Objects/Test Object Base")]
public class TestObjectBase : ScriptableObject
{
	public TestObjectBase otherNode;
}