using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public class LuauClassBuilder<T> where T : unmanaged
{
	private string _className;

	private Type _classType;

	private Dictionary<string, lua_CFunction> _staticFunctions = new Dictionary<string, lua_CFunction>();

	private Dictionary<string, FunctionPointer<lua_CFunction>> _staticFunctionPtrs = new Dictionary<string, FunctionPointer<lua_CFunction>>();

	private Dictionary<int, FieldInfo> _classFields = new Dictionary<int, FieldInfo>();

	private Dictionary<string, lua_CFunction> _properties = new Dictionary<string, lua_CFunction>();

	private Dictionary<string, FunctionPointer<lua_CFunction>> _propertyPtrs = new Dictionary<string, FunctionPointer<lua_CFunction>>();

	private Dictionary<int, lua_CFunction> _functions = new Dictionary<int, lua_CFunction>();

	private Dictionary<int, FunctionPointer<lua_CFunction>> _functionPtrs = new Dictionary<int, FunctionPointer<lua_CFunction>>();

	public LuauClassBuilder(string className)
	{
		_className = className;
		_classType = typeof(T);
	}

	public LuauClassBuilder<T> AddField(string luaName, string fieldName = null)
	{
		if (fieldName == null)
		{
			fieldName = luaName;
		}
		FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
		if (field == null)
		{
			throw new ArgumentException("Property " + fieldName + " does not exist on type " + typeof(T).Name + ".");
		}
		_classFields.TryAdd(LuaHashing.ByteHash(luaName), field);
		return this;
	}

	public LuauClassBuilder<T> AddStaticFunction(string luaName, lua_CFunction function)
	{
		_staticFunctions.TryAdd(luaName, function);
		return this;
	}

	public LuauClassBuilder<T> AddStaticFunction(string luaName, FunctionPointer<lua_CFunction> function)
	{
		_staticFunctionPtrs.TryAdd(luaName, function);
		return this;
	}

	public LuauClassBuilder<T> AddProperty(string luaName, lua_CFunction function)
	{
		_properties.TryAdd(luaName, function);
		return this;
	}

	public LuauClassBuilder<T> AddProperty(string luaName, FunctionPointer<lua_CFunction> function)
	{
		_propertyPtrs.TryAdd(luaName, function);
		return this;
	}

	public LuauClassBuilder<T> AddFunction(string luaName, lua_CFunction function)
	{
		if (luaName.StartsWith("__"))
		{
			_staticFunctions.TryAdd(luaName, function);
		}
		_functions.TryAdd(LuaHashing.ByteHash(luaName), function);
		return this;
	}

	public LuauClassBuilder<T> AddFunction(string luaName, FunctionPointer<lua_CFunction> function)
	{
		if (luaName.StartsWith("__"))
		{
			_staticFunctionPtrs.TryAdd(luaName, function);
		}
		_functionPtrs.TryAdd(LuaHashing.ByteHash(luaName), function);
		return this;
	}

	public unsafe LuauClassBuilder<T> Build(lua_State* L, bool global)
	{
		BurstClassInfo.NewClass<T>(_className, _classFields, _functions, _functionPtrs);
		Luau.luaL_newmetatable(L, _className);
		FunctionPointer<lua_CFunction> fn = BurstCompiler.CompileFunctionPointer<lua_CFunction>(BurstClassInfo.Index);
		Luau.lua_pushcfunction(L, fn, null);
		Luau.lua_setfield(L, -2, "__index");
		FunctionPointer<lua_CFunction> fn2 = BurstCompiler.CompileFunctionPointer<lua_CFunction>(BurstClassInfo.NameCall);
		Luau.lua_pushcfunction(L, fn2, null);
		Luau.lua_setfield(L, -2, "__namecall");
		FunctionPointer<lua_CFunction> fn3 = BurstCompiler.CompileFunctionPointer<lua_CFunction>(BurstClassInfo.NewIndex);
		Luau.lua_pushcfunction(L, fn3, null);
		Luau.lua_setfield(L, -2, "__newindex");
		foreach (KeyValuePair<string, lua_CFunction> staticFunction in _staticFunctions)
		{
			Luau.lua_pushcfunction(L, staticFunction.Value, staticFunction.Key);
			Luau.lua_setfield(L, -2, staticFunction.Key);
		}
		foreach (KeyValuePair<string, FunctionPointer<lua_CFunction>> staticFunctionPtr in _staticFunctionPtrs)
		{
			Luau.lua_pushcfunction(L, staticFunctionPtr.Value, staticFunctionPtr.Key);
			Luau.lua_setfield(L, -2, staticFunctionPtr.Key);
		}
		FixedString32Bytes output = "metahash";
		byte* k = (byte*)UnsafeUtility.AddressOf(ref output) + 2;
		Luau.lua_pushnumber(L, LuaHashing.ByteHash(_className));
		Luau.lua_setfield(L, -2, k);
		Luau.lua_setreadonly(L, -1, 1);
		Luau.lua_pop(L, 1);
		if (global)
		{
			Luau.lua_createtable(L, 0, 0);
			foreach (KeyValuePair<string, lua_CFunction> staticFunction2 in _staticFunctions)
			{
				Luau.lua_pushcfunction(L, staticFunction2.Value, staticFunction2.Key);
				Luau.lua_setfield(L, -2, staticFunction2.Key);
			}
			foreach (KeyValuePair<string, FunctionPointer<lua_CFunction>> staticFunctionPtr2 in _staticFunctionPtrs)
			{
				Luau.lua_pushcfunction(L, staticFunctionPtr2.Value, staticFunctionPtr2.Key);
				Luau.lua_setfield(L, -2, staticFunctionPtr2.Key);
			}
			Luau.lua_pushnumber(L, LuaHashing.ByteHash(_className));
			Luau.lua_setfield(L, -2, k);
			Luau.luaL_getmetatable(L, _className);
			Luau.lua_setmetatable(L, -2);
			Luau.lua_setglobal(L, _className);
		}
		return this;
	}
}
