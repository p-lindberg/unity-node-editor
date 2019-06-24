using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DataDesigner
{
	public class NodeGraphData : ScriptableObject
	{
		[System.Serializable]
		public class NodeData
		{
			public int id;
			public UnityEngine.Object nodeObject;
			public Vector2 graphPosition;
			public bool isExpanded = true;
			public UnityEngine.Object nestedObjects;

			public override bool Equals(object obj)
			{
				var nodeData = obj as NodeData;
				if (nodeData == null)
					return false;

				return nodeData.id == id;
			}

			public override int GetHashCode()
			{
				return id.GetHashCode();
			}
		}

		[SerializeField]/* [HideInInspector] */ List<NodeData> nodes = new List<NodeData>();

		public IEnumerable<NodeData> Nodes { get { return nodes; } }

		public int NodeCount { get { return nodes.Count; } }

		[SerializeField] UnityEngine.Object graphObject = null;

#if UNITY_EDITOR

		public bool IncludeGraphAsNode
		{
			get
			{
				return nodes.Exists(x => x.nodeObject == graphObject);
			}
			set
			{
				if (value && !IncludeGraphAsNode)
					nodes.Add(new NodeData() { id = 0, nodeObject = graphObject, graphPosition = Vector2.zero });
				else if (!value && IncludeGraphAsNode)
					nodes.RemoveAll(x => x.nodeObject == graphObject);
			}
		}

		public UnityEngine.Object GraphObject { get { return graphObject; } }

		public UnityEngine.Object CreateNode(Type nodeType, Vector2 position, string name)
		{
			Undo.RecordObject(this, "Added node to graph.");

			var node = ScriptableObject.CreateInstance(nodeType);
			node.name = name;
			//node.hideFlags = HideFlags.HideInHierarchy;

			nodes.Add(new NodeData() { id = GetUniqueNodeID(), nodeObject = node, graphPosition = position });

			AssetDatabase.AddObjectToAsset(node, GraphObject);
			return node;
		}

		int GetUniqueNodeID()
		{
			return Nodes.Count() == 0 ? 1 : Nodes.Max(x => x.id) + 1;
		}

		public void RemoveNode(UnityEngine.Object node)
		{
			if (nodes.Exists(x => x.nodeObject == node))
				nodes.RemoveAll(x => x.nodeObject == node);
		}
#endif
	}
}