using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Linq;
using System;

public class NodeEditor : ZoomableEditorWindow
{
	[SerializeField] NodeGraph currentTarget;

	NodeGraph CurrentTarget { get { return currentTarget; } set { currentTarget = value; EditorUtility.SetDirty(this); } }

	Vector2 currentMousePosition;

	Dictionary<NodeGraph.NodeData, NodeView> nodeViews = new Dictionary<NodeGraph.NodeData, NodeView>();

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
			var settings = ScriptableObject.CreateInstance<NodeEditorSettings>();
			AssetDatabase.CreateAsset(settings, "Assets/Scripts/Codebase/Node Editor/NodeEditorSettings.asset");
			return settings;
		}
		else
			return AssetDatabase.LoadAssetAtPath<NodeEditorSettings>(AssetDatabase.GUIDToAssetPath(settingsGuids[0]));
	}

	[MenuItem("Window/Node Editor")]
	public static void OpenWindow()
	{
		OpenWindow(null);
	}

	public static void OpenWindow(NodeGraph target)
	{
		var window = Init<NodeEditor>("Node Editor");
		if (target != null)
			window.CurrentTarget = target;
	}

	[OnOpenAssetAttribute(1)]
	public static bool OnOpenAsset(int instanceID, int line)
	{
		var targetObject = EditorUtility.InstanceIDToObject(instanceID);
		var nodeGraph = targetObject as NodeGraph;
		if (nodeGraph != null)
		{
			OpenWindow(nodeGraph);
			return true;
		}

		return false;
	}

	NodeView GetNodeView(NodeGraph.NodeData nodeData)
	{
		NodeView nodeView;
		if (nodeViews.TryGetValue(nodeData, out nodeView))
			return nodeView;

		var newNodeView = new NodeView(nodeData);
		nodeViews[nodeData] = newNodeView;
		return newNodeView;
	}

	protected override void DrawZoomAreaContents(Vector2 origin)
	{
		if (CurrentTarget != null && Settings != null)
		{
			BeginWindows();

			foreach (var nodeData in CurrentTarget.Nodes)
				GetNodeView(nodeData).DrawNode(origin);

			EndWindows();
		}
	}

	public override void OnHandleEvents()
	{
		if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
		{
			var genericMenu = new GenericMenu();
			var mousePosition = Event.current.mousePosition;
			foreach (var derivedType in NodeEditorUtilities.GetDerivedTypes<ScriptableObject>(false, false))
			{
				var nodeAttributes = derivedType.GetCustomAttributes(typeof(NodeAttribute), true).Cast<NodeAttribute>();
				var currentGraphNodeAttribute = nodeAttributes.FirstOrDefault(x => x.GraphType == currentTarget.GetType());
				if (currentGraphNodeAttribute != null)
					genericMenu.AddItem(new GUIContent("Create/" + (currentGraphNodeAttribute.MenuName ?? derivedType.Name)), false, () =>
					{
						var nodePosition = NodeEditorUtilities.RoundVectorToIntegerValues(ConvertScreenCoordsToZoomCoords(mousePosition));
						CurrentTarget.CreateNode(derivedType, nodePosition, derivedType.Name);
					});
			}

			genericMenu.ShowAsContext();
			Event.current.Use();
		}
		else if (Event.current.type == EventType.ValidateCommand)
		{
			Repaint();
		}
	}
}