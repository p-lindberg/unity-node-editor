using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Text.RegularExpressions;
using System.Linq;

public static class NodeEditorUtilities
{
	public static string GetPropertyTypeName(SerializedProperty property)
	{
		var type = property.type;
		var match = Regex.Match(type, @"PPtr<\$(.*?)>");
		if (match.Success)
			type = match.Groups[1].Value;
		return type;
	}

	public static Type GetPropertyType(SerializedProperty property)
	{
		var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
		var matchingTypes = loadedAssemblies.Select(x => x.GetType(GetPropertyTypeName(property))).Where(x => x != null);
		var matches = matchingTypes.Count();
		if (matches == 0)
		{
			Debug.LogError("Could not find type of property " + property.type);
			return null;
		}
		else if (matches == 1)
			return matchingTypes.First();
		else
		{
			Debug.LogError("GetPropertyType inconclusive: more than one type matches the property " + property.type);
			return null;
		}
	}

	public static List<Type> GetDerivedTypes<T>(bool includeBase, bool includeAbstract)
	{
		var baseType = typeof(T);
		var derivedTypes = new List<Type>();

		if (includeBase)
			derivedTypes.Add(baseType);

		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			foreach (var type in assembly.GetTypes())
			{
				if (type.IsAbstract)
				{
					if (includeAbstract && type.IsSubclassOf(baseType))
						derivedTypes.Add(type);
				}
				else if (type.IsSubclassOf(baseType))
					derivedTypes.Add(type);
			}

		return derivedTypes;
	}
}
