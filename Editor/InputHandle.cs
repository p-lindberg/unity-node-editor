using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace DataDesigner
{
	public class InputHandle : Connector, IPropertyView
	{
		public override GUIStyle LeftGUIStyle { get { return guiStyle; } }

		public override GUIStyle RightGUIStyle { get { return guiStyle; } }

		public SerializedProperty ViewProperty { get; private set; }

		GUIStyle guiStyle;

		UnityEngine.Object targetPropertyOwner;
		string targetPropertyName;
		Type inputType;

		public InputHandle(NodeView nodeView, SerializedProperty serializedProperty, Type inputType, Color? color = null) : base(nodeView)
		{
			this.ViewProperty = serializedProperty;
			this.inputType = inputType;

			guiStyle = new GUIStyle();
			var background = new Texture2D(1, 1, TextureFormat.RGBA32, false);
			background.hideFlags = HideFlags.DontSave;

			if (color == null)
				color = nodeView.Settings.DefaultInputColor;

			background.SetPixel(0, 0, color.Value);
			background.Apply();
			guiStyle.normal.background = background;
			guiStyle.active.background = background;

			GetTarget();
		}

		public override bool IsDead()
		{
			return ViewProperty == null;
		}

		protected override void OnDraw(Vector2 origin)
		{
			if (targetPropertyOwner != null && !string.IsNullOrEmpty(targetPropertyName))
			{
				IView targetView = NodeView.NodeEditor.GetOutputHandle(targetPropertyOwner, targetPropertyName);

				if (targetView != null)
				{
					if (!Connecting)
						Handles.DrawLine(rect.center, targetView.GetWindowRect().center);

					active = true;
				}
			}
		}

		void GetTarget()
		{
			var iterator = ViewProperty.Copy();
			var rootDepth = iterator.depth;
			var doContinue = iterator.Next(true);
			while (doContinue && iterator.depth > rootDepth)
			{
				if (iterator.propertyType == SerializedPropertyType.ObjectReference)
					targetPropertyOwner = iterator.objectReferenceValue;
				else if (iterator.propertyType == SerializedPropertyType.String)
					targetPropertyName = iterator.stringValue;

				doContinue = iterator.Next(false);
			}

			/*if (targetPropertyName != null && targetPropertyOwner != null)
				if (NodeView.NodeEditor.GetOutputHandle(targetPropertyOwner, targetPropertyName) == null)
					Disconnect();*/
		}

		public void SetTarget(string propertyName, Type propertyType, UnityEngine.Object propertyOwner)
		{
			if (!this.inputType.IsAssignableFrom(propertyType))
				return;

			var iterator = ViewProperty.Copy();
			var rootDepth = iterator.depth;
			var doContinue = iterator.Next(true);

			SerializedProperty targetSerializedProperty = null;
			SerializedProperty propertyPathSerializedProperty = null;
			while (doContinue && iterator.depth > rootDepth)
			{
				if (iterator.propertyType == SerializedPropertyType.ObjectReference)
					targetSerializedProperty = iterator.Copy();
				else if (iterator.propertyType == SerializedPropertyType.String)
					propertyPathSerializedProperty = iterator.Copy();

				doContinue = iterator.Next(false);
			}

			if (targetSerializedProperty != null && propertyPathSerializedProperty != null)
			{
				targetSerializedProperty.objectReferenceValue = propertyOwner;
				propertyPathSerializedProperty.stringValue = propertyName;
				this.targetPropertyOwner = propertyOwner;
				this.targetPropertyName = propertyName;
				iterator.serializedObject.ApplyModifiedProperties();
			}
			else
				Debug.LogWarning("[DataDesigner] Could not connect to output property " + propertyName
								+ ". The input class needs to have both an object reference and a string as serialized fields.");

		}

		protected override void ConnectTo(IView hitView)
		{
			var propertyInfoView = hitView as IReflectedPropertyView;
			if (propertyInfoView != null)
				SetTarget(propertyInfoView.ReflectedPropertyName, propertyInfoView.ReflectedPropertyType, propertyInfoView.PropertyOwner);
		}

		protected override void Disconnect()
		{
			SetTarget(null, this.inputType, null);
		}
	}
}