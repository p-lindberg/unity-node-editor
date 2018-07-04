using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;

public static class NodeEditorUtilities
{
	public const BindingFlags StandardBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

	public static Type GetPropertyType(SerializedProperty property)
	{
		var typeName = GetPropertyTypeName(property);
		var type = GetTypeByName(typeName);
		if (type == null)
			type = GetTypeByName("UnityEngine." + typeName);

		// SerializedProperty.type does not include the namespace, at least for Unity types.

		return type;
	}

	public static string GetPropertyTypeName(SerializedProperty property)
	{
		var type = property.type;
		var match = Regex.Match(type, @"PPtr<\$(.*?)>");
		if (match.Success)
			type = match.Groups[1].Value;
		return type;
	}

	public static Type GetTypeByName(string typeName)
	{
		var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
		HashSet<Type> matchingTypes = new HashSet<Type>(loadedAssemblies.Select(x => x.GetType(typeName)).Where(x => x != null));
		if (matchingTypes.Count == 1)
			return matchingTypes.First();
		else
			return null;
	}

	public static IEnumerable<Type> GetDerivedTypes(Type baseType, bool includeBase, bool includeAbstract)
	{
		if (includeBase)
			yield return baseType;

		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			foreach (var type in assembly.GetTypes())
			{
				if (type.IsAbstract)
				{
					if (includeAbstract && type.IsSubclassOf(baseType))
						yield return type;
				}
				else if (type.IsSubclassOf(baseType))
					yield return type;
			}
	}

	public static IEnumerable<Type> GetDerivedTypes<T>(bool includeBase, bool includeAbstract)
	{
		return GetDerivedTypes(typeof(T), includeBase, includeAbstract);
	}

	public static Vector2 RoundVectorToIntegerValues(Vector2 vector)
	{
		return new Vector2(Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y));
	}
}
