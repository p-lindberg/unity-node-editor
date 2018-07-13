using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Linq;
using System;
using System.Reflection;

namespace DataDesigner
{
	public class NodeEditor : ZoomableEditorWindow
	{
		public event Action PostDraw;

		[SerializeField] UnityEngine.Object currentTarget;

		public UnityEngine.Object CurrentTarget
		{
			get { return currentTarget; }
			set
			{
				currentTarget = value;
				EditorUtility.SetDirty(this);
			}
		}

		Vector2 currentMousePosition;
		Dictionary<NodeGraphData.NodeData, NodeView> nodeViews = new Dictionary<NodeGraphData.NodeData, NodeView>();
		Dictionary<UnityEngine.Object, NodeGraphData> nodeGraphDataMap = new Dictionary<UnityEngine.Object, NodeGraphData>();

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
			var settingsGuids = AssetDatabase.FindAssets("Node Editor Settings");
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

		public static void OpenWindow(UnityEngine.Object target)
		{
			var window = Init<NodeEditor>("Node Editor");
			if (target != null)
				window.CurrentTarget = target;

			window.Reset();
			window.wantsMouseMove = true;
			window.wantsMouseEnterLeaveWindow = true;
		}

		void Reset()
		{
			nodeViews = new Dictionary<NodeGraphData.NodeData, NodeView>();
		}

		protected override void DrawUtilityBarContents()
		{
			base.DrawUtilityBarContents();
			GUILayout.FlexibleSpace();
			GUILayout.Label(CurrentTarget != null ? CurrentTarget.name : "No graph selected", Settings.GraphHeaderStyle);
		}

		[MenuItem("Assets/Edit in Node Editor")]
		static void EditInNodeEditor()
		{
			OpenWindow(Selection.activeObject);
		}

		[MenuItem("Assets/Edit in Node Editor", true)]
		static bool EditInNodeEditorValidate()
		{
			if (Selection.activeObject == null)
				return false;

			return Selection.activeObject.GetType().IsSubclassOf(typeof(ScriptableObject));
		}

		// TODO: Allow user, via a menu option, to specify that a specific type of scriptable object should be opened on double click.
		/*[OnOpenAssetAttribute(1)]
	public static bool OnOpenAsset(int instanceID, int line)
	{
		var targetObject = EditorUtility.InstanceIDToObject(instanceID);
		if (targetObject != null)
		{
			if (targetObject.GetType().GetCustomAttributes(typeof(NodeGraphAttribute), true).Count() == 0)
				return false;

			OpenWindow(targetObject);
			return true;
		}

		return false;
	}*/

		NodeView GetNodeView(NodeGraphData.NodeData nodeData)
		{
			NodeView nodeView;
			if (nodeViews.TryGetValue(nodeData, out nodeView))
			{
				if (nodeView.NodeObject == null)
				{
					nodeViews.Remove(nodeData);
					return GetNodeView(nodeData);
				}

				return nodeView;
			}
				
			var newNodeView = new NodeView(this, nodeData);
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
			genericMenu.AddItem(new GUIContent("Delete/Confirm"), false, () =>
				{
					DeleteNode(nodeData);
				});

			var nodeGraphData = GetNodeGraphData(nodeData.nodeObject, createIfMissing: false);
			if (nodeGraphData == null)
			{
				genericMenu.AddItem(new GUIContent("Convert to subgraph"), false, () =>
					{
						nodeGraphData = GetNodeGraphData(nodeData.nodeObject);
						nodeViews.Remove(nodeData);
					});	
			}
			else
			{
				genericMenu.AddItem(new GUIContent("Subgraph/Edit"), false, () =>
					{
						OpenWindow(nodeData.nodeObject);
					});

				/*genericMenu.AddItem(new GUIContent("Subgraph/Extract/Confirm"), false, () =>
					{
						// TODO: Implement.
					});*/
			}

			foreach (var scriptableObject in GetSerializedScriptableObjectFields())
			{
				if (scriptableObject.FieldType.IsAssignableFrom(nodeData.nodeObject.GetType()))
					genericMenu.AddItem(new GUIContent("Set as/" + scriptableObject.Name), false, () =>
						{
							Undo.RecordObject(CurrentTarget, "Changed exposed node");
							scriptableObject.SetValue(CurrentTarget, nodeData.nodeObject);
							SaveAllChanges(CurrentTarget);
						});
			}
		}

		void DeleteNode(NodeGraphData.NodeData nodeData)
		{
			var nodeGraphData = GetNodeGraphData(nodeData.nodeObject, createIfMissing: false);

			RecordGraphUndoState(CurrentTarget, "Deleted node");
			GetNodeGraphData(CurrentTarget).RemoveNode(nodeData.nodeObject);
			nodeViews.Remove(nodeData);
			Undo.DestroyObjectImmediate(nodeData.nodeObject);

			if (nodeGraphData != null)
			{
				foreach (var nestedNode in nodeGraphData.Nodes)
				{
					nodeViews.Remove(nestedNode);
					Undo.DestroyObjectImmediate(nestedNode.nodeObject);
				}

				Undo.DestroyObjectImmediate(nodeGraphData);
			}

			SaveAllChanges(CurrentTarget);
		}

		static void SaveAllChanges(UnityEngine.Object graph)
		{
			foreach (var subAsset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(graph)))
				EditorUtility.SetDirty(subAsset);
			
			AssetDatabase.SaveAssets();
		}

		static void RecordGraphUndoState(UnityEngine.Object graph, string undoMessage)
		{
			Undo.RecordObjects(AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(graph)), undoMessage);
		}

		IEnumerable<FieldInfo> GetSerializedScriptableObjectFields()
		{
			foreach (var field in CurrentTarget.GetType().GetFields(NodeEditorUtilities.StandardBindingFlags).Where(x => x.FieldType.IsSubclassOf(typeof(ScriptableObject))))
				if (field.IsPublic || field.GetCustomAttributes(typeof(SerializeField), true).Count() > 0)
					yield return field;
		}

		NodeGraphData GetNodeGraphData(UnityEngine.Object graph, bool createIfMissing = true)
		{
			NodeGraphData nodeGraphData = null;
			if (!nodeGraphDataMap.TryGetValue(graph, out nodeGraphData))
			{
				var subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(graph));
				nodeGraphData = subAssets.OfType<NodeGraphData>().FirstOrDefault(x => x.GraphObject == graph);
				if (nodeGraphData == null && createIfMissing)
				{
					nodeGraphData = ScriptableObject.CreateInstance<NodeGraphData>();
					nodeGraphData.name = "Node Graph Data";

					var serializedObject = new SerializedObject(nodeGraphData);
					var graphObject = serializedObject.FindProperty("graphObject");
					graphObject.objectReferenceValue = graph;
					serializedObject.ApplyModifiedPropertiesWithoutUndo();

					AssetDatabase.AddObjectToAsset(nodeGraphData, graph);
					Undo.RegisterCreatedObjectUndo(nodeGraphData, "Converted node into a graph");
					SaveAllChanges(graph);
				}

				if (nodeGraphData != null)
				{
					var graphType = graph.GetType();
					var graphAttribute = graphType.GetCustomAttributes(typeof(GraphAttribute), true).FirstOrDefault() as GraphAttribute;
					nodeGraphData.IncludeGraphAsNode = graphAttribute != null ? graphAttribute.ShowInDiagram : false;
					nodeGraphDataMap[graph] = nodeGraphData;
				}
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

				foreach (var field in GetSerializedScriptableObjectFields())
					if (field.GetValue(CurrentTarget) as UnityEngine.Object == nodeData.nodeObject)
						nodeView.DrawTag(field.Name);
			}

			EndWindows();
		}

		protected override void OnHandleEvents()
		{
			if (CurrentTarget == null || Settings == null)
				return;

			if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
			{
				var genericMenu = new GenericMenu();
				var mousePosition = Event.current.mousePosition;
				foreach (var connectableType in GetConnectableTypes())
				{
					genericMenu.AddItem(new GUIContent("Create/" + connectableType.Name), false, () =>
						{
							var nodePosition = NodeEditorUtilities.RoundVectorToIntegerValues(ConvertScreenCoordsToZoomCoords(mousePosition));
							GetNodeGraphData(CurrentTarget).CreateNode(connectableType, nodePosition, connectableType.Name);
							SaveAllChanges(CurrentTarget);
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
					SaveAllChanges(CurrentTarget);

				Repaint();
			}

			if (PostDraw != null)
			{
				var temp = PostDraw;
				PostDraw = null;
				temp.Invoke();
			}
		}

		IEnumerable<Type> GetConnectableTypes()
		{
			var connectableTypes = new HashSet<Type>();
			connectableTypes.UnionWith(nodeViews.Values.SelectMany(x => x.ConnectionTypes));
			connectableTypes.UnionWith(GetSerializedScriptableObjectFields().Select(x => x.FieldType));
			var derivedTypes = new HashSet<Type>();
			derivedTypes.UnionWith(connectableTypes.SelectMany(x => NodeEditorUtilities.GetDerivedTypes(x, false, false)));
			connectableTypes.UnionWith(derivedTypes);
			return connectableTypes;
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
}