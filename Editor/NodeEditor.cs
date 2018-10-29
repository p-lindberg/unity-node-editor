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
	public class NodeEditor : ZoomableEditorWindow, IView
	{
		public event Action PostDraw;

		[SerializeField] UnityEngine.Object currentTarget;

		public UnityEngine.Object CurrentTarget
		{
			get { return currentTarget; }
			set
			{
				currentTarget = value;
				currentTargetSerializedObject = null;
				EditorUtility.SetDirty(this);
			}
		}

		SerializedObject currentTargetSerializedObject;

		SerializedObject CurrentTargetSerializedObject
		{
			get
			{
				if (CurrentTarget == null)
					return null;

				if (currentTargetSerializedObject == null)
					currentTargetSerializedObject = new SerializedObject(CurrentTarget);

				return currentTargetSerializedObject;
			}
		}

		Vector2 currentMousePosition;
		Dictionary<NodeGraphData.NodeData, NodeView> nodeViews = new Dictionary<NodeGraphData.NodeData, NodeView>();
		Dictionary<UnityEngine.Object, NodeGraphData> nodeGraphDataMap = new Dictionary<UnityEngine.Object, NodeGraphData>();
		Stack<UnityEngine.Object> graphStack = new Stack<UnityEngine.Object>();

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

		public IEnumerable<IView> SubViews { get { return nodeViews.Values; } }

		[MenuItem("Window/Node Editor")]
		public static void OpenWindow()
		{
			OpenWindow(null);
		}

		public Rect GetWindowRect()
		{
			return position;
		}

		public static void OpenWindow(UnityEngine.Object target)
		{
			var window = Init<NodeEditor>("Node Editor");
			if (target != null)
			{
				if (window.CurrentTarget != null)
					window.graphStack.Push(window.CurrentTarget);

				window.CurrentTarget = target;
			}

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

			if (GUILayout.Button("Back", EditorStyles.toolbarButton))
				GoBack();

			if (GUILayout.Button("Clean", EditorStyles.toolbarButton))
				RemoveUnreachableObjects();

			GUILayout.FlexibleSpace();
			GUILayout.Label(CurrentTarget != null ? CurrentTarget.name : "No graph selected", Settings.GraphHeaderStyle);
		}

		void GoBack()
		{
			if (graphStack.Count > 0)
			{
				currentTarget = graphStack.Pop();
				Reset();
			}
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
				if (nodeView.ViewObject == null)
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

			foreach (var property in NodeEditorUtilities.GetExposedObjectFields(CurrentTargetSerializedObject, false, nodeData.nodeObject.GetType(), true))
			{
				var propertyCopy = property.Copy();
				genericMenu.AddItem(new GUIContent("Set as/" + propertyCopy.displayName), false, () =>
					{
						propertyCopy.objectReferenceValue = nodeData.nodeObject;
						propertyCopy.serializedObject.ApplyModifiedProperties();
					});
			}

			foreach (var property in NodeEditorUtilities.GetExposedObjectArrays(CurrentTargetSerializedObject, false, nodeData.nodeObject.GetType(), true))
			{
				var propertyCopy = property.Copy();
				int foundIndex = -1;
				for (int i = 0; i < propertyCopy.arraySize; i++)
				{
					if (propertyCopy.GetArrayElementAtIndex(i).objectReferenceValue == nodeData.nodeObject)
					{
						foundIndex = i;
						break;
					}
				}

				if (foundIndex == -1)
					genericMenu.AddItem(new GUIContent("Set as/" + propertyCopy.displayName), false, () =>
						{
							propertyCopy.serializedObject.Update();
							propertyCopy.InsertArrayElementAtIndex(propertyCopy.arraySize);
							var insertedProperty = propertyCopy.GetArrayElementAtIndex(propertyCopy.arraySize - 1);
							insertedProperty.objectReferenceValue = nodeData.nodeObject;
							propertyCopy.serializedObject.ApplyModifiedProperties();
						});
				else
					genericMenu.AddItem(new GUIContent("Unset as/" + propertyCopy.displayName), false, () =>
						{
							propertyCopy.serializedObject.Update();
							propertyCopy.DeleteArrayElementAtIndex(foundIndex);
							propertyCopy.DeleteArrayElementAtIndex(foundIndex);
							propertyCopy.serializedObject.ApplyModifiedProperties();
						});
			}
		}

		void DeleteNode(NodeGraphData.NodeData nodeData)
		{
			var nodeGraphData = GetNodeGraphData(nodeData.nodeObject, createIfMissing: false);

			RecordGraphUndoState(CurrentTarget, "Deleted node");

			// Remove the node from any exposed lists on the graph.
			foreach (var property in NodeEditorUtilities.GetExposedObjectArrays(CurrentTargetSerializedObject, false, nodeData.nodeObject.GetType(), true))
			{
				for (int i = property.arraySize - 1; i >= 0; i--)
				{
					if (property.GetArrayElementAtIndex(i).objectReferenceValue == nodeData.nodeObject)
					{
						property.DeleteArrayElementAtIndex(i);
						property.DeleteArrayElementAtIndex(i);
						property.serializedObject.ApplyModifiedProperties();
					}
				}
			}

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

		public void SaveAllChanges(UnityEngine.Object graph = null)
		{
			if (graph == null)
				graph = CurrentTarget;

			foreach (var subAsset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(graph)))
				EditorUtility.SetDirty(subAsset);

			AssetDatabase.SaveAssets();
		}

		static void RecordGraphUndoState(UnityEngine.Object graph, string undoMessage)
		{
			Undo.RecordObjects(AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(graph)), undoMessage);
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
			}

			foreach (var property in NodeEditorUtilities.GetExposedObjectFields(CurrentTargetSerializedObject))
			{
				var nodeView = GetNodeView(property.objectReferenceValue);
				if (nodeView != null)
					nodeView.DrawTag(property.displayName);
			}

			foreach (var property in NodeEditorUtilities.GetExposedObjectArrays(CurrentTargetSerializedObject))
			{
				for (int i = 0; i < property.arraySize; i++)
				{
					if (property.GetArrayElementAtIndex(i).propertyType != SerializedPropertyType.ObjectReference)
						break;

					var nodeView = GetNodeView(property.GetArrayElementAtIndex(i).objectReferenceValue);
					if (nodeView != null)
						nodeView.DrawTag(property.displayName);
				}
			}

			EndWindows();
		}

		protected override void OnHandleEvents()
		{
			if (CurrentTarget == null || Settings == null)
				return;

			if (Event.current.type == EventType.MouseDown)
			{
				if (Event.current.button == 3)
				{
					GoBack();
					Event.current.Use();
				}
				else if (Event.current.button == 0)
				{
					foreach (var nodeView in nodeViews.Values)
						nodeView.ResetFocus();

					Event.current.Use();
				}
				else if (Event.current.button == 1)
				{
					var genericMenu = new GenericMenu();
					var mousePosition = Event.current.mousePosition;
					foreach (var nodeType in GetNodeTypes())
					{
						var menuPathAttribute = nodeType.GetCustomAttributes(typeof(MenuPathAttribute), true).FirstOrDefault() as MenuPathAttribute;
						genericMenu.AddItem(new GUIContent("Create/" + (menuPathAttribute != null ? menuPathAttribute.path : nodeType.Name)), false, () =>
							{
								var nodePosition = NodeEditorUtilities.RoundVectorToIntegerValues(ConvertScreenCoordsToZoomCoords(mousePosition));
								GetNodeGraphData(CurrentTarget).CreateNode(nodeType, nodePosition, nodeType.Name);
								SaveAllChanges(CurrentTarget);
							});
					}

					genericMenu.ShowAsContext();
					Event.current.Use();
				}
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

		IEnumerable<Type> GetNodeTypes()
		{
			return AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(
				(type) =>
				{
					var nodeAttribute = type.GetCustomAttributes(typeof(NodeAttribute), true).FirstOrDefault() as NodeAttribute;
					if (nodeAttribute != null)
						return nodeAttribute.Graphs.FirstOrDefault(t => t == currentTarget.GetType() || CurrentTarget.GetType().IsSubclassOf(t)) != null;

					return false;
				}
			);
		}

		public IView GetViewAtPosition(Vector3 screenPosition)
		{
			return GetViewAtPositionRecursive(this, screenPosition);
		}

		public IView GetViewAtPositionRecursive(IView view, Vector3 screenPosition)
		{
			foreach (var subView in view.SubViews)
			{
				var foundView = GetViewAtPositionRecursive(subView, screenPosition);
				if (foundView != null)
					return foundView;

				if (subView.GetWindowRect().Contains(screenPosition))
					return subView;
			}

			return null;
		}

		public NodeView GetNodeViewAtPosition(Vector3 screenPosition)
		{
			return nodeViews.FirstOrDefault(x => x.Value.GetWindowRect().Contains(screenPosition)).Value;
		}

		public NodeView GetNodeView(UnityEngine.Object nodeObject)
		{
			return nodeViews.FirstOrDefault(x => x.Key.nodeObject == nodeObject).Value;
		}

		public EmbeddedObjectHandle GetEmbeddedObjectHandle(UnityEngine.Object nodeObject)
		{
			var kvp = nodeViews.FirstOrDefault(x => x.Value.EmbeddedObjectHandles.ContainsKey(nodeObject));
			if (kvp.Value != null)
				return kvp.Value.EmbeddedObjectHandles[nodeObject];

			return null;
		}

		public EmbeddedObjectHandle GetEmbeddedObjectHandleAtPosition(Vector3 screenPosition)
		{
			foreach (var nodeView in nodeViews.Values)
			{
				foreach (var handle in nodeView.EmbeddedObjectHandles.Values)
					if (handle.GetWindowRect().Contains(screenPosition))
						return handle;
			}

			return null;
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

		public UnityEngine.Object CreateEmbeddedObject(Type type)
		{
			var instance = ScriptableObject.CreateInstance(type);
			instance.name = ObjectNames.NicifyVariableName(type.Name);
			AssetDatabase.AddObjectToAsset(instance, CurrentTarget);
			SaveAllChanges(CurrentTarget);
			return instance;
		}

		public void DestroyEmbeddedObject(UnityEngine.Object embeddedObject)
		{
			Undo.DestroyObjectImmediate(embeddedObject);
			SaveAllChanges(CurrentTarget);
		}

		public void RemoveUnreachableObjects()
		{
			HashSet<UnityEngine.Object> reachableObjects = new HashSet<UnityEngine.Object>();
			var nodeGraphData = GetNodeGraphData(CurrentTarget);

			foreach (var node in nodeGraphData.Nodes.Select(x => x.nodeObject))
			{
				FindReachableObjects(node, reachableObjects);
				reachableObjects.Add(node);
			}

			reachableObjects.Add(nodeGraphData);
			reachableObjects.Add(CurrentTarget);

			var savedAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(CurrentTarget));
			foreach (var asset in savedAssets)
			{
				if (!reachableObjects.Contains(asset))
					Undo.DestroyObjectImmediate(asset);
			}

			SaveAllChanges(CurrentTarget);
			Reset();
		}

		void FindReachableObjects(UnityEngine.Object parent, HashSet<UnityEngine.Object> reachableObjects)
		{
			var serializedObject = new SerializedObject(parent);
			var iterator = serializedObject.GetIterator();
			do
			{
				if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue != null)
				{
					if (reachableObjects.Add(iterator.objectReferenceValue))
						FindReachableObjects(iterator.objectReferenceValue, reachableObjects);
				}
			} while (iterator.Next(true));
		}
	}
}