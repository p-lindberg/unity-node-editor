/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace DataDesigner
{
	public class NodeConnector : IView
	{
		public event System.Action OnDeath;

		public NodeView NodeView { get; private set; }

		public GUIStyle LeftGUIStyle { get; private set; }

		public GUIStyle RightGUIStyle { get; private set; }

		public string PropertyPath { get; private set; }

		public bool Connecting { get; private set; }

		public bool Dead { get; private set; }

		Rect rect;

		public Rect Rect
		{
			get { return rect; }
			set
			{
				if (Event.current.type != EventType.Repaint)
					return;

				rect = value;
			}
		}

		SerializedObject serializedObject;
		bool visible;
		float height;
		Alignment alignment;

		public NodeConnector(NodeView nodeView, SerializedObject serializedObject, string propertyPath)
		{
			this.NodeView = nodeView;
			this.serializedObject = serializedObject;
			this.PropertyPath = propertyPath;
			LeftGUIStyle = new GUIStyle(NodeEditor.Settings.DefaultNodeViewSettings.LeftConnectorStyle);
			RightGUIStyle = new GUIStyle(NodeEditor.Settings.DefaultNodeViewSettings.RightConnectorStyle);
		}

		public void Initialize()
		{
			ResetDrawProperties();
		}

		public void SetDrawProperties(float height, bool visible, Alignment alignment = Alignment.Auto)
		{
			this.height = height;
			this.visible = visible;
			this.alignment = alignment;
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

		Rect UpdateRect(bool left)
		{
			Rect = new Rect(GetPosition(left), GetSize());
			return Rect;
		}

		public Rect GetWindowRect()
		{
			return Rect;
		}

		public void Draw(Vector2 origin)
		{
			rect = UpdateRect(left: (alignment == Alignment.Left));
			var guiStyle = alignment == Alignment.Left ? LeftGUIStyle : RightGUIStyle;
			bool active = false;

			var property = serializedObject.FindProperty(PropertyPath);
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
				IView targetView = (IView)NodeView.NodeEditor.GetNodeView(currentTarget) ??
								   NodeView.NodeEditor.GetEmbeddedObjectHandle(currentTarget);

				if (targetView != null)
				{
					if (alignment == Alignment.Auto)
						if (targetView.GetWindowRect().position.x < NodeView.GetWindowRect().position.x)
						{
							rect = UpdateRect(left: true);
							guiStyle = LeftGUIStyle;
						}

					if (!Connecting)
						Handles.DrawLine(rect.center, targetView.GetWindowRect().center);

					active = true;
				}
			}

			if (Connecting)
			{
				Handles.DrawLine(rect.center, Event.current.mousePosition);
				active = true;
			}

			if (active)
				SwitchActiveState(guiStyle);

			GUI.Box(rect, "", guiStyle);

			if (active)
				SwitchActiveState(guiStyle);

			HandleConnectorEvents(rect);
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
				IObjectView hit = (IObjectView)NodeView.NodeEditor.GetNodeViewAtPosition(Event.current.mousePosition) ??
													   NodeView.NodeEditor.GetEmbeddedObjectHandleAtPosition(Event.current.mousePosition);
				if (hit != null)
					ConnectTo(hit.ViewObject);
				else
				{
					if (!connectorRect.Contains(Event.current.mousePosition) || Event.current.alt)
						ConnectTo(null);
				}

				Connecting = false;

				Event.current.Use();
			}
		}

		void ConnectTo(UnityEngine.Object target)
		{
			var property = serializedObject.FindProperty(PropertyPath);
			property.objectReferenceValue = target;
			serializedObject.ApplyModifiedProperties();
		}
	}
}*/