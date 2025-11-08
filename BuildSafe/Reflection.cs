using System;
using System.Linq;
using System.Reflection;

namespace BuildSafe;

public static class Reflection<T>
{
	private static Type gCachedType;

	private static MethodInfo[] gMethodsCache;

	private static FieldInfo[] gFieldsCache;

	private static PropertyInfo[] gPropertiesCache;

	private static EventInfo[] gEventsCache;

	public static Type Type { get; } = typeof(T);

	public static EventInfo[] Events => PreFetchEvents();

	public static MethodInfo[] Methods => PreFetchMethods();

	public static FieldInfo[] Fields => PreFetchFields();

	public static PropertyInfo[] Properties => PreFetchProperties();

	private static EventInfo[] PreFetchEvents()
	{
		if (gEventsCache != null)
		{
			return gEventsCache;
		}
		return gEventsCache = Type.GetRuntimeEvents().ToArray();
	}

	private static PropertyInfo[] PreFetchProperties()
	{
		if (gPropertiesCache != null)
		{
			return gPropertiesCache;
		}
		return gPropertiesCache = Type.GetRuntimeProperties().ToArray();
	}

	private static MethodInfo[] PreFetchMethods()
	{
		if (gMethodsCache != null)
		{
			return gMethodsCache;
		}
		return gMethodsCache = Type.GetRuntimeMethods().ToArray();
	}

	private static FieldInfo[] PreFetchFields()
	{
		if (gFieldsCache != null)
		{
			return gFieldsCache;
		}
		return gFieldsCache = Type.GetRuntimeFields().ToArray();
	}
}
public static class Reflection
{
	private static Assembly[] gAssemblyCache;

	private static Type[] gTypeCache;

	public static Assembly[] AllAssemblies => PreFetchAllAssemblies();

	public static Type[] AllTypes => PreFetchAllTypes();

	static Reflection()
	{
		PreFetchAllAssemblies();
		PreFetchAllTypes();
	}

	private static Assembly[] PreFetchAllAssemblies()
	{
		if (gAssemblyCache != null)
		{
			return gAssemblyCache;
		}
		return gAssemblyCache = (from a in AppDomain.CurrentDomain.GetAssemblies()
			where a != null
			select a).ToArray();
	}

	private static Type[] PreFetchAllTypes()
	{
		if (gTypeCache != null)
		{
			return gTypeCache;
		}
		return gTypeCache = (from t in PreFetchAllAssemblies().SelectMany((Assembly a) => a.GetTypes())
			where t != null
			select t).ToArray();
	}

	public static MethodInfo[] GetMethodsWithAttribute<T>() where T : Attribute
	{
		return (from m in AllTypes.SelectMany((Type t) => t.GetRuntimeMethods())
			where m.GetCustomAttributes(typeof(T), inherit: false).Length != 0
			select m).ToArray();
	}
}
