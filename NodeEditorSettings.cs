using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class NodeEditorSettings : ScriptableObject
{
	[SerializeField] ScriptableObject currentTarget;

	public ScriptableObject CurrentTarget { get { return currentTarget; } set { currentTarget = value; EditorUtility.SetDirty(this); } }

	[SerializeField] GUIStyle nodeGUIStyle;

	public GUIStyle NodeGUIStyle { get { return nodeGUIStyle; } }

	[SerializeField] Texture2D windowBackground;

	public Texture2D WindowBackground { get { return windowBackground; } }

	[SerializeField] float defaultLabelWidth;

	public float DefaultLabelWidth { get { return defaultLabelWidth; } }

	[SerializeField] GUIStyle nodeHeaderStyle;

	public GUIStyle NodeHeaderStyle { get { return nodeHeaderStyle; } }

	[SerializeField] bool indentNested = true;

	public bool IndentNested { get { return indentNested; } }

	[SerializeField] bool indentHeadersOnly = true;

	public bool IndentHeadersOnly { get { return indentHeadersOnly; } }

	[SerializeField] GUIStyle separatorStyle;

	public GUIStyle SeparatorStyle { get { return separatorStyle; } }

	[SerializeField] GUIStyle nodeContentToggleStyle;

	public GUIStyle NodeContentToggleStyle { get { return nodeContentToggleStyle; } }
}