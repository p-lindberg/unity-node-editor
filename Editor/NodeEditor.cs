using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Linq;
using System;
using System.Reflection;

public class NodeEditor : ZoomableEditorWindow
{
	[SerializeField] ScriptableObject currentTarget;

	public ScriptableObject CurrentTarget { get { return currentTarget; } set { currentTarget = value; EditorUtility.SetDirty(this); } }

	Vector2 currentMousePosition;

	Dictionary<NodeGraphData.NodeData, NodeView> nodeViews = new Dictionary<NodeGraphData.NodeData, NodeView>();

	static NodeEditorSettings settings;

	public static NodeEditorSettings Settings
	{
		get
		{
			if (settings == null)
				settings = GetSettings();

			return settings;
		}
	}

	protected override Texture2D Background
	{
		get
		{
			return Settings.WindowBackground;
		}
	}

	static NodeEditorSettings GetSettings()
	{
		var settingsGuids = AssetDatabase.FindAssets("t:NodeEditorSettings");
		if (settingsGuids.Length == 0)
		{
			Debug.LogError("Could not find Node Editor Settings object. Please create it.");
			return null;
		}
		else
			return AssetDatabase.LoadAssetAtPath<NodeEditorSettings>(AssetDatabase.GUIDToAssetPath(settingsGuids[0]));
	}

	[MenuItem("Window/Node Editor")]
	public static void OpenWindow()
	{
		OpenWindow(null);
	}

	public static void OpenWindow(ScriptableObject target)
	{
		var window = Init<NodeEditor>("Node Editor");
		if (target != null)
			window.CurrentTarget = target;

		window.wantsMouseMove = true;
		window.wantsMouseEnterLeaveWindow = true;
	}

	protected override void DrawUtilityBarContents()
	{
		base.DrawUtilityBarContents();
		GUILayout.FlexibleSpace();
		GUILayout.Label(CurrentTarget != null ? CurrentTarget.name : "No graph selected", Settings.GraphHeaderStyle);
	}

	[OnOpenAssetAttribute(1)]
	public static bool OnOpenAsset(int instanceID, int line)
	{
		var targetObject = EditorUtility.InstanceIDToObject(instanceID) as ScriptableObject;
		if (targetObject != null)
		{
			if (targetObject.GetType().GetCustomAttributes(typeof(NodeGraphAttribute), true).Count() == 0)
				return false;

			OpenWindow(targetObject);
			return true;
		}

		return false;
	}

	NodeView GetNodeView(NodeGraphData.NodeData nodeData)
	{
		NodeView nodeView;
		if (nodeViews.TryGetValue(nodeData, out nodeView))
			return nodeView;

		var viewParameters = new NodeView.ViewParameters();

		var nodeAttribute = GetCurrentGraphNodeAttribute(nodeData.nodeObject.GetType());
		if (nodeAttribute == null)
		{
			// TODO: The type is no longer a node in this graph type. Draw it with regular node view but grey it out.
		}
		else
			viewParameters.expandedSizeOverride = nodeAttribute.ExpandedSizeOverride;

		var newNodeView = new NodeView(this, nodeData, viewParameters);
		SetupNodeView(newNodeView, nodeData);
		nodeViews[nodeData] = newNodeView;
		return newNodeView;
	}

	void SetupNodeView(NodeView nodeView, NodeGraphData.NodeData nodeData)
	{
		nodeView.OnShowContextMenu += (genericMenu) => SetupNodeContextMenuItems(nodeData, genericMenu);
	}

	void SetupNodeContextMenuItems(NodeGraphData.NodeData nodeData, GenericMenu genericMenu)
	{
		genericMenu.AddItem(new GUIContent("Delete"), false, () =>
		{
			GetNodeGraphData(CurrentTarget).DeleteNode(nodeData.nodeObject);
			nodeViews.Remove(nodeData);
		});

		foreach (var exposedNode in GetExposedNodeFields())
		{
			if (exposedNode.Key.FieldType.IsAssignableFrom(nodeData.nodeObject.GetType()))
				genericMenu.AddItem(new GUIContent("Set as/" + (exposedNode.Value.Name ?? exposedNode.Key.Name)), false, () =>
				{
					Undo.RecordObject(CurrentTarget, "Changed exposed node");
					exposedNode.Key.SetValue(CurrentTarget, nodeData.nodeObject);
					EditorUtility.SetDirty(CurrentTarget);
				});
		}
	}

	IEnumerable<KeyValuePair<FieldInfo, ExposedNodeAttribute>> GetExposedNodeFields()
	{
		foreach (var field in CurrentTarget.GetType().GetFields(NodeEditorUtilities.StandardBindingFlags).Where(x => x.FieldType.IsSubclassOf(typeof(ScriptableObject))))
		{
			var exposedNodeAttribute = field.GetCustomAttributes(typeof(ExposedNodeAttribute), true).FirstOrDefault() as ExposedNodeAttribute;
			if (exposedNodeAttribute != null)
				yield return new KeyValuePair<FieldInfo, ExposedNodeAttribute>(field, exposedNodeAttribute);
		}
	}

	NodeAttribute GetCurrentGraphNodeAttribute(Type type)
	{
		var nodeAttributes = type.GetCustomAttributes(typeof(NodeAttribute), true).Cast<NodeAttribute>();
		return nodeAttributes.FirstOrDefault(x => x.GraphType == currentTarget.GetType());
	}

	NodeGraphData GetNodeGraphData(ScriptableObject scriptableObject)
	{
		var nodeGraphData = AssetDatabase.LoadAssetAtPath<NodeGraphData>(AssetDatabase.GetAssetPath(scriptableObject));
		if (nodeGraphData == null)
		{
			nodeGraphData = ScriptableObject.CreateInstance<NodeGraphData>();
			nodeGraphData.name = "Node Graph Data";
			AssetDatabase.AddObjectToAsset(nodeGraphData, scriptableObject);
			EditorUtility.SetDirty(scriptableObject);
			AssetDatabase.SaveAssets();
		}

		return nodeGraphData;
	}

	protected override void DrawZoomAreaContents(Vector2 origin)
	{
		if (CurrentTarget == null || Settings == null)
			return;

		BeginWindows();

		foreach (var nodeData in GetNodeGraphData(CurrentTarget).Nodes)
		{
			var nodeView = GetNodeView(nodeData);
			nodeView.Draw(origin);

			foreach (var kvp in GetExposedNodeFields())
				if (kvp.Key.GetValue(CurrentTarget) as UnityEngine.Object == nodeData.nodeObject)
					nodeView.DrawTag(kvp.Value.Name ?? kvp.Key.Name);
		}

		EndWindows();
	}

	public override void OnHandleEvents()
	{
		if (CurrentTarget == null || Settings == null)
			return;

		if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
		{
			var genericMenu = new GenericMenu();
			var mousePosition = Event.current.mousePosition;
			foreach (var derivedType in NodeEditorUtilities.GetDerivedTypes<ScriptableObject>(false, false))
			{
				var currentGraphNodeAttribute = GetCurrentGraphNodeAttribute(derivedType);
				if (currentGraphNodeAttribute != null)
					genericMenu.AddItem(new GUIContent("Create/" + (currentGraphNodeAttribute.NodeName ?? derivedType.Name)), false, () =>
					{
						var nodePosition = NodeEditorUtilities.RoundVectorToIntegerValues(ConvertScreenCoordsToZoomCoords(mousePosition));
						GetNodeGraphData(CurrentTarget).CreateNode(derivedType, nodePosition, (currentGraphNodeAttribute.NodeName ?? derivedType.Name));
					});
			}

			genericMenu.ShowAsContext();
			Event.current.Use();
		}
		else if (Event.current.type == EventType.MouseMove
				|| Event.current.type == EventType.MouseDrag)
			Repaint();
		else if (Event.current.type == EventType.ValidateCommand)
		{
			if (Event.current.commandName == "UndoRedoPerformed")
			{
				AssetDatabase.SaveAssets();
			}

			Repaint();
		}
	}

	public NodeGraphData.NodeData GetNodeViewAtMousePosition(Vector3 screenPosition)
	{
		return nodeViews.FirstOrDefault(x => x.Value.GetWindowRect().Contains(screenPosition)).Key;
	}

	public NodeView GetNodeView(UnityEngine.Object nodeObject)
	{
		return nodeViews.FirstOrDefault(x => x.Key.nodeObject == nodeObject).Value;
	}

	public Rect GetNodeViewRect(NodeGraphData.NodeData nodeData)
	{
		var view = nodeViews.FirstOrDefault(x => x.Key == nodeData).Value;
		return view != null ? view.GetWindowRect() : default(Rect);
	}

	public Rect GetNodeViewRect(UnityEngine.Object nodeObject)
	{
		var view = nodeViews.FirstOrDefault(x => x.Key.nodeObject == nodeObject).Value;
		return view != null ? view.GetWindowRect() : default(Rect);
	}
}