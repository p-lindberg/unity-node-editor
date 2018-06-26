using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class NodeView
{
	public class NodeReference
	{
		public string propertyPath;
		public float height;
	}

	NodeGraph.NodeData nodeData;
	float currentPropertyHeight;
	bool dragging;
	Stack<NodeReference> nodeReferences = new Stack<NodeReference>();
	Rect windowRect = new Rect(0, 0, 0, 0);
	Vector2 windowSize;

	public NodeView(NodeGraph.NodeData nodeData)
	{
		this.nodeData = nodeData;
	}

	public virtual void DrawNode(Vector2 origin)
	{
		windowRect.position = origin + nodeData.graphPosition;
		var newRect = GUILayout.Window(nodeData.id, windowRect, (id) =>
		{
			HandleEvents();
			DrawNodeContents();
			GUI.DragWindow();
		}, new GUIContent(), NodeEditor.Settings.NodeGUIStyle);

		windowSize = newRect.size;

		while (nodeReferences.Count > 0)
			DrawConnector(nodeReferences.Pop());

		if (dragging)
			nodeData.graphPosition = NodeEditorUtilities.RoundVectorToIntegerValues(newRect.position - origin);
	}

	protected virtual void DrawConnector(NodeReference nodeReference)
	{
		GUI.Box(new Rect(new Vector2(windowRect.position.x, windowRect.position.y + nodeReference.height), new Vector2(windowSize.x + 30f, 10f)), "");
	}

	protected virtual void DrawNodeContents()
	{
		EditorGUILayout.BeginVertical();
		EditorGUIUtility.labelWidth = NodeEditor.Settings.DefaultLabelWidth;
		EditorGUILayout.LabelField(nodeData.nodeObject.name, NodeEditor.Settings.NodeHeaderStyle);

		currentPropertyHeight += EditorGUIUtility.singleLineHeight + 3 * EditorGUIUtility.standardVerticalSpacing;

		var serializedObject = new SerializedObject(nodeData.nodeObject);
		serializedObject.Update();

		var iterator = serializedObject.GetIterator();
		iterator.NextVisible(true);

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		nodeData.isExpanded = EditorGUILayout.Toggle(nodeData.isExpanded, NodeEditor.Settings.NodeContentToggleStyle);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();

		currentPropertyHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

		if (nodeData.isExpanded)
		{
			EditorGUILayout.BeginVertical(NodeEditor.Settings.SeparatorStyle);
			DrawPropertiesRecursive(iterator);
			EditorGUILayout.EndVertical();
		}

		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.EndVertical();

		currentPropertyHeight = 0f;
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
				currentPropertyHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

				if (NodeEditor.Settings.IndentHeadersOnly)
					EditorGUI.indentLevel = 0;

				if (iterator.isExpanded)
				{
					currentPropertyHeight += EditorGUIUtility.standardVerticalSpacing;
					EditorGUILayout.BeginVertical(NodeEditor.Settings.SeparatorStyle);

					var proceed = DrawPropertiesRecursive(iterator);

					EditorGUILayout.EndVertical();

					if (depth == iterator.depth)
						currentPropertyHeight += EditorGUIUtility.standardVerticalSpacing;

					if (proceed)
						continue;
					else
						return false;
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
						nodeReferences.Push(new NodeReference() { propertyPath = iterator.propertyPath, height = currentPropertyHeight });
					}
				}

				currentPropertyHeight += EditorGUI.GetPropertyHeight(iterator) + EditorGUIUtility.standardVerticalSpacing;
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

		dragging = Event.current.type == EventType.MouseDrag;
	}
}
