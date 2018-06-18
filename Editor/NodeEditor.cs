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

	protected override void DrawZoomAreaContents(Vector2 origin)
	{
		if (CurrentTarget != null && Settings != null)
		{
			BeginWindows();
			int i = 0;
			foreach (var nodeData in CurrentTarget.Nodes)
			{
				var position = origin + nodeData.graphPosition;

				var rect = new Rect(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), 200, 60);
				var newRect = GUILayout.Window(i, rect, (id) =>
				{
					HandleNodeEvents(nodeData.nodeObject);
					DrawNode(nodeData.nodeObject, ref nodeData.isExpanded);
					GUI.DragWindow(new Rect(0, 0, 200, 30));
				}, new GUIContent(), Settings.NodeGUIStyle);
				i++;
				nodeData.graphPosition = newRect.position - origin;
			}
			EndWindows();
		}
	}

	void HandleNodeEvents(UnityEngine.Object node)
	{
		if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
			Selection.activeObject = node;
		else if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
		{
			var genericMenu = new GenericMenu();
			genericMenu.AddItem(new GUIContent("Delete"), false, () =>
			{
				CurrentTarget.DeleteNode(node);
			});
			genericMenu.ShowAsContext();
			Event.current.Use();
		}
	}

	void DrawNode(UnityEngine.Object node, ref bool isExpanded)
	{
		EditorGUILayout.BeginVertical();
		EditorGUIUtility.labelWidth = Settings.DefaultLabelWidth;
		EditorGUILayout.LabelField(node.name, Settings.NodeHeaderStyle);

		var serializedObject = new SerializedObject(node);
		serializedObject.Update();

		var iterator = serializedObject.GetIterator();
		iterator.NextVisible(true);

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		isExpanded = EditorGUILayout.Toggle(isExpanded, Settings.NodeContentToggleStyle);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		if (isExpanded)
			DrawPropertiesRecursive(iterator);

		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.EndVertical();
	}

	bool DrawPropertiesRecursive(SerializedProperty iterator)
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

				if (Settings.IndentHeadersOnly)
					EditorGUI.indentLevel = 0;

				if (iterator.isExpanded)
				{
					EditorGUILayout.BeginVertical(Settings.SeparatorStyle);

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
					if (nodeAttributes.Any(x => x.GraphType == CurrentTarget.GetType()))
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
						CurrentTarget.CreateNode(derivedType, ConvertScreenCoordsToZoomCoords(mousePosition), derivedType.Name);
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