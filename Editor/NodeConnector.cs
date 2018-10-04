using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace DataDesigner
{
	public class NodeConnector : Connector, IPropertyView
	{
		GUIStyle leftGuiStyle;

		GUIStyle rightGuiStyle;

		public override GUIStyle LeftGUIStyle { get { return leftGuiStyle; } }

		public override GUIStyle RightGUIStyle { get { return rightGuiStyle; } }

		public string PropertyPath { get; private set; }

		public SerializedProperty ViewProperty
		{
			get
			{
				return serializedObject.FindProperty(PropertyPath);
			}
		}

		SerializedObject serializedObject;

		public NodeConnector(NodeView nodeView, SerializedObject serializedObject, string propertyPath) : base(nodeView)
		{
			this.serializedObject = serializedObject;
			this.PropertyPath = propertyPath;
			leftGuiStyle = new GUIStyle(NodeEditor.Settings.DefaultNodeViewSettings.LeftConnectorStyle);
			rightGuiStyle = new GUIStyle(NodeEditor.Settings.DefaultNodeViewSettings.RightConnectorStyle);
		}

		public override bool IsDead()
		{
			return serializedObject.FindProperty(PropertyPath) == null;
		}

		protected override void OnDraw(Vector2 origin)
		{
			var property = serializedObject.FindProperty(PropertyPath);
			if (property == null)
				return;

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
							currentGuiStyle = LeftGUIStyle;
						}

					if (!Connecting)
						Handles.DrawLine(rect.center, targetView.GetWindowRect().center);

					active = true;
				}
			}
		}

		void ConnectToObject(UnityEngine.Object target)
		{
			var property = serializedObject.FindProperty(PropertyPath);
			property.objectReferenceValue = target;
			serializedObject.ApplyModifiedProperties();
		}

		protected override void Disconnect()
		{
			ConnectToObject(null);
		}

		protected override void ConnectTo(IView hitView)
		{
			var objectView = hitView as IObjectView;
			if (objectView != null)
				ConnectToObject(objectView.ViewObject);
		}
	}
}