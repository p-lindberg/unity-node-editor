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
		public override GUIStyle LeftGUIStyle { get { return guiStyle; } }

		public override GUIStyle RightGUIStyle { get { return guiStyle; } }

		public UnityEngine.Object PropertyOwner { get; private set; }

		public string ReflectedPropertyName { get; private set; }

		public Type ReflectedPropertyType { get; private set; }

		GUIStyle guiStyle;

		public OutputHandle(NodeView nodeView, UnityEngine.Object propertyOwner, string propertyName, Type reflectedPropertyType, Color? color = null) : base(nodeView)
		{
			this.PropertyOwner = propertyOwner;
			this.ReflectedPropertyName = propertyName;
			this.ReflectedPropertyType = reflectedPropertyType;

			guiStyle = new GUIStyle();
			var background = new Texture2D(1, 1, TextureFormat.RGBA32, false);

			if (color == null)
				color = nodeView.Settings.DefaultOutputColor;

			background.SetPixel(0, 0, color.Value);
			background.Apply();
			guiStyle.normal.background = background;
			guiStyle.active.background = background;
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
				inputHandle.SetTarget(ReflectedPropertyName, ReflectedPropertyType, PropertyOwner);
		}
	}
}