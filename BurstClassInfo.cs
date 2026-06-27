using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AOT;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

[BurstCompile]
public static class BurstClassInfo
{
	public enum EFieldTypes
	{
		Float,
		Int,
		Double,
		Bool,
		String,
		LightUserData
	}

	[BurstCompile]
	public struct BurstFieldInfo
	{
		public int NameHash;

		public FixedString32Bytes Name;

		public FixedString32Bytes MetatableName;

		public int Offset;

		public EFieldTypes FieldType;

		public int Size;
	}

	[BurstCompile]
	public struct ClassInfo
	{
		public int NameHash;

		public int Size;

		public FixedString32Bytes Name;

		public NativeHashMap<int, BurstFieldInfo> FieldList;

		public NativeHashMap<int, IntPtr> FunctionList;
	}

	public abstract class ClassList
	{
		private class FieldKey
		{
		}

		public static class MetatableNames<T>
		{
			public static FixedString32Bytes Name;
		}

		public static readonly SharedStatic<NativeHashMap<int, ClassInfo>> InfoFields = SharedStatic<NativeHashMap<int, ClassInfo>>.GetOrCreateUnsafe(0u, -7258312696341931442L, -7445903157129162016L);
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal unsafe delegate int Index_00004CF0_0024PostfixBurstDelegate(lua_State* L);

	internal static class Index_00004CF0_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = BurstCompiler.CompileFunctionPointer<Index_00004CF0_0024PostfixBurstDelegate>(Index).Value;
			}
			P_0 = Pointer;
		}

		private static IntPtr GetFunctionPointer()
		{
			nint result = 0;
			GetFunctionPointerDiscard(ref result);
			return result;
		}

		public unsafe static int Invoke(lua_State* L)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					return ((delegate* unmanaged[Cdecl]<lua_State*, int>)functionPointer)(L);
				}
			}
			return Index_0024BurstManaged(L);
		}
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal unsafe delegate int NewIndex_00004CF1_0024PostfixBurstDelegate(lua_State* L);

	internal static class NewIndex_00004CF1_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = BurstCompiler.CompileFunctionPointer<NewIndex_00004CF1_0024PostfixBurstDelegate>(NewIndex).Value;
			}
			P_0 = Pointer;
		}

		private static IntPtr GetFunctionPointer()
		{
			nint result = 0;
			GetFunctionPointerDiscard(ref result);
			return result;
		}

		public unsafe static int Invoke(lua_State* L)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					return ((delegate* unmanaged[Cdecl]<lua_State*, int>)functionPointer)(L);
				}
			}
			return NewIndex_0024BurstManaged(L);
		}
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal unsafe delegate int NameCall_00004CF2_0024PostfixBurstDelegate(lua_State* L);

	internal static class NameCall_00004CF2_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = BurstCompiler.CompileFunctionPointer<NameCall_00004CF2_0024PostfixBurstDelegate>(NameCall).Value;
			}
			P_0 = Pointer;
		}

		private static IntPtr GetFunctionPointer()
		{
			nint result = 0;
			GetFunctionPointerDiscard(ref result);
			return result;
		}

		public unsafe static int Invoke(lua_State* L)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					return ((delegate* unmanaged[Cdecl]<lua_State*, int>)functionPointer)(L);
				}
			}
			return NameCall_0024BurstManaged(L);
		}
	}

	private static readonly FixedString32Bytes _k_metatableLookup = "metahash";

	public unsafe static void NewClass<T>(string className, Dictionary<int, FieldInfo> fieldList, Dictionary<int, lua_CFunction> functionList, Dictionary<int, FunctionPointer<lua_CFunction>> functionPtrList) where T : unmanaged
	{
		if (!ClassList.InfoFields.Data.IsCreated)
		{
			ClassList.InfoFields.Data = new NativeHashMap<int, ClassInfo>(20, Allocator.Persistent);
		}
		ClassList.MetatableNames<T>.Name = className;
		ReflectionMetaNames.ReflectedNames.TryAdd(typeof(T), className);
		ClassInfo item = new ClassInfo
		{
			NameHash = LuaHashing.ByteHash(className)
		};
		if (className.Length > 30)
		{
			throw new Exception("Name to long");
		}
		item.Name = className;
		item.Size = sizeof(T);
		item.FieldList = new NativeHashMap<int, BurstFieldInfo>(fieldList.Count, Allocator.Persistent);
		foreach (KeyValuePair<int, FieldInfo> field in fieldList)
		{
			BurstFieldInfo item2 = new BurstFieldInfo
			{
				NameHash = field.Key,
				Name = field.Value.Name,
				Offset = (int)Marshal.OffsetOf<T>(field.Value.Name)
			};
			Type fieldType = field.Value.FieldType;
			if (fieldType == typeof(float))
			{
				item2.FieldType = EFieldTypes.Float;
			}
			else if (fieldType == typeof(int))
			{
				item2.FieldType = EFieldTypes.Int;
			}
			else if (fieldType == typeof(double))
			{
				item2.FieldType = EFieldTypes.Double;
			}
			else if (fieldType == typeof(bool))
			{
				item2.FieldType = EFieldTypes.Bool;
			}
			else if (fieldType == typeof(FixedString32Bytes))
			{
				item2.FieldType = EFieldTypes.String;
			}
			else if (!fieldType.IsPrimitive)
			{
				item2.FieldType = EFieldTypes.LightUserData;
				ReflectionMetaNames.ReflectedNames.TryGetValue(fieldType, out item2.MetatableName);
			}
			item2.Size = Marshal.SizeOf(fieldType);
			item.FieldList.TryAdd(field.Key, item2);
		}
		item.FunctionList = new NativeHashMap<int, IntPtr>(functionList.Count + functionPtrList.Count, Allocator.Persistent);
		foreach (KeyValuePair<int, lua_CFunction> function in functionList)
		{
			item.FunctionList.TryAdd(function.Key, Marshal.GetFunctionPointerForDelegate(function.Value));
		}
		foreach (KeyValuePair<int, FunctionPointer<lua_CFunction>> functionPtr in functionPtrList)
		{
			item.FunctionList.TryAdd(functionPtr.Key, functionPtr.Value.Value);
		}
		ClassList.InfoFields.Data.Add(item.NameHash, item);
	}

	[BurstCompile]
	[MonoPInvokeCallback(typeof(Index_00004CF0_0024PostfixBurstDelegate))]
	public unsafe static int Index(lua_State* L)
	{
		return Index_00004CF0_0024BurstDirectCall.Invoke(L);
	}

	[BurstCompile]
	[MonoPInvokeCallback(typeof(NewIndex_00004CF1_0024PostfixBurstDelegate))]
	public unsafe static int NewIndex(lua_State* L)
	{
		return NewIndex_00004CF1_0024BurstDirectCall.Invoke(L);
	}

	[BurstCompile]
	[MonoPInvokeCallback(typeof(NameCall_00004CF2_0024PostfixBurstDelegate))]
	public unsafe static int NameCall(lua_State* L)
	{
		return NameCall_00004CF2_0024BurstDirectCall.Invoke(L);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	internal unsafe static int Index_0024BurstManaged(lua_State* L)
	{
		FixedString32Bytes output = _k_metatableLookup;
		byte* k = (byte*)UnsafeUtility.AddressOf(ref output) + 2;
		Luau.luaL_getmetafield(L, 1, k);
		if (!ClassList.InfoFields.Data.TryGetValue((int)Luau.luaL_checknumber(L, -1), out var item))
		{
			FixedString32Bytes output2 = "\"Internal Class Info Error\"";
			Luau.luaL_errorL(L, (sbyte*)UnsafeUtility.AddressOf(ref output2) + 2);
			return 0;
		}
		Luau.lua_pop(L, 1);
		byte* tname = (byte*)UnsafeUtility.AddressOf(ref item.Name) + 2;
		IntPtr zero = IntPtr.Zero;
		switch ((Luau.lua_Types)Luau.lua_type(L, 1))
		{
		case Luau.lua_Types.LUA_TUSERDATA:
			zero = (IntPtr)Luau.luaL_checkudata(L, 1, tname);
			break;
		case Luau.lua_Types.LUA_TTABLE:
			zero = Luau.lua_light_ptr(L, 1);
			break;
		default:
		{
			FixedString32Bytes output3 = "\"Unknown type for __index\"";
			Luau.luaL_errorL(L, (sbyte*)UnsafeUtility.AddressOf(ref output3) + 2);
			return 0;
		}
		}
		int len = Luau.lua_objlen(L, 2);
		int key = LuaHashing.ByteHash(Luau.luaL_checkstring(L, 2), len);
		if (item.FieldList.TryGetValue(key, out var item2))
		{
			IntPtr intPtr = zero + item2.Offset;
			switch (item2.FieldType)
			{
			case EFieldTypes.Float:
				Luau.lua_pushnumber(L, *(float*)(void*)intPtr);
				return 1;
			case EFieldTypes.Int:
				Luau.lua_pushnumber(L, *(int*)(void*)intPtr);
				return 1;
			case EFieldTypes.Double:
				Luau.lua_pushnumber(L, *(double*)(void*)intPtr);
				return 1;
			case EFieldTypes.Bool:
				Luau.lua_pushboolean(L, (*(bool*)(void*)intPtr) ? 1 : 0);
				return 1;
			case EFieldTypes.String:
				Luau.lua_pushstring(L, (byte*)(void*)intPtr + 2);
				return 1;
			case EFieldTypes.LightUserData:
				Luau.lua_class_push(L, item2.MetatableName, intPtr);
				return 1;
			}
		}
		if (item.FunctionList.TryGetValue(key, out var item3))
		{
			FunctionPointer<lua_CFunction> fn = new FunctionPointer<lua_CFunction>(item3);
			FixedString32Bytes output4 = "";
			Luau.lua_pushcclosurek(L, fn, (byte*)UnsafeUtility.AddressOf(ref output4) + 2, 0, null);
			return 1;
		}
		FixedString32Bytes output5 = "\"Unknown Type?\"";
		Luau.luaL_errorL(L, (sbyte*)UnsafeUtility.AddressOf(ref output5) + 2);
		return 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	internal unsafe static int NewIndex_0024BurstManaged(lua_State* L)
	{
		FixedString32Bytes output = _k_metatableLookup;
		byte* k = (byte*)UnsafeUtility.AddressOf(ref output) + 2;
		Luau.luaL_getmetafield(L, 1, k);
		if (!ClassList.InfoFields.Data.TryGetValue((int)Luau.luaL_checknumber(L, -1), out var item))
		{
			FixedString32Bytes output2 = "\"Internal Class Info Error\"";
			Luau.luaL_errorL(L, (sbyte*)UnsafeUtility.AddressOf(ref output2) + 2);
			return 0;
		}
		Luau.lua_pop(L, 1);
		byte* tname = (byte*)UnsafeUtility.AddressOf(ref item.Name) + 2;
		IntPtr zero = IntPtr.Zero;
		switch ((Luau.lua_Types)Luau.lua_type(L, 1))
		{
		case Luau.lua_Types.LUA_TUSERDATA:
			zero = (IntPtr)Luau.luaL_checkudata(L, 1, tname);
			break;
		case Luau.lua_Types.LUA_TTABLE:
			zero = Luau.lua_light_ptr(L, 1);
			break;
		default:
		{
			FixedString32Bytes output3 = "\"Unknown type for __newindex\"";
			Luau.luaL_errorL(L, (sbyte*)UnsafeUtility.AddressOf(ref output3) + 2);
			return 0;
		}
		}
		int len = Luau.lua_objlen(L, 2);
		int key = LuaHashing.ByteHash(Luau.luaL_checkstring(L, 2), len);
		if (item.FieldList.TryGetValue(key, out var item2))
		{
			IntPtr intPtr = zero + item2.Offset;
			switch (item2.FieldType)
			{
			case EFieldTypes.Float:
				*(float*)(void*)intPtr = (float)Luau.luaL_checknumber(L, 3);
				return 0;
			case EFieldTypes.Int:
				*(int*)(void*)intPtr = (int)Luau.luaL_checknumber(L, 3);
				return 0;
			case EFieldTypes.Double:
				*(double*)(void*)intPtr = Luau.luaL_checknumber(L, 3);
				return 0;
			case EFieldTypes.Bool:
				*(bool*)(void*)intPtr = Luau.lua_toboolean(L, 3) != 0;
				return 0;
			case EFieldTypes.LightUserData:
				Buffer.MemoryCopy((void*)(IntPtr)Luau.lua_class_get(L, 3, item2.MetatableName), (void*)intPtr, item2.Size, item2.Size);
				return 0;
			}
		}
		FixedString32Bytes output4 = "\"Unknown Type\"";
		Luau.luaL_errorL(L, (sbyte*)UnsafeUtility.AddressOf(ref output4) + 2);
		return 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	internal unsafe static int NameCall_0024BurstManaged(lua_State* L)
	{
		FixedString32Bytes output = _k_metatableLookup;
		byte* k = (byte*)UnsafeUtility.AddressOf(ref output) + 2;
		Luau.luaL_getmetafield(L, 1, k);
		if (!ClassList.InfoFields.Data.TryGetValue((int)Luau.luaL_checknumber(L, -1), out var item))
		{
			FixedString32Bytes output2 = "\"Internal Class Info Error\"";
			Luau.luaL_errorL(L, (sbyte*)UnsafeUtility.AddressOf(ref output2) + 2);
			return 0;
		}
		Luau.lua_pop(L, 1);
		int key = LuaHashing.ByteHash(Luau.lua_namecallatom(L, null));
		if (item.FunctionList.TryGetValue(key, out var item2))
		{
			return new FunctionPointer<lua_CFunction>(item2).Invoke(L);
		}
		FixedString32Bytes output3 = "\"Function not found in function list\"";
		Luau.luaL_errorL(L, (sbyte*)UnsafeUtility.AddressOf(ref output3) + 2);
		return 0;
	}
}
