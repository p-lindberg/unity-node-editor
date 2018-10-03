using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace DataDesigner
{
	public class NodeConnector
	{
		public event System.Action OnDeath;

		public NodeView NodeView { get; private set; }

		public GUIStyle LeftGUIStyle { get; private set; }

		public GUIStyle RightGUIStyle { get; private set; }

		public string PropertyPath { get; private set; }

		public bool Connecting { get; private set; }

		public bool Dead { get; private set; }

		public Rect Rect { get; private set; }

		public SerializedObject SerializedObject { get; private set; }
		bool visible;
		float height;

		public NodeConnector(NodeView nodeView, SerializedObject serializedObject, string propertyPath)
		{
			this.NodeView = nodeView;
			this.SerializedObject = serializedObject;
			this.PropertyPath = propertyPath;
			LeftGUIStyle = new GUIStyle(NodeEditor.Settings.DefaultNodeViewSettings.LeftConnectorStyle);
			RightGUIStyle = new GUIStyle(NodeEditor.Settings.DefaultNodeViewSettings.RightConnectorStyle);
		}

		public void Initialize()
		{
			ResetDrawProperties();
		}

		public void SetDrawProperties(float height, bool visible)
		{
			this.height = height;
			this.visible = visible;
		}

		void ResetDrawProperties()
		{
			visible = false;
			height = 0f;
		}

		Vector2 GetPosition(bool left)
		{
			if (visible)
			{
				var nodePosition = NodeView.GetWindowRect().position;
				var nodeSize = NodeView.GetWindowRect().size;
				if (left)
					return nodePosition + new Vector2(-GetSize().x, height);
				else
					return nodePosition + new Vector2(nodeSize.x, height);
			}
			else
				return NodeView.GetWindowRect().center;
		}

		Vector2 GetSize()
		{
			if (visible)
				return Vector2.one * EditorGUIUtility.singleLineHeight;
			else
				return Vector2.zero;
		}

		Rect GetRect(bool left)
		{
			return new Rect(GetPosition(left), GetSize());
		}

		public void Draw()
		{
			Rect = GetRect(left: false);
			var guiStyle = RightGUIStyle;
			bool active = false;

			var property = SerializedObject.FindProperty(PropertyPath);
			if (property == null)
			{
				Dead = true;
				if (OnDeath != null)
					OnDeath.Invoke();

				return;
			}

			var currentTarget = property.objectReferenceValue;
			if (currentTarget != null)
			{
				Rect targetRect = default(Rect);

				var targetType = NodeEditorUtilities.GetPropertyType(property);
				if (targetType.IsDefined(typeof(SocketAttribute), true))
				{
					var targetConnector = NodeView.NodeEditor.GetSocketConnector(currentTarget);
					if (targetConnector != null)
						targetRect = targetConnector.Rect;
				}
				else
				{
					var targetNodeView = NodeView.NodeEditor.GetNodeView(currentTarget);
					if (targetNodeView != null)
						targetRect = targetNodeView.GetWindowRect();
				}


				if (targetRect.position.x < NodeView.GetWindowRect().position.x)
				{
					Rect = GetRect(left: true);
					guiStyle = LeftGUIStyle;
				}

				if (!Connecting)
					Handles.DrawLine(Rect.center, targetRect.center);

				active = true;
			}

			if (Connecting)
			{
				Handles.DrawLine(Rect.center, Event.current.mousePosition);
				active = true;
			}

			if (active)
				SwitchActiveState(guiStyle);

			GUI.Box(Rect, "", guiStyle);

			if (active)
				SwitchActiveState(guiStyle);

			HandleConnectorEvents(Rect);
			ResetDrawProperties();
		}

		void SwitchActiveState(GUIStyle guiStyle)
		{
			var temp = guiStyle.normal.background;
			guiStyle.normal.background = guiStyle.active.background;
			guiStyle.active.background = temp;
		}

		void HandleConnectorEvents(Rect connectorRect)
		{
			if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && connectorRect.Contains(Event.current.mousePosition))
			{
				Connecting = true;
				Event.current.Use();
			}
			else if (Connecting && ((Event.current.type == EventType.MouseUp && Event.current.button == 0)
					 || (Event.current.type == EventType.MouseLeaveWindow)))
			{
				var hit = NodeView.NodeEditor.GetNodeViewAtMousePosition(Event.current.mousePosition);
				if (hit != null)
					ConnectTo(hit.nodeObject);
				else
				{
					var connectorHit = NodeView.NodeEditor.GetNodeConnectorAtPosition(Event.current.mousePosition);
					if (connectorHit != null)
						ConnectTo(connectorHit.SerializedObject.targetObject);
					else if (!connectorRect.Contains(Event.current.mousePosition) || Event.current.alt)
						ConnectTo(null);
				}

				Connecting = false;

				Event.current.Use();
			}
		}

		void ConnectTo(UnityEngine.Object target)
		{
			var property = SerializedObject.FindProperty(PropertyPath);
			property.objectReferenceValue = target;
			SerializedObject.ApplyModifiedProperties();
		}
	}
}