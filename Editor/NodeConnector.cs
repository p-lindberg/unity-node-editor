using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class NodeConnector
{
	public event System.Action OnDeath;

	public NodeView NodeView { get; private set; }
	public GUIStyle LeftGUIStyle { get; private set; }
	public GUIStyle RightGUIStyle { get; private set; }
	public string PropertyPath { get; private set; }
	public bool Connecting { get; private set; }
	public bool Dead { get; private set; }

	SerializedObject serializedObject;
	bool visible;
	float height;

	public NodeConnector(NodeView nodeView, SerializedObject serializedObject, string propertyPath)
	{
		this.NodeView = nodeView;
		this.serializedObject = serializedObject;
		this.PropertyPath = propertyPath;
		LeftGUIStyle = new GUIStyle(NodeEditor.Settings.DefaultNodeViewSettings.LeftConnectorStyle);
		RightGUIStyle = new GUIStyle(NodeEditor.Settings.DefaultNodeViewSettings.RightConnectorStyle);
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
		var rect = GetRect(left: false);
		var guiStyle = RightGUIStyle;
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
			var targetNodeView = NodeView.NodeEditor.GetNodeView(currentTarget);
			if (targetNodeView != null)
			{
				if (targetNodeView.GetWindowRect().position.x < NodeView.GetWindowRect().position.x)
				{
					rect = GetRect(left: true);
					guiStyle = LeftGUIStyle;
				}

				if (!Connecting)
					Handles.DrawLine(rect.center, targetNodeView.GetWindowRect().center);

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
			var hit = NodeView.NodeEditor.GetNodeViewAtMousePosition(Event.current.mousePosition);
			if (hit != null)
				ConnectTo(hit.nodeObject);
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
