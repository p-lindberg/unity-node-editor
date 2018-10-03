/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace DataDesigner
{
	public class EmbeddedObjectHandle : IView, IObjectView
	{
		public event System.Action OnDeath;

		public NodeView NodeView { get; private set; }

		public GUIStyle LeftGUIStyle { get; private set; }

		public GUIStyle RightGUIStyle { get; private set; }

		public bool Connecting { get; private set; }

		public bool Dead { get; private set; }

		public UnityEngine.Object ViewObject { get; private set; }

		public IEnumerable<IView> SubViews { get { yield break; } }

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

		bool visible;
		float height;
		Alignment alignment;

		public EmbeddedObjectHandle(NodeView nodeView, UnityEngine.Object embeddedObject)
		{
			this.NodeView = nodeView;
			this.ViewObject = embeddedObject;
			LeftGUIStyle = NodeEditor.Settings.DefaultNodeViewSettings.EmbeddedObjectHandleLeft;
			RightGUIStyle = NodeEditor.Settings.DefaultNodeViewSettings.EmbeddedObjectHandle;
		}

		public void Initialize()
		{
			ResetDrawProperties();
		}

		public void SetDrawProperties(float height, bool visible, Alignment alignment = Alignment.Auto)
		{
			this.visible = visible;
			this.height = height;
			this.alignment = alignment;
		}

		void ResetDrawProperties()
		{
			visible = false;
			height = 0f;
		}

		Vector2 GetPosition()
		{
			if (visible)
			{
				var nodePosition = NodeView.GetWindowRect().position;
				var nodeSize = NodeView.GetWindowRect().size;
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
			Rect = new Rect(GetPosition(), GetSize());
			return Rect;
		}

		public Rect GetWindowRect()
		{
			return Rect;
		}

		public void Draw(Vector2 origin)
		{
			rect = UpdateRect(left: alignment == Alignment.Left);

			if (ViewObject == null)
			{
				Dead = true;
				if (OnDeath != null)
					OnDeath.Invoke();

				return;
			}

			GUI.Box(rect, "", alignment == Alignment.Left ? LeftGUIStyle : RightGUIStyle);
			ResetDrawProperties();
		}
	}
}*/