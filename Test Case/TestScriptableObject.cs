using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Node(graphType: typeof(TestNodeGraph))]
[CreateAssetMenu(menuName = "Scriptable Objects/Test Scriptable Object")]
public class TestScriptableObject : ScriptableObject
{
	[SerializeField] TestScriptableObject othernode;
	[SerializeField] TestScriptableObject othernode1;
	[SerializeField] TestScriptableObject othernode2;
	[SerializeField] TestScriptableObject othernode3;
}