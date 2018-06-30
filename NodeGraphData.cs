using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class NodeGraphData : ScriptableObject
{
	[System.Serializable]
	public class NodeData
	{
		public int id;
		public UnityEngine.Object nodeObject;
		public Vector2 graphPosition;
		public bool isExpanded;
	}

	[SerializeField]/* [HideInInspector] */ List<NodeData> nodes = new List<NodeData>();

	public IEnumerable<NodeData> Nodes { get { return nodes; } }

	public int NodeCount { get { return nodes.Count; } }

#if UNITY_EDITOR

	UnityEngine.Object graphObject;

	public UnityEngine.Object GraphObject
	{
		get
		{
			if (graphObject == null)
				graphObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(this));

			return graphObject;
		}
	}

	public UnityEngine.Object CreateNode(Type nodeType, Vector2 position, string name)
	{
		Undo.RecordObject(this, "Added node to graph.");

		var node = ScriptableObject.CreateInstance(nodeType);
		node.name = name;
		//node.hideFlags = HideFlags.HideInHierarchy;

		nodes.Add(new NodeData() { id = GetUniqueNodeID(), nodeObject = node, graphPosition = position });

		AssetDatabase.AddObjectToAsset(node, GraphObject);
		EditorUtility.SetDirty(GraphObject);
		AssetDatabase.SaveAssets();
		return node;
	}

	int GetUniqueNodeID()
	{
		return Nodes.Count() == 0 ? 0 : Nodes.Max(x => x.id) + 1;
	}

	public void DeleteNode(UnityEngine.Object node)
	{
		if (nodes.Exists(x => x.nodeObject == node))
		{
			Undo.RecordObjects(new UnityEngine.Object[] { GraphObject, node, this }, "Removed node from graph.");
			nodes.RemoveAll(x => x.nodeObject == node);
			Undo.DestroyObjectImmediate(node);
			EditorUtility.SetDirty(GraphObject);
			AssetDatabase.SaveAssets();
		}
	}
#endif
}