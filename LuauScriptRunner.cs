using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

public class LuauScriptRunner
{
	public static List<LuauScriptRunner> ScriptRunners = new List<LuauScriptRunner>();

	public bool ShouldTick;

	private lua_CFunction postTickCallback;

	private lua_CFunction preTickCallback;

	public string ScriptName;

	public string Script;

	public unsafe lua_State* L;

	public unsafe static bool ErrorCheck(lua_State* L, int status)
	{
		if (status != 0)
		{
			sbyte* value = Luau.lua_tostring(L, -1);
			LuauHud.Instance.LuauLog(new string(value));
			sbyte* value2 = (sbyte*)Luau.lua_debugtrace(L);
			LuauHud.Instance.LuauLog(new string(value2));
			LuauHud.Instance.LuauLog("Error code: " + status);
			Luau.lua_close(L);
			return true;
		}
		return false;
	}

	public unsafe bool Tick(float deltaTime)
	{
		if (!ShouldTick)
		{
			return false;
		}
		preTickCallback(L);
		LuauVm.ProcessEvents();
		if (!ShouldTick)
		{
			return false;
		}
		Luau.lua_settop(L, 0);
		Luau.lua_getfield(L, -10002, "tick");
		if (Luau.lua_type(L, -1) == 7)
		{
			Luau.lua_pushnumber(L, deltaTime);
			int status = Luau.lua_pcall(L, 1, 0, 0);
			ShouldTick = !ErrorCheck(L, status);
			if (ShouldTick)
			{
				postTickCallback(L);
				Luau.lua_settop(L, 0);
				int data = Luau.lua_gc(L, 3, 0);
				Luau.lua_gc(L, 6, data);
			}
			return ShouldTick;
		}
		Luau.lua_pop(L, 1);
		return false;
	}

	public unsafe LuauScriptRunner(string script, string name, [CanBeNull] lua_CFunction bindings = null, [CanBeNull] lua_CFunction preTick = null, [CanBeNull] lua_CFunction postTick = null)
	{
		Script = script;
		ScriptName = name;
		L = Luau.luaL_newstate();
		ScriptRunners.Add(this);
		Luau.luaL_openlibs(L);
		Bindings.Vec3Builder(L);
		Bindings.QuatBuilder(L);
		bindings?.Invoke(L);
		postTickCallback = postTick;
		preTickCallback = preTick;
		nuint size = 0u;
		Luau.lua_register(L, Luau.lua_print, "print");
		byte[] bytes = Encoding.UTF8.GetBytes(script);
		sbyte* data = Luau.luau_compile(script, (nuint)bytes.Length, null, &size);
		Luau.luau_load(L, name, data, size, 0);
		int status = Luau.lua_resume(L, null, 0);
		ShouldTick = !ErrorCheck(L, status);
	}

	public LuauScriptRunner FromFile(string filePath, [CanBeNull] lua_CFunction bindings = null, [CanBeNull] lua_CFunction tick = null)
	{
		return new LuauScriptRunner(File.ReadAllText(Path.Join(Application.persistentDataPath, "Scripts", filePath)), filePath, bindings, tick);
	}

	~LuauScriptRunner()
	{
		LuauVm.ClassBuilders.Clear();
		Bindings.LuauPlayerList.Clear();
		Bindings.LuauGameObjectList.Clear();
		Bindings.LuauGameObjectListReverse.Clear();
		Bindings.LuauGameObjectStates.Clear();
		Bindings.LuauVRRigList.Clear();
		Bindings.LuauAIAgentList.Clear();
		Bindings.Components.ComponentList.Clear();
		ReflectionMetaNames.ReflectedNames.Clear();
		if (BurstClassInfo.ClassList.InfoFields.Data.IsCreated)
		{
			BurstClassInfo.ClassList.InfoFields.Data.Clear();
		}
	}
}
