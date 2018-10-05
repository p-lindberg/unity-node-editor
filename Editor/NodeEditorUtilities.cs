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
		static BindingFlags BindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

		public const BindingFlags StandardBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

		static Dictionary<KeyPair<Type, string>, Type> propertyTypes = new Dictionary<KeyPair<Type, string>, Type>();
		static Dictionary<KeyPair<Type, string>, FieldInfo> fieldInfos = new Dictionary<KeyPair<Type, string>, FieldInfo>();

		/// <summary>
		/// Gets the property field info. For array items, will return field info of the array itself.
		/// </summary>
		public static FieldInfo GetPropertyFieldInfo(SerializedProperty property)
		{
			var propertyPath = property.propertyPath;
			var targetObjectType = property.serializedObject.targetObject.GetType();
			var keyPair = KeyPair.From(targetObjectType, propertyPath);

			FieldInfo fieldInfo;
			if (!fieldInfos.TryGetValue(keyPair, out fieldInfo))
			{
				fieldInfo = GetFieldInfoViaPath(targetObjectType, propertyPath);
				fieldInfos[keyPair] = fieldInfo;
			}

			return fieldInfo;
		}

		public static FieldInfo GetFieldInfoViaPath(this Type rootType, string path)
		{
			var pathWithoutArrayIndices = Regex.Replace(path, @"Array.data\[(.*)\]", "");
			string[] fieldNames = pathWithoutArrayIndices.Split('.');
			var type = rootType;
			FieldInfo fieldInfo = null;
			foreach (string fieldName in fieldNames)
			{
				if (type.IsArray)
					type = type.GetElementType();
				else
				{
					fieldInfo = GetFieldIncludingPrivateBaseFields(type, fieldName);
					type = fieldInfo != null ? fieldInfo.FieldType : null;
				}

				if (type == null)
					break;
			}

			return fieldInfo;
		}

		public static Type GetPropertyType(SerializedProperty property)
		{
			if (property.serializedObject.targetObject == null)
				return null;

			var propertyPath = property.propertyPath;
			var targetObjectType = property.serializedObject.targetObject.GetType();
			var keyPair = KeyPair.From(targetObjectType, propertyPath);
			Type propertyType;
			if (!propertyTypes.TryGetValue(keyPair, out propertyType))
			{
				propertyType = GetTypeViaPath(targetObjectType, propertyPath);
				propertyTypes[keyPair] = propertyType;
			}

			return propertyType;
		}

		public static Type GetTypeViaPath(this Type rootType, string path)
		{
			var pathWithoutArrayIndices = Regex.Replace(path, @"Array.data\[(.*)\]", "");
			string[] fieldNames = pathWithoutArrayIndices.Split('.');
			var type = rootType;
			foreach (string fieldName in fieldNames)
			{
				if (type.IsArray)
					type = type.GetElementType();
				else
				{
					var field = GetFieldIncludingPrivateBaseFields(type, fieldName);
					type = field != null ? field.FieldType : null;
				}

				if (type == null)
					break;
			}

			return type;
		}

		static FieldInfo GetFieldIncludingPrivateBaseFields(Type type, string fieldName)
		{
			FieldInfo fieldInfo = null;
			while (fieldInfo == null)
			{
				fieldInfo = type.GetField(fieldName, BindingFlags);
				if (fieldInfo == null)
				{
					type = type.BaseType;
					if (type == typeof(object))
						break;
				}
			}
			return fieldInfo;
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