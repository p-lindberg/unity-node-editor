using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;

namespace DataDesigner
{
	public static class NodeEditorUtilities
	{
		public const BindingFlags StandardBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

		public static Type GetPropertyType(SerializedProperty property)
		{
			var typeName = GetPropertyTypeName(property.type);
			return GetTypeByName(typeName);
		}

		public static Type GetPropertyElementType(SerializedProperty property)
		{
			var typeName = GetPropertyTypeName(property.arrayElementType);
			return GetTypeByName(typeName);
		}

		static string GetPropertyTypeName(string propertyType)
		{
			var match = Regex.Match(propertyType, @"PPtr<\$(.*?)>");
			if (match.Success)
				propertyType = match.Groups[1].Value;
			return propertyType;
		}

		static Dictionary<string, Type> typeCache = new Dictionary<string, Type>();

		static Type GetTypeByName(string typeName)
		{
			Type cachedType;
			if (typeCache.TryGetValue(typeName, out cachedType))
				return cachedType;

			var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			HashSet<Type> matchingTypes = new HashSet<Type>(loadedAssemblies.SelectMany(x => x.GetTypes().Where(y => y.Name == typeName)));
			if (matchingTypes.Count == 1)
			{
				var foundType = matchingTypes.First();
				typeCache[typeName] = foundType;
				return foundType;
			}
			else
			{
				typeCache[typeName] = null;
				return null;
			}
		}

		public static IEnumerable<Type> GetDerivedTypes(Type baseType, bool includeBase, bool includeAbstract)
		{
			if (includeBase)
			{
				if (!baseType.IsAbstract || (baseType.IsAbstract && includeAbstract))
					yield return baseType;
			}

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

		public static IEnumerable<SerializedProperty> GetExposedObjectFields(SerializedObject serializedObject, bool enterChildren = false, Type type = null, bool assignableFrom = false)
		{
			var iterator = serializedObject.GetIterator();
			var doContinue = iterator.NextVisible(true);
			while (doContinue)
			{
				if (iterator.propertyType == SerializedPropertyType.ObjectReference)
				{
					if (type != null)
					{
						var propertyType = NodeEditorUtilities.GetPropertyType(iterator);
						if (propertyType != null)
						{
							if (assignableFrom)
							{
								if (propertyType.IsAssignableFrom(type))
									yield return iterator;
							}
							else if (propertyType == type)
								yield return iterator;
						}
					}
					else
						yield return iterator;
				}

				doContinue = iterator.NextVisible(enterChildren);
			}
		}

		public static IEnumerable<SerializedProperty> GetExposedObjectArrays(SerializedObject serializedObject, bool enterChildren = false, Type type = null, bool assignableFrom = false)
		{
			var iterator = serializedObject.GetIterator();
			var doContinue = iterator.NextVisible(true);
			while (doContinue)
			{
				if (iterator.isArray)
				{
					if (type != null)
					{
						var propertyType = NodeEditorUtilities.GetPropertyElementType(iterator);
						if (propertyType != null)
						{
							if (assignableFrom)
							{
								if (propertyType.IsAssignableFrom(type))
									yield return iterator;
							}
							else if (propertyType == type)
								yield return iterator;
						}
					}
					else
						yield return iterator;
				}

				doContinue = iterator.NextVisible(enterChildren);
			}
		}
	}
}