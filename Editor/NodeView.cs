using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

// TODO: Highlight selected, highlight when mousing over a connectable node while connecting

public class NodeView
{
	public event System.Action OnDelete;

	public class NodeReference
	{
		public System.Type propertyType;
		public UnityEngine.Object currentTarget;
		public string propertyPath;
		public float height;
	}

	public bool IsConnecting { get { return connectingNodeReference != null; } }
	public bool IsDead { get { return nodeData == null; } }

	NodeEditor parentEditor;
	NodeGraph.NodeData nodeData;
	float currentPropertyHeight;
	bool dragging;
	Stack<NodeReference> nodeReferences = new Stack<NodeReference>();
	Vector2 windowSize;
	NodeReference connectingNodeReference;
	Rect windowRect;

	SerializedObject serializedObject;

	public NodeView(NodeEditor parentEditor, NodeGraph.NodeData nodeData)
	{
		this.parentEditor = parentEditor;
		this.nodeData = nodeData;
		serializedObject = new SerializedObject(nodeData.nodeObject);
	}

	public Rect GetWindowRect(Vector2 origin)
	{
		return new Rect(origin + nodeData.graphPosition, windowSize);
	}

	public Rect GetWindowRectNoSize(Vector2 origin)
	{
		return new Rect(origin + nodeData.graphPosition, Vector2.zero);
	}

	public virtual void DrawNode(Vector2 origin)
	{
		windowRect = GetWindowRect(origin);
		var newRect = GUILayout.Window(nodeData.id, GetWindowRectNoSize(origin), (id) =>
		{
			HandleEventsInside();
			DrawNodeContents();
			GUI.DragWindow();
		}, new GUIContent(), NodeEditor.Settings.NodeGUIStyle);

		if (Event.current.type == EventType.Repaint)
			windowSize = newRect.size;

		while (nodeReferences.Count > 0)
			DrawConnector(nodeReferences.Pop());

		if (dragging)
			nodeData.graphPosition = NodeEditorUtilities.RoundVectorToIntegerValues(newRect.position - origin);

		if (IsConnecting)
			Handles.DrawLine(new Vector3(windowRect.position.x + 180f, windowRect.position.y + connectingNodeReference.height + 5f), Event.current.mousePosition);

		HandleEventsOutside();
	}

	protected virtual void DrawConnector(NodeReference nodeReference)
	{
		var connectorRect = new Rect(new Vector2(windowRect.position.x, windowRect.position.y + nodeReference.height), new Vector2(windowSize.x + 30f, 10f));
		GUI.Box(connectorRect, "");
		if (nodeReference.currentTarget != null)
		{
			var from = new Vector3(windowRect.position.x + 180f, windowRect.position.y + nodeReference.height + 5f);
			var to = parentEditor.GetNodeViewRect(nodeReference.currentTarget).position;
			Handles.DrawLine(from, to);
		}
		HandleConnectorEvents(connectorRect, nodeReference);
	}

	protected virtual void HandleConnectorEvents(Rect connectorRect, NodeReference nodeReference)
	{
		if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && connectorRect.Contains(Event.current.mousePosition))
		{
			connectingNodeReference = nodeReference;
			Event.current.Use();
		}
	}

	protected void TryConnectNodeReference(NodeReference nodeReference, UnityEngine.Object target)
	{
		Debug.Log(serializedObject);
		Debug.Log(nodeReference);
		var property = serializedObject.FindProperty(nodeReference.propertyPath);
		property.objectReferenceValue = target;
		serializedObject.ApplyModifiedProperties();
	}

	protected virtual void DrawNodeContents()
	{
		EditorGUILayout.BeginVertical();
		EditorGUIUtility.labelWidth = NodeEditor.Settings.DefaultLabelWidth;
		EditorGUILayout.LabelField(nodeData.nodeObject.name, NodeEditor.Settings.NodeHeaderStyle);

		currentPropertyHeight += EditorGUIUtility.singleLineHeight + 3 * EditorGUIUtility.standardVerticalSpacing;

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
						nodeReferences.Push(new NodeReference()
						{
							propertyType = propertyType,
							currentTarget = iterator.objectReferenceValue,
							propertyPath = iterator.propertyPath,
							height = currentPropertyHeight
						});
					}
				}

				currentPropertyHeight += EditorGUI.GetPropertyHeight(iterator) + EditorGUIUtility.standardVerticalSpacing;
			}

			next = iterator.NextVisible(iterator.isExpanded);
		}

		return next;
	}

	public virtual void HandleEventsInside()
	{
		if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
			Selection.activeObject = nodeData.nodeObject;
		else if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
		{
			var genericMenu = new GenericMenu();
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

	public virtual void HandleEventsOutside()
	{
		if (IsConnecting && ((Event.current.type == EventType.MouseUp && Event.current.button == 0)
							 || (Event.current.type == EventType.MouseLeaveWindow)))
		{
			var hit = parentEditor.RaycastNode(Event.current.mousePosition);
			if (hit != null)
				TryConnectNodeReference(connectingNodeReference, hit.nodeObject);

			connectingNodeReference = null;

			Event.current.Use();
		}
	}
}
