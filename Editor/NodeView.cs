using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

// TODO: Highlight selected, highlight when mousing over a connectable node while connecting
namespace DataDesigner
{
	public class NodeView
	{
		public event System.Action<GenericMenu> OnShowContextMenu;

		public bool IsDead { get { return nodeData == null; } }

		public NodeEditor NodeEditor { get; private set; }

		public NodeViewSettings Settings { get { return NodeEditor.Settings.DefaultNodeViewSettings; } }

		public virtual GUIStyle GUIStyle { get { return Settings.GUIStyle; } }

		public UnityEngine.Object NodeObject { get { return nodeData.nodeObject; } }

		float currentPropertyHeight;
		bool dragging;
		Vector2 origin;
		NodeGraphData.NodeData nodeData;
		Vector2 size;
		SerializedObject serializedObject;
		Dictionary<string, NodeConnector> nodeConnectors = new Dictionary<string, NodeConnector>();
		System.Action postDraw;
		bool renaming;

		public NodeView(NodeEditor nodeEditor, NodeGraphData.NodeData nodeData)
		{
			this.NodeEditor = nodeEditor;
			this.nodeData = nodeData;
			serializedObject = new SerializedObject(nodeData.nodeObject);

			var iterator = serializedObject.GetIterator();
			if (iterator.NextVisible(true))
				FindConnectionsRecursive(iterator);
		}

		Rect GetWindowRectInternal()
		{
			var minSize = nodeData.isExpanded ? Settings.MinimumSize : Settings.MinimumSizeCollapsed;
			return new Rect(origin + nodeData.graphPosition, minSize);
		}

		NodeConnector GetNodeConnector(string propertyPath, Type propertyType)
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
				var temp = postDraw;
				postDraw = null;
				temp.Invoke();
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
				DrawRightClickContextMenu();
				Event.current.Use();
			}

			dragging = Event.current.type == EventType.MouseDrag;
		}

		void DrawRightClickContextMenu()
		{
			var genericMenu = new GenericMenu();
			genericMenu.AddItem(new GUIContent(nodeData.isExpanded ? "Collapse" : "Expand"), false, () =>
				{
					nodeData.isExpanded = !nodeData.isExpanded;
				});

			if (OnShowContextMenu != null)
				OnShowContextMenu.Invoke(genericMenu);

			NodeEditor.PostDraw += () => genericMenu.ShowAsContext();
		}

		public void DrawTag(string tag)
		{
			var labelRect = GetWindowRect();
			labelRect.position = new Vector2(labelRect.position.x, labelRect.position.y - 2 * EditorGUIUtility.singleLineHeight);
			labelRect.height = 2 * EditorGUIUtility.singleLineHeight;
			GUILayout.BeginArea(labelRect, "");
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label(tag, Settings.TagStyle);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.EndArea();
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

		void DrawHeader()
		{
			if (renaming)
			{
				nodeData.nodeObject.name = EditorGUILayout.TextField(nodeData.nodeObject.name);

				if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return)
				{
					renaming = false;
					Event.current.Use();
				}
			}
			else
				GUILayout.Label(nodeData.nodeObject.name, Settings.NodeHeaderStyle);

			if (Event.current.type == EventType.MouseDown)
			{
				var lastRect = GUILayoutUtility.GetLastRect();
				if (lastRect.Contains(Event.current.mousePosition) && Event.current.clickCount > 1)
				{
					renaming = true;
					Event.current.Use();
				}
				else
					renaming = false;
			}
		}

		void DrawCollapsedContents()
		{
			EditorGUILayout.BeginVertical();
			GUILayout.FlexibleSpace();
			DrawHeader();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndVertical();
		}

		void DrawExpandedContents()
		{
			EditorGUILayout.BeginVertical();
			EditorGUIUtility.labelWidth = Settings.MinimumLabelWidth;
			EditorGUIUtility.fieldWidth = Settings.MinimumFieldWidth;
			DrawHeader();
			GUILayout.Space(EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);

			currentPropertyHeight += EditorGUIUtility.singleLineHeight + 3 * EditorGUIUtility.standardVerticalSpacing;

			serializedObject.Update();

			currentPropertyHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			if (nodeData.isExpanded)
			{
				EditorGUILayout.BeginVertical(Settings.SeparatorStyle);

				var iterator = serializedObject.GetIterator();
				if (iterator.NextVisible(true))
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
			var depth = next != false ? iterator.depth : 0;
			while (next && iterator.depth >= depth)
			{
				if (Settings.IndentNested)
					EditorGUI.indentLevel = iterator.depth;
				
				if (iterator.hasVisibleChildren)
				{
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
						if (propertyType.IsSubclassOf(typeof(ScriptableObject)))
							GetNodeConnector(iterator.propertyPath, propertyType).SetDrawProperties(currentPropertyHeight, true);
					}

					currentPropertyHeight += EditorGUI.GetPropertyHeight(iterator) + EditorGUIUtility.standardVerticalSpacing;
				}

				next = iterator.NextVisible(iterator.isExpanded);
			}

			return next;
		}

		protected bool FindConnectionsRecursive(SerializedProperty iterator)
		{
			bool next = iterator.NextVisible(true);
			var depth = next != false ? iterator.depth : 0;
			while (next && iterator.depth >= depth)
			{
				if (iterator.hasVisibleChildren)
				{
					if (FindConnectionsRecursive(iterator))
						continue;
					else
						return false;
				}
				else if (iterator.propertyType == SerializedPropertyType.ObjectReference)
				{
					var propertyType = NodeEditorUtilities.GetPropertyType(iterator);
					if (propertyType.IsSubclassOf(typeof(ScriptableObject)))
						GetNodeConnector(iterator.propertyPath, propertyType).Initialize();
				}

				next = iterator.NextVisible(true);
			}

			return next;
		}

		public void ResetFocus()
		{
			renaming = false;
		}
	}
}