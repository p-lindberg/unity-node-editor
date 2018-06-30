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
}