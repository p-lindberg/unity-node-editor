using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Node Editor/Node View Settings")]
public class NodeViewSettings : ScriptableObject
{
	[Header("Layout Settings")]

	[SerializeField] Vector2 minimumSize;

	public Vector2 MinimumSize { get { return minimumSize; } }

	[SerializeField] Vector2 minimumSizeCollapsed;

	public Vector2 MinimumSizeCollapsed { get { return minimumSizeCollapsed; } }

	[SerializeField] float minimumLabelWidth;

	public float MinimumLabelWidth { get { return minimumLabelWidth; } }

	[SerializeField] float minimumFieldWidth;

	public float MinimumFieldWidth { get { return minimumFieldWidth; } }

	[SerializeField] bool indentNested = true;

	public bool IndentNested { get { return indentNested; } }

	[SerializeField] bool indentHeadersOnly = true;

	public bool IndentHeadersOnly { get { return indentHeadersOnly; } }

	[Header("GUI Styles")]

	[SerializeField] GUIStyle guiStyle;

	public GUIStyle GUIStyle { get { return guiStyle; } }

	[SerializeField] GUIStyle nodeHeaderStyle;

	public GUIStyle NodeHeaderStyle { get { return nodeHeaderStyle; } }

	[SerializeField] GUIStyle separatorStyle;

	public GUIStyle SeparatorStyle { get { return separatorStyle; } }

	[SerializeField] GUIStyle leftConnectorStyle;

	public GUIStyle LeftConnectorStyle { get { return leftConnectorStyle; } }

	[SerializeField] GUIStyle rightConnectorStyle;

	public GUIStyle RightConnectorStyle { get { return rightConnectorStyle; } }

	[SerializeField] GUIStyle tagStyle;

	public GUIStyle TagStyle { get { return tagStyle; } }

	[SerializeField] GUIStyle foldouts;

	public GUIStyle Foldouts { get { return foldouts; } set { foldouts = value; } }

	[SerializeField] GUIStyle embeddedObject;

	public GUIStyle EmbeddedObject { get { return embeddedObject; } }

	[SerializeField] GUIStyle embeddedObjectHandle;

	public GUIStyle EmbeddedObjectHandle { get { return embeddedObjectHandle; } }

	[SerializeField] GUIStyle embeddedObjectHandleLeft;

	public GUIStyle EmbeddedObjectHandleLeft { get { return embeddedObjectHandleLeft; } }

	[SerializeField] GUIStyle outputStyle;

	public GUIStyle OutputStyle { get { return outputStyle; } }

	[SerializeField] GUIStyle inputStyle;

	public GUIStyle InputStyle { get { return inputStyle; } }

	[SerializeField] Color defaultInputColor;

	public Color DefaultInputColor { get { return defaultInputColor; } }

	[SerializeField] Color defaultOutputColor;

	public Color DefaultOutputColor { get { return defaultOutputColor; } }
}