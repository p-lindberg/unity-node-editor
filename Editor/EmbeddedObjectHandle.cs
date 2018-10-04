using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace DataDesigner
{
	public class EmbeddedObjectHandle : Connector, IObjectView
	{
		public override GUIStyle LeftGUIStyle { get { return leftGuiStyle; } }

		public override GUIStyle RightGUIStyle { get { return rightGuiStyle; } }

		public UnityEngine.Object ViewObject { get; private set; }

		GUIStyle leftGuiStyle;
		GUIStyle rightGuiStyle;

		public EmbeddedObjectHandle(NodeView nodeView, UnityEngine.Object embeddedObject) : base(nodeView)
		{
			this.ViewObject = embeddedObject;
			leftGuiStyle = new GUIStyle(NodeEditor.Settings.DefaultNodeViewSettings.EmbeddedObjectHandleLeft);
			rightGuiStyle = new GUIStyle(NodeEditor.Settings.DefaultNodeViewSettings.EmbeddedObjectHandle);
		}

		public override bool IsDead()
		{
			return ViewObject == null;
		}

		protected override void OnDraw(Vector2 origin)
		{

		}

		protected override void ConnectToObject(UnityEngine.Object target)
		{

		}

		protected override void ConnectWithProperty(SerializedProperty property)
		{
			var propertyType = NodeEditorUtilities.GetPropertyType(property);
			if (propertyType.IsAssignableFrom(ViewObject.GetType()))
			{
				property.objectReferenceValue = ViewObject;
				property.serializedObject.ApplyModifiedProperties();
			}
		}

		protected override void Disconnect()
		{

		}
	}
}