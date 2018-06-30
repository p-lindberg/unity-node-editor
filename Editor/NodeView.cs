using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

// TODO: Highlight selected, highlight when mousing over a connectable node while connecting

public class NodeView
{
	public event System.Action OnDelete;

	public bool IsDead { get { return nodeData == null; } }

	public NodeEditor NodeEditor { get; private set; }

	public NodeViewSettings Settings { get { return NodeEditor.Settings.DefaultNodeViewSettings; } }

	public virtual GUIStyle GUIStyle { get { return Settings.GUIStyle; } }

	public class ViewParameters
	{
		public string name;
		public Vector2 expandedSizeOverride;
	}

	ViewParameters viewParameters;
	float currentPropertyHeight;
	bool dragging;
	Vector2 origin;
	NodeGraph.NodeData nodeData;
	Vector2 size;
	SerializedObject serializedObject;
	Dictionary<string, NodeConnector> nodeConnectors = new Dictionary<string, NodeConnector>();
	System.Action postDraw;

	public NodeView(NodeEditor nodeEditor, NodeGraph.NodeData nodeData, ViewParameters viewParameters)
	{
		this.NodeEditor = nodeEditor;
		this.nodeData = nodeData;
		this.viewParameters = viewParameters;
		serializedObject = new SerializedObject(nodeData.nodeObject);
	}

	Rect GetWindowRectInternal()
	{
		var minSize = nodeData.isExpanded ? Settings.MinimumSize : Settings.MinimumSizeCollapsed;

		if (nodeData.isExpanded)
		{
			minSize.x = viewParameters.expandedSizeOverride.x != 0 ? viewParameters.expandedSizeOverride.x : minSize.x;
			minSize.y = viewParameters.expandedSizeOverride.y != 0 ? viewParameters.expandedSizeOverride.y : minSize.y;
		}

		return new Rect(origin + nodeData.graphPosition, minSize);
	}

	NodeConnector GetNodeConnector(string propertyPath)
	{
		NodeConnector nodeConnector;
		if (nodeConnectors.TryGetValue(propertyPath, out nodeConnector))
			return nodeConnector;

		nodeConnector = new NodeConnector(this, serializedObject, propertyPath);
		nodeConnector.OnDeath += () => postDraw += () => nodeConnectors.Remove(nodeConnector.PropertyPath);
		nodeConnectors[propertyPath] = nodeConnector;
		return nodeConnector;
	}

	public Rect GetWindowRect()
	{
		return new Rect(origin + nodeData.graphPosition, size);
	}

	public virtual void Draw(Vector2 origin)
	{
		this.origin = origin;
		var newRect = GUILayout.Window(nodeData.id, GetWindowRectInternal(), (id) =>
		{
			DrawContents();
			HandleEvents();
			GUI.DragWindow();
		}, new GUIContent(), GUIStyle);

		if (Event.current.type == EventType.Repaint)
			size = newRect.size;

		foreach (var nodeConnector in nodeConnectors.Values)
			nodeConnector.Draw();

		if (dragging)
			nodeData.graphPosition = NodeEditorUtilities.RoundVectorToIntegerValues(newRect.position - origin);

		if (postDraw != null)
		{
			postDraw.Invoke();
			postDraw = null;
		}
	}

	public virtual void HandleEvents()
	{
		if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
		{
			if (Event.current.control)
				nodeData.isExpanded = !nodeData.isExpanded;

			Selection.activeObject = nodeData.nodeObject;
		}
		else if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
		{
			var genericMenu = new GenericMenu();
			genericMenu.AddItem(new GUIContent(nodeData.isExpanded ? "Collapse" : "Expand"), false, () =>
			{
				nodeData.isExpanded = !nodeData.isExpanded;
			});
			genericMenu.AddItem(new GUIContent("Delete"), false, () =>
			{
				nodeData.nodeGraph.DeleteNode(nodeData.nodeObject);
				nodeData = null;

				if (OnDelete != null)
					OnDelete.Invoke();
			});
			genericMenu.ShowAsContext();
			Event.current.Use();
		}

		dragging = Event.current.type == EventType.MouseDrag;
	}

	protected virtual void DrawContents()
	{
		EditorGUIUtility.labelWidth = Settings.MinimumLabelWidth;
		EditorGUIUtility.fieldWidth = Settings.MinimumFieldWidth;

		if (nodeData.isExpanded)
			DrawExpandedContents();
		else
			DrawCollapsedContents();
	}

	void DrawCollapsedContents()
	{
		EditorGUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
		GUILayout.Label(nodeData.nodeObject.name, Settings.NodeHeaderStyle);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndVertical();
	}

	void DrawExpandedContents()
	{
		EditorGUILayout.BeginVertical();
		EditorGUIUtility.labelWidth = Settings.MinimumLabelWidth;
		EditorGUIUtility.fieldWidth = Settings.MinimumFieldWidth;
		GUILayout.Label(nodeData.nodeObject.name, Settings.NodeHeaderStyle);

		GUILayout.Space(EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);

		currentPropertyHeight += EditorGUIUtility.singleLineHeight + 3 * EditorGUIUtility.standardVerticalSpacing;

		serializedObject.Update();

		var iterator = serializedObject.GetIterator();
		iterator.NextVisible(true);

		currentPropertyHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

		if (nodeData.isExpanded)
		{
			EditorGUILayout.BeginVertical(Settings.SeparatorStyle);
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
				if (Settings.IndentNested)
					EditorGUI.indentLevel = iterator.depth;

				iterator.isExpanded = EditorGUILayout.Foldout(iterator.isExpanded, iterator.displayName, true);
				currentPropertyHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

				if (Settings.IndentHeadersOnly)
					EditorGUI.indentLevel = 0;

				if (iterator.isExpanded)
				{
					currentPropertyHeight += EditorGUIUtility.standardVerticalSpacing;
					EditorGUILayout.BeginVertical(Settings.SeparatorStyle);

					var proceed = DrawPropertiesRecursive(iterator);

					EditorGUILayout.EndVertical();

					if (proceed)
					{
						if (depth == iterator.depth)
							currentPropertyHeight += EditorGUIUtility.standardVerticalSpacing;

						continue;
					}
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
						var nodeConnector = GetNodeConnector(iterator.propertyPath);
						nodeConnector.SetDrawProperties(currentPropertyHeight, true);
					}
				}

				currentPropertyHeight += EditorGUI.GetPropertyHeight(iterator) + EditorGUIUtility.standardVerticalSpacing;
			}

			next = iterator.NextVisible(iterator.isExpanded);
		}

		return next;
	}
}
