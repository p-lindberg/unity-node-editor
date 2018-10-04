using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace DataDesigner
{
	public class OutputHandle : Connector, IReflectedPropertyView
	{
		public override GUIStyle LeftGUIStyle { get { return leftGuiStyle; } }

		public override GUIStyle RightGUIStyle { get { return rightGuiStyle; } }

		public UnityEngine.Object PropertyOwner { get; private set; }

		public string ReflectedPropertyName { get; private set; }
		GUIStyle leftGuiStyle;
		GUIStyle rightGuiStyle;

		public OutputHandle(NodeView nodeView, UnityEngine.Object propertyOwner, string propertyName) : base(nodeView)
		{
			this.PropertyOwner = propertyOwner;
			this.ReflectedPropertyName = propertyName;
			leftGuiStyle = new GUIStyle(NodeEditor.Settings.DefaultNodeViewSettings.EmbeddedObjectHandleLeft);
			rightGuiStyle = new GUIStyle(NodeEditor.Settings.DefaultNodeViewSettings.EmbeddedObjectHandle);
		}

		public override bool IsDead()
		{
			return PropertyOwner == null || string.IsNullOrEmpty(ReflectedPropertyName);
		}

		protected override void OnDraw(Vector2 origin)
		{

		}

		protected override void Disconnect()
		{

		}

		protected override void ConnectTo(IView hitView)
		{
			var inputHandle = hitView as InputHandle;
			if (inputHandle != null)
				inputHandle.SetTarget(ReflectedPropertyName, PropertyOwner);
		}
	}
}