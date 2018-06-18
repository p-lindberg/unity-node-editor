using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Node(graphType: typeof(TestNodeGraph))]
[CreateAssetMenu(menuName = "Scriptable Objects/Test Scriptable Object1")]
public class TestScriptableObject1 : ScriptableObject
{
	[SerializeField] int a;
	[SerializeField] string b;
	[SerializeField] GameObject c;
	[SerializeField] AudioClip d;
}