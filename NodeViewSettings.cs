using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Node Editor/Node View Settings")]
public class NodeViewSettings : ScriptableObject
{
	[Header("Layout Settings")]

	[SerializeField] Vector2 minimumSize = Vector2.zero;

	public Vector2 MinimumSize { get { return minimumSize; } }

	[SerializeField] Vector2 minimumSizeCollapsed = Vector2.zero;

	public Vector2 MinimumSizeCollapsed { get { return minimumSizeCollapsed; } }

	[SerializeField] float minimumLabelWidth = 0;

	public float MinimumLabelWidth { get { return minimumLabelWidth; } }

	[SerializeField] float minimumFieldWidth = 0;

	public float MinimumFieldWidth { get { return minimumFieldWidth; } }

	[SerializeField] bool indentNested = true;

	public bool IndentNested { get { return indentNested; } }

	[SerializeField] bool indentHeadersOnly = true;

	public bool IndentHeadersOnly { get { return indentHeadersOnly; } }

	[Header("GUI Styles")]

	[SerializeField] GUIStyle guiStyle = null;

	public GUIStyle GUIStyle { get { return guiStyle; } }

	[SerializeField] GUIStyle nodeHeaderStyle = null;

	public GUIStyle NodeHeaderStyle { get { return nodeHeaderStyle; } }

	[SerializeField] GUIStyle separatorStyle = null;

	public GUIStyle SeparatorStyle { get { return separatorStyle; } }

	[SerializeField] GUIStyle leftConnectorStyle = null;

	public GUIStyle LeftConnectorStyle { get { return leftConnectorStyle; } }

	[SerializeField] GUIStyle rightConnectorStyle = null;

	public GUIStyle RightConnectorStyle { get { return rightConnectorStyle; } }

	[SerializeField] GUIStyle tagStyle = null;

	public GUIStyle TagStyle { get { return tagStyle; } }

	[SerializeField] GUIStyle foldouts = null;

	public GUIStyle Foldouts { get { return foldouts; } set { foldouts = value; } }

	[SerializeField] GUIStyle embeddedObject = null;

	public GUIStyle EmbeddedObject { get { return embeddedObject; } }

	[SerializeField] GUIStyle embeddedObjectHandle = null;

	public GUIStyle EmbeddedObjectHandle { get { return embeddedObjectHandle; } }

	[SerializeField] GUIStyle embeddedObjectHandleLeft = null;

	public GUIStyle EmbeddedObjectHandleLeft { get { return embeddedObjectHandleLeft; } }
}