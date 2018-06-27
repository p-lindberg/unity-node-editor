using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

// TODO: To avoid editor-only data ending up in a build, we can put the node data in an embedded asset.
//		 So long as there is no direct reference to that asset (in the editor we can find it by editor methods),
//		 it should not be included in the build (hopefully).
// ALSO: Moving editor data to a separate class will leave this class empty. We could drop it, and use an attribute
//		 instead. This is actually more flexible, because then a single asset could be more than one type of
//		 graph. 

public class NodeGraph : ScriptableObject
{
	[System.Serializable]
	public class NodeData
	{
		public int id;
		public NodeGraph nodeGraph;
		public UnityEngine.Object nodeObject;
		public Vector2 graphPosition;
		public bool isExpanded;
	}

	[SerializeField]/* [HideInInspector] */ List<NodeData> nodes = new List<NodeData>();

	public IEnumerable<NodeData> Nodes { get { return nodes; } }

	public int NodeCount { get { return nodes.Count; } }

#if UNITY_EDITOR
	public UnityEngine.Object CreateNode(Type nodeType, Vector2 position, string name)
	{
		Undo.RecordObject(this, "Added node to graph.");

		var node = ScriptableObject.CreateInstance(nodeType);
		node.name = name;
		//node.hideFlags = HideFlags.HideInHierarchy;

		nodes.Add(new NodeData() { id = GetUniqueNodeID(), nodeGraph = this, nodeObject = node, graphPosition = position });

		AssetDatabase.AddObjectToAsset(node, this);
		EditorUtility.SetDirty(this);
		AssetDatabase.SaveAssets();
		return node;
	}

	int GetUniqueNodeID()
	{
		return Nodes.Max(x => x.id) + 1;
	}

	public void DeleteNode(UnityEngine.Object node)
	{
		if (nodes.Exists(x => x.nodeObject == node))
		{
			Undo.RecordObjects(new UnityEngine.Object[] { this, node }, "Removed node from graph.");
			nodes.RemoveAll(x => x.nodeObject == node);
			Undo.DestroyObjectImmediate(node);
			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();
		}
	}
#endif
}