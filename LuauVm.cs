using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.StructWrapping;
using GorillaExtensions;
using GorillaGameModes;
using GT_CustomMapSupportRuntime;
using Photon.Pun;
using Photon.Realtime;
using Unity.Collections;
using UnityEngine;

public class LuauVm : MonoBehaviourPunCallbacks, IOnEventCallback
{
	public static List<object> ClassBuilders = new List<object>();

	public static List<GCHandle> Handles = new List<GCHandle>();

	private static Dictionary<int, float> callTimers = new Dictionary<int, float>();

	private static float callCount = 25f;

	public static Queue<object[]> eventQueue = new Queue<object[]>();

	public static Queue<object[]> localEventQueue = new Queue<object[]>();

	public static Queue<GameObject> touchEventsQueue = new Queue<GameObject>();

	private void LateUpdate()
	{
		foreach (LuauScriptRunner scriptRunner in LuauScriptRunner.ScriptRunners)
		{
			if (!scriptRunner.Tick(Time.deltaTime))
			{
				LuauHud.Instance.LuauLog(scriptRunner.ScriptName + " errored out");
				LuauScriptRunner.ScriptRunners.Remove(scriptRunner);
				break;
			}
		}
	}

	private void Start()
	{
	}

	private void Awake()
	{
	}

	public void OnEvent(EventData eventData)
	{
		if (eventData.Code != 180)
		{
			return;
		}
		float value = 0f;
		callTimers.TryGetValue(eventData.Sender, out value);
		if (value < Time.time - 1f)
		{
			value = Time.time - 1f;
		}
		value += 1f / callCount;
		callTimers[eventData.Sender] = value;
		if (!(value > Time.time))
		{
			object[] array = new object[2]
			{
				NetworkSystem.Instance.GetPlayer(eventData.Sender),
				(object[])eventData.CustomData
			};
			if (array.Length <= 20)
			{
				eventQueue.Enqueue(array);
			}
		}
	}

	public unsafe static int SendEvent(lua_State* L, object[] args, bool useTable = true)
	{
		try
		{
			NetPlayer netPlayer = null;
			if (args[0] is NetPlayer)
			{
				netPlayer = (NetPlayer)args[0];
				args = (object[])args[1];
			}
			if (GorillaGameManager.instance.GameType() != GameModeType.Custom)
			{
				return -1;
			}
			Luau.lua_getfield(L, -10002, "onEvent");
			if (Luau.lua_type(L, -1) == 7)
			{
				if (args[0] is string text)
				{
					if (string.IsNullOrEmpty(text))
					{
						Luau.lua_pop(L, 1);
						return 0;
					}
					if (text.Length > 30)
					{
						Luau.lua_pop(L, 1);
						return 0;
					}
					Luau.lua_pushstring(L, (string)args[0]);
					if (useTable)
					{
						Luau.lua_createtable(L, args.Length, 0);
					}
					for (int i = 1; i < args.Length; i++)
					{
						object obj = args[i];
						if (obj.IsType<double>())
						{
							if (!double.IsFinite((double)obj))
							{
								continue;
							}
							Luau.lua_pushnumber(L, (double)obj);
						}
						else if (obj.IsType<bool>())
						{
							Luau.lua_pushboolean(L, (int)obj);
						}
						else if (obj.IsType<Vector3>())
						{
							Vector3 vector = (Vector3)obj;
							vector.ClampMagnitudeSafe(10000000f);
							*Luau.lua_class_push<Vector3>(L, "Vec3") = vector;
						}
						else if (obj.IsType<Quaternion>())
						{
							Quaternion quaternion = (Quaternion)obj;
							if (!float.IsFinite(quaternion.x) || !float.IsFinite(quaternion.y) || !float.IsFinite(quaternion.z) || !float.IsFinite(quaternion.w))
							{
								continue;
							}
							*Luau.lua_class_push<Quaternion>(L, "Quat") = quaternion;
						}
						else if (obj.IsType<Player>())
						{
							int actorNumber = ((Player)obj).ActorNumber;
							if (Bindings.LuauPlayerList.TryGetValue(actorNumber, out var value))
							{
								Luau.lua_class_push(L, "Player", value);
							}
							else
							{
								NetPlayer netPlayer2 = (NetPlayer)obj;
								if (netPlayer2 == null)
								{
									Luau.lua_pushnil(L);
								}
								else
								{
									Bindings.LuauPlayer* ptr = Luau.lua_class_push<Bindings.LuauPlayer>(L);
									ptr->PlayerID = netPlayer2.ActorNumber;
									ptr->PlayerName = netPlayer2.SanitizedNickName;
									ptr->PlayerMaterial = 0;
									ptr->IsMasterClient = netPlayer2.IsMasterClient;
									VRRigCache.Instance.TryGetVrrig(netPlayer2, out var playerRig);
									VRRig rig = playerRig.Rig;
									Bindings.LuauVRRigList[netPlayer2.ActorNumber] = rig;
									Bindings.PlayerFunctions.UpdatePlayer(L, rig, ptr);
									Bindings.LuauPlayerList[netPlayer2.ActorNumber] = (IntPtr)ptr;
								}
							}
						}
						else if (obj.IsType<Bindings.LuauAIAgent>())
						{
							int entityID = ((Bindings.LuauAIAgent)obj).EntityID;
							if (Bindings.LuauAIAgentList.TryGetValue(entityID, out var value2))
							{
								Luau.lua_class_push(L, "AIAgent", value2);
							}
							else
							{
								bool flag = false;
								if (Bindings.LuauAIAgentList.Count + Bindings.LuauGrabbablesList.Count == GT_CustomMapSupportRuntime.Constants.aiAgentLimit)
								{
									Debug.Log("[LuauVM::OnEvent] Custom Map AI Agent limit has already been reached!");
								}
								else
								{
									GameEntityManager entityManager = CustomMapsGameManager.GetEntityManager();
									if (entityManager.IsNotNull())
									{
										GameEntityId entityIdFromNetId = entityManager.GetEntityIdFromNetId(entityID);
										GameEntity gameEntity = entityManager.GetGameEntity(entityIdFromNetId);
										if (gameEntity.IsNotNull() && gameEntity.gameObject.IsNotNull() && gameEntity.gameObject.GetComponent<GameAgent>() != null)
										{
											Bindings.LuauAIAgent* ptr2 = Luau.lua_class_push<Bindings.LuauAIAgent>(L);
											Bindings.AIAgentFunctions.UpdateEntity(gameEntity, ptr2);
											Bindings.LuauAIAgentList[entityID] = (IntPtr)ptr2;
											flag = true;
										}
									}
								}
								if (!flag)
								{
									Luau.lua_pushnil(L);
								}
							}
						}
						else if (obj == null)
						{
							Luau.lua_pushnil(L);
						}
						if (useTable)
						{
							Luau.lua_rawseti(L, -2, i);
						}
					}
					if (netPlayer != null)
					{
						int actorNumber2 = netPlayer.ActorNumber;
						if (Bindings.LuauPlayerList.TryGetValue(actorNumber2, out var value3))
						{
							Luau.lua_class_push(L, "Player", value3);
						}
						else
						{
							NetPlayer netPlayer3 = netPlayer;
							if (netPlayer3 == null)
							{
								Luau.lua_pushnil(L);
							}
							else
							{
								Bindings.LuauPlayer* ptr3 = Luau.lua_class_push<Bindings.LuauPlayer>(L);
								ptr3->PlayerID = netPlayer3.ActorNumber;
								ptr3->PlayerName = netPlayer3.SanitizedNickName;
								ptr3->PlayerMaterial = 0;
								ptr3->IsMasterClient = netPlayer3.IsMasterClient;
								VRRigCache.Instance.TryGetVrrig(netPlayer3, out var playerRig2);
								VRRig rig2 = playerRig2.Rig;
								Bindings.LuauVRRigList[netPlayer3.ActorNumber] = rig2;
								Bindings.PlayerFunctions.UpdatePlayer(L, rig2, ptr3);
								Bindings.LuauPlayerList[netPlayer3.ActorNumber] = (IntPtr)ptr3;
							}
						}
						return Luau.lua_pcall(L, 3, 0, 0);
					}
					return Luau.lua_pcall(L, 2, 0, 0);
				}
				Luau.lua_pop(L, 1);
				return 0;
			}
			Luau.lua_pop(L, 1);
			return 0;
		}
		catch (Exception)
		{
		}
		return 0;
	}

	public unsafe static void ProcessEvents()
	{
		while (eventQueue.Count > 0)
		{
			object[] args = eventQueue.Dequeue();
			foreach (LuauScriptRunner scriptRunner in LuauScriptRunner.ScriptRunners)
			{
				if (scriptRunner.ShouldTick)
				{
					int status = SendEvent(scriptRunner.L, args);
					scriptRunner.ShouldTick = !LuauScriptRunner.ErrorCheck(scriptRunner.L, status);
				}
			}
		}
		while (localEventQueue.Count > 0)
		{
			object[] args2 = localEventQueue.Dequeue();
			foreach (LuauScriptRunner scriptRunner2 in LuauScriptRunner.ScriptRunners)
			{
				if (scriptRunner2.ShouldTick)
				{
					int status2 = SendEvent(scriptRunner2.L, args2, useTable: false);
					scriptRunner2.ShouldTick = !LuauScriptRunner.ErrorCheck(scriptRunner2.L, status2);
				}
			}
		}
		while (touchEventsQueue.Count > 0)
		{
			GameObject key = touchEventsQueue.Dequeue();
			foreach (LuauScriptRunner scriptRunner3 in LuauScriptRunner.ScriptRunners)
			{
				if (scriptRunner3.ShouldTick && Bindings.LuauTriggerCallbacks.TryGetValue(key, out var value))
				{
					Luau.lua_getref(scriptRunner3.L, value);
					if (Luau.lua_type(scriptRunner3.L, -1) == 7)
					{
						int status3 = Luau.lua_pcall(scriptRunner3.L, 0, 0, 0);
						scriptRunner3.ShouldTick = !LuauScriptRunner.ErrorCheck(scriptRunner3.L, status3);
					}
				}
			}
		}
	}

	~LuauVm()
	{
		try
		{
			foreach (GCHandle handle in Handles)
			{
				handle.Free();
			}
			if (!BurstClassInfo.ClassList.InfoFields.Data.IsCreated)
			{
				return;
			}
			foreach (KVPair<int, BurstClassInfo.ClassInfo> datum in BurstClassInfo.ClassList.InfoFields.Data)
			{
				if (datum.Value.FieldList.IsCreated)
				{
					datum.Value.FieldList.Dispose();
				}
			}
			BurstClassInfo.ClassList.InfoFields.Data.Dispose();
		}
		catch (ObjectDisposedException message)
		{
			Debug.Log(message);
		}
	}
}
