using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class NodeView
{
	NodeGraph.NodeData nodeData;

	public NodeView(NodeGraph.NodeData nodeData)
	{
		this.nodeData = nodeData;
	}

	public virtual void DrawNode(Vector2 origin)
	{
		var position = origin + nodeData.graphPosition;

		var rect = new Rect(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), 200, 60);
		var newRect = GUILayout.Window(nodeData.id, rect, (id) =>
		{
			HandleEvents();
			DrawNodeContents();
			GUI.DragWindow(new Rect(0, 0, 200, 30));
		}, new GUIContent(), NodeEditor.Settings.NodeGUIStyle);
		nodeData.graphPosition = newRect.position - origin;
	}

	public virtual void DrawNodeContents()
	{
		EditorGUILayout.BeginVertical();
		EditorGUIUtility.labelWidth = NodeEditor.Settings.DefaultLabelWidth;
		EditorGUILayout.LabelField(nodeData.nodeObject.name, NodeEditor.Settings.NodeHeaderStyle);

		var serializedObject = new SerializedObject(nodeData.nodeObject);
		serializedObject.Update();

		var iterator = serializedObject.GetIterator();
		iterator.NextVisible(true);

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		nodeData.isExpanded = EditorGUILayout.Toggle(nodeData.isExpanded, NodeEditor.Settings.NodeContentToggleStyle);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		if (nodeData.isExpanded)
			DrawPropertiesRecursive(iterator);

		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.EndVertical();
	}

	protected bool DrawPropertiesRecursive(SerializedProperty iterator)
	{
		bool next = iterator.NextVisible(true);
		var depth = iterator.depth;
		while (next && iterator.depth >= depth)
		{
			if (iterator.hasVisibleChildren)
			{
				if (NodeEditor.Settings.IndentNested)
					EditorGUI.indentLevel = iterator.depth;

				iterator.isExpanded = EditorGUILayout.Foldout(iterator.isExpanded, iterator.displayName, true);

				if (NodeEditor.Settings.IndentHeadersOnly)
					EditorGUI.indentLevel = 0;

				if (iterator.isExpanded)
				{
					EditorGUILayout.BeginVertical(NodeEditor.Settings.SeparatorStyle);

					if (DrawPropertiesRecursive(iterator))
					{
						EditorGUILayout.EndVertical();
						continue;
					}
					else
					{
						EditorGUILayout.EndVertical();
						return false;
					}
				}
			}

			else
			{
				EditorGUILayout.PropertyField(iterator, false);
				if (iterator.propertyType == SerializedPropertyType.ObjectReference)
				{
					var propertyType = NodeEditorUtilities.GetPropertyType(iterator);
					var nodeAttributes = propertyType.GetCustomAttributes(typeof(NodeAttribute), true).Cast<NodeAttribute>();
					if (nodeAttributes.Any(x => x.GraphType == nodeData.nodeGraph.GetType()))
					{
						// Link!
					}
				}

				var lastRect = GUILayoutUtility.GetLastRect();
			}

			next = iterator.NextVisible(iterator.isExpanded);
		}

		return next;
	}

	public virtual void HandleEvents()
	{
		if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
			Selection.activeObject = nodeData.nodeObject;
		else if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
		{
			var genericMenu = new GenericMenu();
			genericMenu.AddItem(new GUIContent("Delete"), false, () =>
			{
				nodeData.nodeGraph.DeleteNode(nodeData.nodeObject);
			});
			genericMenu.ShowAsContext();
			Event.current.Use();
		}
	}
}
