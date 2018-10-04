using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace DataDesigner
{
	public abstract class Connector : IView
	{
		public event System.Action OnDeath;

		public NodeView NodeView { get; private set; }

		public abstract GUIStyle LeftGUIStyle { get; }

		public abstract GUIStyle RightGUIStyle { get; }

		public bool Connecting { get; private set; }

		public bool Dead { get; private set; }

		public virtual IEnumerable<IView> SubViews { get { yield break; } }

		protected Rect rect;

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

		bool visible;
		float height;
		protected bool active;
		protected Alignment alignment;
		protected GUIStyle currentGuiStyle;

		public Connector(NodeView nodeView)
		{
			this.NodeView = nodeView;
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

		protected Rect UpdateRect(bool left)
		{
			Rect = new Rect(GetPosition(left), GetSize());
			return Rect;
		}

		public Rect GetWindowRect()
		{
			return Rect;
		}

		public abstract bool IsDead();

		public void Draw(Vector2 origin)
		{
			rect = UpdateRect(left: (alignment == Alignment.Left));
			currentGuiStyle = alignment == Alignment.Left ? LeftGUIStyle : RightGUIStyle;
			active = false;

			if (IsDead())
			{
				Dead = true;
				if (OnDeath != null)
					OnDeath.Invoke();

				return;
			}

			OnDraw(origin);

			if (Connecting)
			{
				Handles.DrawLine(rect.center, Event.current.mousePosition);
				active = true;
			}

			if (active)
				SwitchActiveState(currentGuiStyle);

			GUI.Box(rect, "", currentGuiStyle);

			if (active)
				SwitchActiveState(currentGuiStyle);

			HandleConnectorEvents(rect);
			ResetDrawProperties();
		}

		protected abstract void OnDraw(Vector2 origin);

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
				var viewHit = NodeView.NodeEditor.GetViewAtPosition(Event.current.mousePosition);

				if (viewHit != null)
					ConnectTo(viewHit);
				else if (!connectorRect.Contains(Event.current.mousePosition) || Event.current.alt)
					Disconnect();

				Connecting = false;

				Event.current.Use();
			}
		}

		protected abstract void ConnectTo(IView hitView);

		protected abstract void Disconnect();
	}
}