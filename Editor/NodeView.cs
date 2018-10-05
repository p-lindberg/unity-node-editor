using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using System.Reflection;

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
	public class NodeView : IView, IObjectView
	{
		public event System.Action<GenericMenu> OnShowContextMenu;

		public bool IsDead { get { return nodeData == null; } }

		public NodeEditor NodeEditor { get; private set; }

		public NodeViewSettings Settings { get { return NodeEditor.Settings.DefaultNodeViewSettings; } }

		public virtual GUIStyle GUIStyle { get { return Settings.GUIStyle; } }

		public UnityEngine.Object ViewObject { get { return nodeData.nodeObject; } }

		public IReadOnlyDictionary<UnityEngine.Object, EmbeddedObjectHandle> EmbeddedObjectHandles { get { return embeddedObjectHandles; } }

		public IReadOnlyDictionary<KeyPair<UnityEngine.Object, string>, OutputHandle> OutputHandles { get { return outputHandles; } }

		public IEnumerable<IView> SubViews
		{
			get
			{
				foreach (var view in embeddedObjectHandles.Values)
					yield return view;
				foreach (var view in nodeConnectors.Values)
					yield return view;
				foreach (var view in outputHandles.Values)
					yield return view;
				foreach (var view in inputHandles.Values)
					yield return view;
			}
		}

		float currentPropertyHeight;
		bool dragging;
		Vector2 origin;
		NodeGraphData.NodeData nodeData;
		Vector2 size;
		SerializedObject serializedObject;
		SerializedProperty propertyIterator;

		Dictionary<KeyPair<SerializedObject, string>, NodeConnector> nodeConnectors = new Dictionary<KeyPair<SerializedObject, string>, NodeConnector>();
		Dictionary<UnityEngine.Object, EmbeddedObjectHandle> embeddedObjectHandles = new Dictionary<UnityEngine.Object, EmbeddedObjectHandle>();
		Dictionary<object, SerializedObject> nestedObjects = new Dictionary<object, SerializedObject>();
		Dictionary<KeyPair<UnityEngine.Object, string>, OutputHandle> outputHandles = new Dictionary<KeyPair<UnityEngine.Object, string>, OutputHandle>();
		Dictionary<KeyPair<UnityEngine.Object, string>, InputHandle> inputHandles = new Dictionary<KeyPair<UnityEngine.Object, string>, InputHandle>();
		HashSet<string> IgnoreProperties = new HashSet<string>();

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

		EmbeddedObjectHandle GetEmbeddedObjectHandle(UnityEngine.Object embeddedObject)
		{
			EmbeddedObjectHandle embeddedObjectHandle;
			if (embeddedObjectHandles.TryGetValue(embeddedObject, out embeddedObjectHandle))
				return embeddedObjectHandle;

			embeddedObjectHandle = new EmbeddedObjectHandle(this, embeddedObject);
			embeddedObjectHandle.OnDeath += () => postDraw += () => embeddedObjectHandles.Remove(embeddedObject);
			embeddedObjectHandles[embeddedObject] = embeddedObjectHandle;
			return embeddedObjectHandle;
		}

		OutputHandle GetOutputHandle(UnityEngine.Object owner, string propertyName, Type type, Color? color = null)
		{
			OutputHandle outputHandle;
			var keyPair = KeyPair.From(owner, propertyName);
			if (outputHandles.TryGetValue(keyPair, out outputHandle))
				return outputHandle;

			outputHandle = new OutputHandle(this, owner, propertyName, type, color);
			outputHandle.OnDeath += () => postDraw += () => outputHandles.Remove(keyPair);
			outputHandles[keyPair] = outputHandle;
			return outputHandle;
		}

		InputHandle GetInputHandle(SerializedProperty serializedProperty, Type type, Color? color = null)
		{
			InputHandle inputHandle;
			var keyPair = KeyPair.From(serializedProperty.serializedObject.targetObject, serializedProperty.propertyPath);
			if (inputHandles.TryGetValue(keyPair, out inputHandle))
				return inputHandle;

			inputHandle = new InputHandle(this, serializedProperty.Copy(), type, color);
			inputHandle.OnDeath += () => postDraw += () => inputHandles.Remove(keyPair);
			inputHandles[keyPair] = inputHandle;
			return inputHandle;
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
				nodeConnector.Draw(origin);

			foreach (var embeddedObjectHandle in embeddedObjectHandles.Values)
				embeddedObjectHandle.Draw(origin);

			foreach (var outputHandle in outputHandles.Values)
				outputHandle.Draw(origin);

			foreach (var inputHandle in inputHandles.Values)
				inputHandle.Draw(origin);

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

			currentPropertyHeight += 2 * EditorGUIUtility.singleLineHeight + 4 * EditorGUIUtility.standardVerticalSpacing;

			DrawThroughput(serializedObject);

			serializedObject.Update();
			foreach (var nestedObject in nestedObjects)
				nestedObject.Value.Update();

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
			var propertyType = NodeEditorUtilities.GetPropertyType(iterator);
			if (IgnoreProperties.Contains(iterator.propertyPath) || propertyType.IsDefined(typeof(Input), true))
				return iterator.NextVisible(false);
			else if (iterator.isArray && iterator.type != "string")
				return DrawArray(iterator, propertyDepth, indentationDepth, showDisplayName);
			else if (iterator.hasVisibleChildren)
				return DrawNestedClass(iterator, propertyDepth, indentationDepth, showDisplayName);
			else
			{
				DrawField(iterator, indentationDepth, showDisplayName);
				return iterator.NextVisible(iterator.isExpanded);
			}
		}

		bool DrawThroughput(SerializedObject serializedObject)
		{
			var targetObjectType = serializedObject.targetObject.GetType();
			Queue<Action> outputDraws = new Queue<Action>();
			Queue<Action> inputDraws = new Queue<Action>();
			Queue<Action> outputWithFieldDraws = new Queue<Action>();
			foreach (var property in targetObjectType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
			{
				var outputAttribute = property.GetCustomAttribute(typeof(OutputAttribute), true) as OutputAttribute;
				if (outputAttribute != null)
				{
					// Find a SerializedProperty with matching name.
					SerializedProperty matchingProperty = null;
					var serializedPropertyIterator = serializedObject.GetIterator();
					while (serializedPropertyIterator.Next(true))
					{
						if (serializedPropertyIterator.name.ToLower() == property.Name.ToLower())
						{
							matchingProperty = serializedPropertyIterator.Copy();
							IgnoreProperties.Add(matchingProperty.propertyPath);
							break;
						}
					}

					var queue = matchingProperty != null ? outputWithFieldDraws : outputDraws;

					queue.Enqueue(() =>
					{
						GetOutputHandle(serializedObject.targetObject, property.Name, property.PropertyType, outputAttribute.color).SetDrawProperties(currentPropertyHeight, true, Alignment.Right);
						var connectionType = property.PropertyType;
						if (matchingProperty != null)
						{
							EditorGUILayout.PropertyField(matchingProperty, false);
							matchingProperty.serializedObject.ApplyModifiedProperties();
						}
						else
							EditorGUILayout.SelectableLabel(ObjectNames.NicifyVariableName(property.Name) + " (" + property.PropertyType.Name + ")",
															Settings.OutputStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing));
					});
				}
			}

			var iterator = serializedObject.GetIterator();
			var doContinue = iterator.Next(true);

			while (doContinue)
			{
				var serializedPropertyType = NodeEditorUtilities.GetPropertyType(iterator);
				Input inputTypeAttribute = null;
				if (serializedPropertyType != null)
					inputTypeAttribute = serializedPropertyType.GetCustomAttribute(typeof(Input), true) as Input;
				if (inputTypeAttribute != null)
				{
					var serializedProperty = iterator.Copy();
					inputDraws.Enqueue(() =>
					{
						GetInputHandle(serializedProperty, inputTypeAttribute.type, inputTypeAttribute.color).SetDrawProperties(currentPropertyHeight, true, Alignment.Left);
						EditorGUILayout.LabelField(serializedProperty.displayName + " (" + inputTypeAttribute.type.Name + ")", Settings.InputStyle, GUILayout.Width(5f * serializedProperty.displayName.Length));
					});

					doContinue = iterator.Next(false);
				}
				else
					doContinue = iterator.Next(true);
			}

			while (inputDraws.Count > 0 || outputDraws.Count > 0)
			{
				EditorGUILayout.BeginHorizontal();
				if (inputDraws.Count > 0)
					inputDraws.Dequeue().Invoke();

				if (outputDraws.Count > 0)
					outputDraws.Dequeue().Invoke();
				EditorGUILayout.EndHorizontal();

				currentPropertyHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			}

			while (outputWithFieldDraws.Count > 0)
			{
				outputWithFieldDraws.Dequeue().Invoke();
				currentPropertyHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			}

			return doContinue;
		}

		void DrawPropertyDecorators(SerializedProperty property)
		{
			var fieldInfo = NodeEditorUtilities.GetPropertyFieldInfo(property);
			var headers = fieldInfo.GetCustomAttributes(typeof(HeaderAttribute), true).Cast<HeaderAttribute>();
			foreach (var header in headers.OrderBy(x => x.order))
			{
				EditorGUILayout.LabelField(header.header, EditorStyles.boldLabel);
				currentPropertyHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			}
		}

		bool DrawNestedClass(SerializedProperty property, int propertyDepth, int indentationDepth, bool showDisplayName = true)
		{
			DrawPropertyDecorators(property);

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

		bool isFirstNested = false;

		void DrawObjectField(SerializedProperty property, int indentationDepth, bool showDisplayName = true)
		{
			var propertyType = NodeEditorUtilities.GetPropertyType(property);
			if (propertyType == null)
				return;

			var fieldInfo = NodeEditorUtilities.GetPropertyFieldInfo(property);
			if (fieldInfo.GetCustomAttributes(typeof(EmbeddedAttribute), true).Any())
			{
				DrawPropertyDecorators(property);

				if (showDisplayName)
				{
					EditorGUILayout.LabelField(property.displayName, EditorStyles.boldLabel);
					currentPropertyHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				}

				// Because unity seems to add an extra standard vertical spacing when drawing a
				// group inside a group, but only the first one.
				if (isFirstNested)
					currentPropertyHeight += EditorGUIUtility.standardVerticalSpacing;

				GUILayout.BeginVertical(Settings.EmbeddedObject);
				currentPropertyHeight += EditorGUIUtility.standardVerticalSpacing;
				isFirstNested = true;
				if (property.objectReferenceValue != null)
				{
					var nestedIterator = GetNestedPropertyIterator(property);

					EditorGUILayout.BeginHorizontal();
					nestedIterator.isExpanded = EditorGUILayout.Foldout(nestedIterator.isExpanded, property.objectReferenceValue.name, Settings.Foldouts);

					GetEmbeddedObjectHandle(property.objectReferenceValue).SetDrawProperties(currentPropertyHeight, true);

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

				isFirstNested = false;
				GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
				currentPropertyHeight += EditorGUIUtility.standardVerticalSpacing;
				GUILayout.EndVertical();
			}
			else
			{
				if (propertyType.GetCustomAttributes(typeof(NodeAttribute), true).Any())
				{
					var alignAttribute = (AlignAttribute)fieldInfo.GetCustomAttributes(typeof(AlignAttribute), true).FirstOrDefault();
					var alignment = alignAttribute != null ? alignAttribute.alignment : Alignment.Auto;
					GetNodeConnector(property.serializedObject, property.propertyPath).SetDrawProperties(currentPropertyHeight, true, alignment);
				}

				DrawPropertyField(property, showDisplayName);
			}

			GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
			currentPropertyHeight += EditorGUIUtility.standardVerticalSpacing;
		}

		void DrawObjectCreationMenu(SerializedProperty property)
		{
			var genericMenu = new GenericMenu();
			var elementType = NodeEditorUtilities.GetPropertyType(property);
			var derivedTypes = NodeEditorUtilities.GetDerivedTypes(elementType, true, false);
			foreach (var derivedType in derivedTypes)
			{
				var menuPathAttribute = derivedType.GetCustomAttributes(typeof(MenuPathAttribute), true).FirstOrDefault() as MenuPathAttribute;
				genericMenu.AddItem(new GUIContent(menuPathAttribute != null ? menuPathAttribute.path : ObjectNames.NicifyVariableName(derivedType.Name)), false, () =>
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
			DrawPropertyDecorators(property);

			property.isExpanded = DrawArrayHeader(property.isExpanded, property);

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

				if (arrayProperty.isExpanded)
				{
					EditorGUILayout.BeginHorizontal();

					if (GUILayout.Button(new GUIContent(">"), GUILayout.Width(15), GUILayout.Height(12)))
						DrawArrayControls(arrayProperty, index);

					EditorGUILayout.BeginVertical();
					doContinue = DrawProperty(property, basePropertyDepth, indentationDepth, showDisplayName: false);
					EditorGUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();
				}
				else
					doContinue = property.NextVisible(false);

				index++;
			}

			return doContinue;
		}

		bool DrawArrayHeader(bool isExpanded, SerializedProperty property)
		{
			EditorGUILayout.BeginHorizontal();
			isExpanded = EditorGUILayout.Foldout(isExpanded, property.displayName, Settings.Foldouts);

			if (property.arraySize > 0 && GUILayout.Button("-", GUILayout.Width(15), GUILayout.Height(12)))
			{
				property.DeleteArrayElementAtIndex(property.arraySize - 1);
				isExpanded = true;
			}

			if (GUILayout.Button("+", GUILayout.Width(15), GUILayout.Height(12)))
			{
				property.InsertArrayElementAtIndex(property.arraySize);
				var insertedElement = property.GetArrayElementAtIndex(property.arraySize - 1);
				if (insertedElement.propertyType == SerializedPropertyType.ObjectReference && insertedElement.objectReferenceValue != null)
					property.DeleteArrayElementAtIndex(property.arraySize - 1);

				isExpanded = true;
			}

			EditorGUILayout.EndHorizontal();

			currentPropertyHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			return isExpanded;
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
				var propertyType = NodeEditorUtilities.GetPropertyType(iterator);
				if (propertyType != null && propertyType.IsDefined(typeof(Input), true))
				{
					next = iterator.NextVisible(false);
					continue;
				}
				else if (iterator.hasVisibleChildren)
				{
					if (FindConnectionsRecursive(iterator))
						continue;
					else
						return false;
				}
				else if (iterator.propertyType == SerializedPropertyType.ObjectReference)
				{
					var fieldInfo = NodeEditorUtilities.GetPropertyFieldInfo(iterator);
					if (propertyType != null)
					{
						if (fieldInfo.GetCustomAttributes(typeof(EmbeddedAttribute), true).Any() && iterator.objectReferenceValue != null)
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