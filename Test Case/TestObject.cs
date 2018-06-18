using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Node(graphType: typeof(TestNodeGraph))]
[CreateAssetMenu(menuName = "Scriptable Objects/Test Object")]
public class TestObject : ScriptableObject
{
	[SerializeField] TestScriptableObject othernode;
}