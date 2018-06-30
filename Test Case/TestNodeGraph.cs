using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TestNodeGraph")]
[NodeGraph]
public class TestNodeGraph : ScriptableObject
{
	[ExposedNode(name: "Root Node")] public TestObjectBase rootNode;
	[ExposedNode(name: "Secondary Root")] public TestObjectBase secondaryRoot;
}