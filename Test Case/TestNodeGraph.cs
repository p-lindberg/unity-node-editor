using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TestNodeGraph")]
public class TestNodeGraph : ScriptableObject
{
	public TestObjectBase rootNode;
	public TestObjectBase secondaryRoot;
}