using System.Runtime.CompilerServices;
using UnityEngine;

public static class ComponentUtils
{
	private static readonly uint[] kHashBits = new uint[4];

	public static T EnsureComponent<T>(this Component ctx, ref T target) where T : Component
	{
		if (ctx.AsNull() == null)
		{
			return null;
		}
		if (target.AsNull() != null)
		{
			return target;
		}
		return target = ctx.GetComponent<T>();
	}

	public static bool TryEnsureComponent<T>(this Component ctx, ref T target) where T : Component
	{
		if (ctx.AsNull() == null)
		{
			return false;
		}
		if (target.AsNull() != null)
		{
			return true;
		}
		target = ctx.GetComponent<T>();
		return true;
	}

	public static T AddComponent<T>(this Component c) where T : Component
	{
		return c.gameObject.AddComponent<T>();
	}

	public static void GetOrAddComponent<T>(this Component c, out T result) where T : Component
	{
		if (!c.TryGetComponent<T>(out result))
		{
			result = c.gameObject.AddComponent<T>();
		}
	}

	public static bool GetComponentAndSetFieldIfNullElseLogAndDisable<T>(this Behaviour c, ref T fieldRef, string fieldName, string fieldTypeName, string msgSuffix = "Disabling.", [CallerMemberName] string caller = "__UNKNOWN_CALLER__") where T : Component
	{
		if (c.GetComponentAndSetFieldIfNullElseLog(ref fieldRef, fieldName, fieldTypeName, msgSuffix, caller))
		{
			return true;
		}
		c.enabled = false;
		return false;
	}

	public static bool GetComponentAndSetFieldIfNullElseLog<T>(this Behaviour c, ref T fieldRef, string fieldName, string fieldTypeName, string msgSuffix = "", [CallerMemberName] string caller = "__UNKNOWN_CALLER__") where T : Component
	{
		if (fieldRef != null)
		{
			return true;
		}
		fieldRef = c.GetComponent<T>();
		if (fieldRef != null)
		{
			return true;
		}
		Debug.LogError(caller + ": Could not find " + fieldTypeName + " \"" + fieldName + "\" on \"" + c.name + "\". " + msgSuffix, c);
		return false;
	}

	public static bool DisableIfNull<T>(this Behaviour c, T fieldRef, string fieldName, string fieldTypeName, [CallerMemberName] string caller = "__UNKNOWN_CALLER__") where T : Object
	{
		if (fieldRef != null)
		{
			return true;
		}
		c.enabled = false;
		return false;
	}

	public static Hash128 ComputeStaticHash128(Component c, string k)
	{
		return ComputeStaticHash128(c, StaticHash.Compute(k));
	}

	public static Hash128 ComputeStaticHash128(Component c, int k = 0)
	{
		if (c == null)
		{
			return default(Hash128);
		}
		Transform transform = c.transform;
		Component[] components = c.gameObject.GetComponents(typeof(Component));
		uint[] array = kHashBits;
		int siblingIndex = transform.GetSiblingIndex();
		int num = components.Length;
		int i;
		for (i = 0; i < num && (object)c != components[i]; i++)
		{
		}
		int num2 = StaticHash.Compute(k + 2, 1);
		int num3 = StaticHash.Compute(siblingIndex + 4, num2);
		int num4 = StaticHash.Compute(num + 8, num3);
		int num5 = StaticHash.Compute(i + 16, num4);
		array[0] = (uint)num2;
		array[1] = (uint)num3;
		array[2] = (uint)num4;
		array[3] = (uint)num5;
		new SRand(StaticHash.Compute(num2, num3, num4, num5)).Shuffle(array);
		Hash128 outHash = new Hash128(array[0], array[1], array[2], array[3]);
		Hash128 inHash = Hash128.Compute(c.GetType().FullName);
		Hash128 inHash2 = TransformUtils.ComputePathHash(transform);
		Hash128 inHash3 = transform.localToWorldMatrix.QuantizedHash128();
		HashUtilities.AppendHash(ref inHash, ref outHash);
		HashUtilities.AppendHash(ref inHash2, ref outHash);
		HashUtilities.AppendHash(ref inHash3, ref outHash);
		return outHash;
	}
}
