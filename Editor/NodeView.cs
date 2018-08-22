using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

public class TextInputPopup : PopupWindowContent
{
	Action<string> onTextChanged;
	Action onComplete;
	string text;

	public TextInputPopup(string text, Action<string> onTextChanged, Action onComplete)
	{
		this.text = text;
		this.onTextChanged = onTextChanged;
		this.onComplete = onComplete;
	}

	public override Vector2 GetWindowSize()
	{
		return new Vector2(200, EditorGUIUtility.singleLineHeight);
	}

	public override void OnGUI(Rect rect)
	{
		text = GUILayout.TextField(text);
		onTextChanged.Invoke(text);
	}

	public override void OnOpen()
	{
		
	}

	public override void OnClose()
	{
		onComplete.Invoke();
	}
}

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
		SerializedProperty propertyIterator;

		Dictionary<KeyPair<SerializedObject, string>, NodeConnector> nodeConnectors = new Dictionary<KeyPair<SerializedObject, string>, NodeConnector>();
		Dictionary<object, SerializedObject> nestedObjects = new Dictionary<object, SerializedObject>();

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

		NodeConnector GetNodeConnector(SerializedObject serializedObject, string propertyPath)
		{
			NodeConnector nodeConnector;
			if (nodeConnectors.TryGetValue(KeyPair.From(serializedObject, propertyPath), out nodeConnector))
				return nodeConnector;

			nodeConnector = new NodeConnector(this, serializedObject, propertyPath);
			nodeConnector.OnDeath += () => postDraw += () => nodeConnectors.Remove(KeyPair.From(serializedObject, propertyPath));
			nodeConnectors[KeyPair.From(serializedObject, propertyPath)] = nodeConnector;
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
			foreach (var nestedObject in nestedObjects)
				nestedObject.Value.Update();			

			currentPropertyHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			if (nodeData.isExpanded)
			{
				EditorGUILayout.BeginVertical(Settings.SeparatorStyle);

				if (propertyIterator == null)
					propertyIterator = serializedObject.GetIterator();
				else
					propertyIterator.Reset();
				
				if (propertyIterator.NextVisible(true))
					DrawPropertiesRecursive(propertyIterator, 0);

				EditorGUILayout.EndVertical();
			}
				
			serializedObject.ApplyModifiedProperties();
			foreach (var nestedObject in nestedObjects)
				nestedObject.Value.ApplyModifiedProperties();
			
			EditorGUILayout.EndVertical();

			currentPropertyHeight = 0f;
		}

		protected bool DrawPropertiesRecursive(SerializedProperty iterator, int indentationDepth)
		{
			bool doContinue = iterator.NextVisible(true);
			var propertyDepth = doContinue != false ? iterator.depth : 0;
			while (doContinue && iterator.depth >= propertyDepth)
			{
				if (Settings.IndentNested)
					EditorGUI.indentLevel = iterator.depth + indentationDepth;
				
				doContinue = DrawProperty(iterator, propertyDepth, indentationDepth);
			}

			return doContinue;
		}

		bool DrawProperty(SerializedProperty iterator, int propertyDepth, int indentationDepth, bool showDisplayName = true)
		{
			if (iterator.isArray && iterator.type != "string")
				return DrawArray(iterator, propertyDepth, indentationDepth, showDisplayName);
			else if (iterator.hasVisibleChildren)
				return DrawNestedClass(iterator, propertyDepth, indentationDepth, showDisplayName);
			else
			{
				DrawField(iterator, indentationDepth, showDisplayName);
				return iterator.NextVisible(iterator.isExpanded);
			}
		}

		bool DrawNestedClass(SerializedProperty property, int propertyDepth, int indentationDepth, bool showDisplayName = true)
		{
			property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, property.displayName, true);
			currentPropertyHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			if (Settings.IndentHeadersOnly)
				EditorGUI.indentLevel = 0;

			if (property.isExpanded)
			{
				currentPropertyHeight += EditorGUIUtility.standardVerticalSpacing;
				EditorGUILayout.BeginVertical(Settings.SeparatorStyle);

				var proceed = DrawPropertiesRecursive(property, indentationDepth);

				EditorGUILayout.EndVertical();

				if (proceed)
				{
					if (propertyDepth == property.depth)
						currentPropertyHeight += EditorGUIUtility.standardVerticalSpacing;

					return true;
				}
				else
					return false;
			}

			return property.NextVisible(true);
		}

		void DrawField(SerializedProperty property, int indentationDepth, bool showDisplayName = true)
		{
			if (property.propertyType == SerializedPropertyType.ObjectReference)
				DrawObjectField(property, indentationDepth, showDisplayName);
			else
				DrawPropertyField(property, showDisplayName);
		}

		SerializedProperty GetNestedPropertyIterator(SerializedProperty property)
		{
			SerializedObject nestedSerializedObject;
			if (!nestedObjects.TryGetValue(property.objectReferenceValue, out nestedSerializedObject))
			{
				nestedSerializedObject = new SerializedObject(property.objectReferenceValue);
				nestedObjects[property.objectReferenceValue] = nestedSerializedObject;
			}

			return nestedSerializedObject.GetIterator();
		}

		void DrawObjectField(SerializedProperty property, int indentationDepth, bool showDisplayName = true)
		{
			var propertyType = NodeEditorUtilities.GetPropertyType(property);
			if (propertyType.GetCustomAttributes(typeof(EmbeddedAttribute), true).Any())
			{
				if (property.objectReferenceValue != null)
				{
					var nestedIterator = GetNestedPropertyIterator(property);
						
					EditorGUILayout.BeginHorizontal();
					nestedIterator.isExpanded = EditorGUILayout.Foldout(nestedIterator.isExpanded, property.objectReferenceValue.name, Settings.Foldouts);

					if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
					{
						var lastRect = GUILayoutUtility.GetLastRect();
						if (lastRect.Contains(Event.current.mousePosition))
						{
							var target = property.objectReferenceValue;
							PopupWindow.Show(lastRect, new TextInputPopup(target.name, (text) => target.name = text, () => NodeEditor.SaveAllChanges()));
							Event.current.Use();
						}
					}

					currentPropertyHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

					if (GUILayout.Button("-", GUILayout.Width(15), GUILayout.Height(12)))
					{
						var instance = property.objectReferenceValue;
						property.objectReferenceValue = null;
						property.serializedObject.ApplyModifiedProperties();
						nestedObjects.Remove(instance);
						NodeEditor.DestroyEmbeddedObject(instance);
					}
					EditorGUILayout.EndHorizontal();

					if (nestedIterator.isExpanded && nestedIterator.NextVisible(true))
						DrawPropertiesRecursive(nestedIterator, indentationDepth);
				}
				else
				{
					EditorGUILayout.BeginHorizontal();
					if (GUILayout.Button("+", GUILayout.Width(15), GUILayout.Height(12)))
						DrawObjectCreationMenu(property.Copy());

					EditorGUILayout.LabelField("Empty");
					EditorGUILayout.EndHorizontal();

					currentPropertyHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				}
			}
			else
			{
				if (propertyType.GetCustomAttributes(typeof(NodeAttribute), true).Any())
					GetNodeConnector(property.serializedObject, property.propertyPath).SetDrawProperties(currentPropertyHeight, true);

				DrawPropertyField(property, showDisplayName);
			}
		}

		void DrawObjectCreationMenu(SerializedProperty property)
		{
			var genericMenu = new GenericMenu();
			var elementType = NodeEditorUtilities.GetPropertyType(property);
			var derivedTypes = NodeEditorUtilities.GetDerivedTypes(elementType, true, false);
			foreach (var derivedType in derivedTypes)
			{
				genericMenu.AddItem(new GUIContent("Create/" + ObjectNames.NicifyVariableName(derivedType.Name)), false, () =>
					{
						var embeddedObject = NodeEditor.CreateEmbeddedObject(derivedType);
						property.objectReferenceValue = embeddedObject;
						property.serializedObject.ApplyModifiedProperties();
					});
			}

			genericMenu.ShowAsContext();
		}

		void DrawPropertyField(SerializedProperty property, bool showDisplayName = true)
		{
			if (showDisplayName)
				EditorGUILayout.PropertyField(property);
			else
				EditorGUILayout.PropertyField(property, GUIContent.none);
			
			currentPropertyHeight += EditorGUI.GetPropertyHeight(property) + EditorGUIUtility.standardVerticalSpacing;
		}

		bool DrawArray(SerializedProperty property, int basePropertyDepth, int indentationDepth, bool showDisplayName = true)
		{
			DrawArrayHeader(property);

			var arrayProperty = property.Copy();
			var arrayPropertyDepth = property.depth;
			var doContinue = property.NextVisible(true);
			int index = 0;
			while (doContinue && property.depth > arrayPropertyDepth)
			{
				if (property.propertyType == SerializedPropertyType.ArraySize)
				{
					doContinue = property.NextVisible(true);
					continue;
				}

				EditorGUILayout.BeginHorizontal();

				if (GUILayout.Button(new GUIContent(">"), GUILayout.Width(15), GUILayout.Height(12)))
					DrawArrayControls(arrayProperty, index);
				
				EditorGUILayout.BeginVertical();
				doContinue = DrawProperty(property, basePropertyDepth, indentationDepth, showDisplayName: false);
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
				
				index++;
			}

			return doContinue;
		}

		void DrawArrayHeader(SerializedProperty property)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(property.displayName, EditorStyles.boldLabel);

			if (property.arraySize > 0 && GUILayout.Button("-", GUILayout.Width(15), GUILayout.Height(12)))
				property.DeleteArrayElementAtIndex(property.arraySize - 1);
			
			if (GUILayout.Button("+", GUILayout.Width(15), GUILayout.Height(12)))
			{
				property.InsertArrayElementAtIndex(property.arraySize);
				var insertedElement = property.GetArrayElementAtIndex(property.arraySize - 1);
				if (insertedElement.propertyType == SerializedPropertyType.ObjectReference && insertedElement.objectReferenceValue != null)
					property.DeleteArrayElementAtIndex(property.arraySize - 1);
			}

			EditorGUILayout.EndHorizontal();
			
			currentPropertyHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}

		void DrawArrayControls(SerializedProperty arrayProperty, int index)
		{
			var genericMenu = new GenericMenu();
			genericMenu.AddItem(new GUIContent("Delete"), false, () =>
				{
					arrayProperty.DeleteArrayElementAtIndex(index);
					arrayProperty.serializedObject.ApplyModifiedProperties();
				});

			if (index > 0)
				genericMenu.AddItem(new GUIContent("Move Up"), false, () =>
					{
						arrayProperty.MoveArrayElement(index, index - 1);
						arrayProperty.serializedObject.ApplyModifiedProperties();
					});

			if (index < arrayProperty.arraySize - 1)
				genericMenu.AddItem(new GUIContent("Move Down"), false, () =>
					{
						arrayProperty.MoveArrayElement(index, index + 1);
						arrayProperty.serializedObject.ApplyModifiedProperties();
					});
					
			genericMenu.ShowAsContext();
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
					if (propertyType != null)
					{
						if (propertyType.GetCustomAttributes(typeof(EmbeddedAttribute), true).Any() && iterator.objectReferenceValue != null)
						{
							var nestedIterator = GetNestedPropertyIterator(iterator);
							FindConnectionsRecursive(nestedIterator);
						}
						else if (propertyType.GetCustomAttributes(typeof(NodeAttribute), true).Any())
							GetNodeConnector(iterator.serializedObject, iterator.propertyPath).Initialize();
					}
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