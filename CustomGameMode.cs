using System;
using System.Collections.Generic;
using System.IO;
using AOT;
using Fusion;
using GorillaExtensions;
using GorillaGameModes;
using GorillaNetworking;
using GT_CustomMapSupportRuntime;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public sealed class CustomGameMode : GorillaGameManager
{
	public static LuauScriptRunner gameScriptRunner;

	public static string LuaScript = "";

	private static bool WasInRoom = false;

	public static bool GameModeInitialized;

	public override void OnSerializeRead(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	public override void OnSerializeRead(object obj)
	{
	}

	public override void OnSerializeWrite(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	public override object OnSerializeWrite()
	{
		return null;
	}

	public override void AddFusionDataBehaviour(NetworkObject obj)
	{
	}

	public override GameModeType GameType()
	{
		return GameModeType.Custom;
	}

	public unsafe override int MyMatIndex(NetPlayer forPlayer)
	{
		if (gameScriptRunner == null)
		{
			return 0;
		}
		if (!gameScriptRunner.ShouldTick)
		{
			return 0;
		}
		if (Bindings.LuauPlayerList.TryGetValue(forPlayer.ActorNumber, out var value))
		{
			return ((Bindings.LuauPlayer*)(void*)value)->PlayerMaterial;
		}
		return 0;
	}

	public unsafe override void OnPlayerEnteredRoom(NetPlayer player)
	{
		try
		{
			if (gameScriptRunner != null && gameScriptRunner.ShouldTick && !Bindings.LuauPlayerList.ContainsKey(player.ActorNumber))
			{
				lua_State* l = gameScriptRunner.L;
				Luau.lua_getglobal(l, "Players");
				int num = Luau.lua_objlen(l, -1);
				Bindings.LuauPlayer* ptr = Luau.lua_class_push<Bindings.LuauPlayer>(l);
				ptr->PlayerID = player.ActorNumber;
				ptr->PlayerMaterial = 0;
				ptr->IsMasterClient = player.IsMasterClient;
				VRRig vRRig = FindPlayerVRRig(player);
				ptr->PlayerName = vRRig.playerNameVisible;
				Bindings.LuauVRRigList[player.ActorNumber] = vRRig;
				Bindings.PlayerFunctions.UpdatePlayer(l, vRRig, ptr);
				Bindings.LuauPlayerList[player.ActorNumber] = (IntPtr)ptr;
				Luau.lua_rawseti(gameScriptRunner.L, -2, num + 1);
				ptr->PlayerName = vRRig.playerNameVisible;
				if (player.IsLocal)
				{
					ptr->IsPCVR = PlayFabAuthenticator.instance.platform.ToString() != "Quest";
					Luau.lua_rawgeti(l, -1, num + 1);
					Luau.lua_setglobal(l, "LocalPlayer");
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning(ex.ToString());
		}
	}

	public unsafe override void OnPlayerLeftRoom(NetPlayer player)
	{
		try
		{
			if (gameScriptRunner == null || !gameScriptRunner.ShouldTick)
			{
				return;
			}
			lua_State* l = gameScriptRunner.L;
			Bindings.LuauPlayerList.Remove(player.ActorNumber);
			Luau.lua_getglobal(l, "Players");
			int num = Luau.lua_objlen(l, -1);
			for (int i = 1; i <= num; i++)
			{
				Luau.lua_rawgeti(l, -1, i);
				Bindings.LuauPlayer* ptr = (Bindings.LuauPlayer*)Luau.lua_touserdata(l, -1);
				Luau.lua_pop(l, 1);
				if (ptr != null && ptr->PlayerID == player.ActorNumber)
				{
					for (int j = i; j < num; j++)
					{
						Luau.lua_rawgeti(l, -1, j + 1);
						Luau.lua_rawseti(l, -2, j);
					}
					Luau.lua_pushnil(l);
					Luau.lua_rawseti(l, -2, num);
					break;
				}
			}
			Luau.lua_pop(l, 1);
		}
		catch (Exception ex)
		{
			Debug.LogWarning(ex.ToString());
		}
	}

	public unsafe override void OnMasterClientSwitched(NetPlayer newMasterClient)
	{
		try
		{
			if (gameScriptRunner == null || !gameScriptRunner.ShouldTick)
			{
				return;
			}
			foreach (KeyValuePair<int, IntPtr> luauPlayer in Bindings.LuauPlayerList)
			{
				Bindings.LuauPlayer* ptr = (Bindings.LuauPlayer*)(void*)luauPlayer.Value;
				ptr->IsMasterClient = false;
			}
			Bindings.LuauPlayerList.TryGetValue(newMasterClient.ActorNumber, out var value);
			Bindings.LuauPlayer* ptr2 = (Bindings.LuauPlayer*)(void*)value;
			ptr2->IsMasterClient = true;
		}
		catch (Exception ex)
		{
			Debug.LogWarning(ex.ToString());
		}
	}

	public static void OnPlayerHit(GameEntity entity, int hitPlayer, float damage)
	{
		if (gameScriptRunner != null && gameScriptRunner.ShouldTick)
		{
			object[] item = new object[4]
			{
				"playerHit",
				(double)entity.GetNetId(),
				(double)hitPlayer,
				(double)damage
			};
			LuauVm.eventQueue.Enqueue(item);
		}
	}

	public static void TaggedByAI(GameEntity entity, int taggedPlayer)
	{
		if (gameScriptRunner != null && gameScriptRunner.ShouldTick)
		{
			object[] item = new object[3]
			{
				"taggedByAI",
				(double)entity.GetNetId(),
				(double)taggedPlayer
			};
			LuauVm.eventQueue.Enqueue(item);
		}
	}

	public override void HitPlayer(NetPlayer taggedPlayer)
	{
	}

	public unsafe static void OnEntityGrabbed(GameEntity entity, bool isGrabbed)
	{
		if (gameScriptRunner == null || !gameScriptRunner.ShouldTick)
		{
			return;
		}
		_ = gameScriptRunner.L;
		if (Bindings.LuauGrabbablesList.ContainsKey(entity.GetNetId()))
		{
			if (isGrabbed)
			{
				object[] item = new object[2]
				{
					"entityGrabbed",
					(double)entity.GetNetId()
				};
				LuauVm.localEventQueue.Enqueue(item);
			}
			else
			{
				object[] item2 = new object[2]
				{
					"entityReleased",
					(double)entity.GetNetId()
				};
				LuauVm.localEventQueue.Enqueue(item2);
			}
		}
	}

	public unsafe static void OnGameEntityRemoved(GameEntity entity)
	{
		if (gameScriptRunner == null || !gameScriptRunner.ShouldTick)
		{
			return;
		}
		lua_State* l = gameScriptRunner.L;
		if (Bindings.LuauAIAgentList.ContainsKey(entity.GetNetId()))
		{
			Bindings.LuauAIAgentList[entity.GetNetId()] = IntPtr.Zero;
			Luau.lua_getglobal(l, "AIAgents");
			int num = Luau.lua_objlen(l, -1);
			for (int i = 1; i <= num; i++)
			{
				Luau.lua_rawgeti(l, -1, i);
				Bindings.LuauGrabbableEntity* ptr = (Bindings.LuauGrabbableEntity*)Luau.lua_touserdata(l, -1);
				Luau.lua_pop(l, 1);
				if (ptr != null && ptr->EntityID == entity.GetNetId())
				{
					Luau.lua_pushnil(l);
					Luau.lua_rawseti(l, -2, i);
					break;
				}
			}
			Luau.lua_pop(l, 1);
			object[] item = new object[2]
			{
				"agentDestroyed",
				(double)entity.id.index
			};
			LuauVm.localEventQueue.Enqueue(item);
		}
		else
		{
			if (!Bindings.LuauGrabbablesList.ContainsKey(entity.GetNetId()))
			{
				return;
			}
			Bindings.LuauGrabbablesList[entity.GetNetId()] = IntPtr.Zero;
			Luau.lua_getglobal(l, "GrabbableEntities");
			int num2 = Luau.lua_objlen(l, -1);
			for (int j = 1; j <= num2; j++)
			{
				Luau.lua_rawgeti(l, -1, j);
				Bindings.LuauGrabbableEntity* ptr2 = (Bindings.LuauGrabbableEntity*)Luau.lua_touserdata(l, -1);
				Luau.lua_pop(l, 1);
				if (ptr2 != null && ptr2->EntityID == entity.GetNetId())
				{
					Luau.lua_pushnil(l);
					Luau.lua_rawseti(l, -2, j);
					break;
				}
			}
			Luau.lua_pop(l, 1);
			object[] item2 = new object[2]
			{
				"entityDestroyed",
				(double)entity.id.index
			};
			LuauVm.localEventQueue.Enqueue(item2);
		}
	}

	public override void StartPlaying()
	{
		base.StartPlaying();
		try
		{
			PhotonNetwork.AddCallbackTarget(this);
			GameModeInitialized = true;
			if (LuaScript != "")
			{
				LuaStart();
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning(ex.ToString());
		}
	}

	public unsafe static void LuaStart()
	{
		if (LuaScript == "")
		{
			return;
		}
		RunGamemodeScript(LuaScript);
		if (!gameScriptRunner.ShouldTick)
		{
			return;
		}
		lua_State* l = gameScriptRunner.L;
		Bindings.LuauPlayerList.Clear();
		Luau.lua_getglobal(l, "Players");
		Player[] playerList = PhotonNetwork.PlayerList;
		for (int i = 0; i < playerList.Length; i++)
		{
			NetPlayer netPlayer = playerList[i];
			if (netPlayer != null)
			{
				Bindings.LuauPlayer* ptr = Luau.lua_class_push<Bindings.LuauPlayer>(l);
				ptr->PlayerID = netPlayer.ActorNumber;
				ptr->PlayerMaterial = 0;
				ptr->IsMasterClient = netPlayer.IsMasterClient;
				Bindings.LuauPlayerList[netPlayer.ActorNumber] = (IntPtr)ptr;
				VRRigCache.Instance.TryGetVrrig(netPlayer, out var playerRig);
				VRRig rig = playerRig.Rig;
				ptr->PlayerName = rig.playerNameVisible;
				Bindings.LuauVRRigList[netPlayer.ActorNumber] = rig;
				Bindings.PlayerFunctions.UpdatePlayer(l, rig, ptr);
				ptr->PlayerName = rig.playerNameVisible;
				Luau.lua_rawseti(l, -2, i + 1);
				if (netPlayer.IsLocal)
				{
					ptr->IsPCVR = PlayFabAuthenticator.instance.platform.ToString() != "Quest";
					Luau.lua_rawgeti(l, -1, i + 1);
					Luau.lua_setglobal(l, "LocalPlayer");
				}
			}
			else
			{
				Luau.lua_pushnil(l);
				Luau.lua_rawseti(l, -2, i + 1);
			}
		}
		for (int j = playerList.Length; j <= 20; j++)
		{
			Luau.lua_pushnil(l);
			Luau.lua_rawseti(l, -2, j + 1);
		}
		Bindings.LuauAIAgentList.Clear();
		Luau.lua_getglobal(l, "AIAgents");
		List<GameAgent> agents = CustomMapsGameManager.instance.gameAgentManager.GetAgents();
		for (int k = 0; k < agents.Count; k++)
		{
			GameAgent gameAgent = agents[k];
			if (!gameAgent.IsNull() && !gameAgent.entity.IsNull())
			{
				Bindings.LuauAIAgent* ptr2 = Luau.lua_class_push<Bindings.LuauAIAgent>(l);
				Bindings.AIAgentFunctions.UpdateEntity(gameAgent.entity, ptr2);
				Bindings.LuauAIAgentList[gameAgent.entity.GetNetId()] = (IntPtr)ptr2;
				Luau.lua_rawseti(l, -2, Bindings.LuauAIAgentList.Count);
				if (Bindings.LuauAIAgentList.Count + Bindings.LuauGrabbablesList.Count == GT_CustomMapSupportRuntime.Constants.aiAgentLimit)
				{
					Debug.Log("[CustomGameMode::LuaStart] Custom Map AI Agent limit has been reached!");
					break;
				}
			}
		}
		Luau.lua_pop(l, 1);
		Bindings.LuauGrabbablesList.Clear();
		Luau.lua_getglobal(l, "GrabbableEntities");
		List<GameEntity> gameEntities = CustomMapsGameManager.instance.gameEntityManager.GetGameEntities();
		for (int m = 0; m < gameEntities.Count; m++)
		{
			GameEntity gameEntity = gameEntities[m];
			if (!gameEntity.IsNull())
			{
				Bindings.LuauGrabbableEntity* ptr3 = Luau.lua_class_push<Bindings.LuauGrabbableEntity>(l);
				Bindings.GrabbableEntityFunctions.UpdateEntity(gameEntity, ptr3);
				Bindings.LuauGrabbablesList[gameEntity.GetNetId()] = (IntPtr)ptr3;
				Luau.lua_rawseti(l, -2, Bindings.LuauGrabbablesList.Count);
				if (Bindings.LuauAIAgentList.Count + Bindings.LuauGrabbablesList.Count == GT_CustomMapSupportRuntime.Constants.aiAgentLimit)
				{
					Debug.Log("[CustomGameMode::LuaStart] Custom Map AI Agent limit has been reached!");
					break;
				}
			}
		}
		Luau.lua_pop(l, 1);
	}

	public override void StopPlaying()
	{
		base.StopPlaying();
		try
		{
			GameModeInitialized = false;
			if (gameScriptRunner != null)
			{
				StopScript();
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning(ex.ToString());
		}
	}

	public unsafe static void StopScript()
	{
		if (gameScriptRunner.ShouldTick)
		{
			Luau.lua_close(gameScriptRunner.L);
		}
		LuauScriptRunner.ScriptRunners.Remove(gameScriptRunner);
		gameScriptRunner.ShouldTick = false;
		gameScriptRunner = null;
		foreach (KeyValuePair<GameObject, Bindings.LuauGameObjectInitialState> luauGameObjectState in Bindings.LuauGameObjectStates)
		{
			Bindings.LuauGameObjectInitialState value = luauGameObjectState.Value;
			GameObject key = luauGameObjectState.Key;
			if (!key.IsNotNull())
			{
				continue;
			}
			if (value.Created)
			{
				key.Destroy();
				continue;
			}
			key.SetActive(value: true);
			key.transform.localPosition = value.Position;
			key.transform.localRotation = value.Rotation;
			key.transform.localScale = value.Scale;
			MeshRenderer component = key.GetComponent<MeshRenderer>();
			Collider component2 = key.GetComponent<Collider>();
			if (component != null)
			{
				component.enabled = value.Visible;
			}
			if (component2 != null)
			{
				component2.enabled = value.Collidable;
			}
		}
		Bindings.LuauGameObjectStates.Clear();
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

	public static void TouchPlayer(NetPlayer touchedPlayer)
	{
		if (gameScriptRunner != null && gameScriptRunner.ShouldTick)
		{
			object[] item = new object[2]
			{
				"touchedPlayer",
				touchedPlayer.GetPlayerRef()
			};
			LuauVm.localEventQueue.Enqueue(item);
		}
	}

	public static void TaggedByEnvironment()
	{
		if (gameScriptRunner != null && gameScriptRunner.ShouldTick)
		{
			object[] item = new object[2] { "taggedByEnvironment", null };
			LuauVm.localEventQueue.Enqueue(item);
		}
	}

	[MonoPInvokeCallback(typeof(lua_CFunction))]
	public unsafe static int GameModeBindings(lua_State* L)
	{
		Bindings.GorillaLocomotionSettingsBuilder(L);
		Bindings.PlayerInputBuilder(L);
		Bindings.PlayerBuilder(L);
		Bindings.GameObjectBuilder(L);
		Bindings.AIAgentBuilder(L);
		Bindings.GrabbableEntityBuilder(L);
		Bindings.RoomStateBuilder(L);
		Bindings.Components.Build(L);
		Luau.lua_createtable(L, 10, 0);
		Luau.lua_setglobal(L, "Players");
		Luau.lua_createtable(L, GT_CustomMapSupportRuntime.Constants.aiAgentLimit, 0);
		Luau.lua_setglobal(L, "AIAgents");
		Luau.lua_createtable(L, GT_CustomMapSupportRuntime.Constants.aiAgentLimit, 0);
		Luau.lua_setglobal(L, "GrabbableEntities");
		Luau.lua_register(L, Bindings.LuaEmit.Emit, "emitEvent");
		Luau.lua_register(L, Bindings.LuaStartVibration, "startVibration");
		Luau.lua_register(L, Bindings.LuaPlaySound, "playSound");
		Luau.lua_register(L, Bindings.JSON.DataSave, "dataSave");
		Luau.lua_register(L, Bindings.JSON.DataLoad, "dataLoad");
		Luau.lua_register(L, Bindings.PlayerUtils.SetVelocity, "setPlayerVelocity");
		Luau.lua_register(L, Bindings.PlayerUtils.TeleportPlayer, "setPlayerPosition");
		Luau.lua_register(L, Bindings.RayCastUtils.RayCast, "rayCast");
		return 0;
	}

	public unsafe override float[] LocalPlayerSpeed()
	{
		if (Bindings.LocomotionSettings == null || gameScriptRunner == null || !gameScriptRunner.ShouldTick)
		{
			playerSpeed[0] = 6.5f;
			playerSpeed[1] = 1.1f;
		}
		else
		{
			playerSpeed[0] = Bindings.LocomotionSettings->maxJumpSpeed.ClampSafe(0f, 100f);
			playerSpeed[1] = Bindings.LocomotionSettings->jumpMultiplier.ClampSafe(0f, 100f);
		}
		return playerSpeed;
	}

	[MonoPInvokeCallback(typeof(lua_CFunction))]
	public unsafe static int AfterTickGamemode(lua_State* L)
	{
		try
		{
			foreach (KeyValuePair<GameObject, IntPtr> luauGameObjectDepth in Bindings.LuauGameObjectDepthList)
			{
				GameObject key = luauGameObjectDepth.Key;
				if (key.IsNotNull())
				{
					Transform obj = key.transform;
					Bindings.LuauGameObject* ptr = (Bindings.LuauGameObject*)(void*)luauGameObjectDepth.Value;
					Vector3 position = ptr->Position;
					position = new Vector3((float)Math.Round(position.x, 4), (float)Math.Round(position.y, 4), (float)Math.Round(position.z, 4));
					obj.SetPositionAndRotation(position, ptr->Rotation);
					obj.localScale = ptr->Scale;
				}
			}
		}
		catch (Exception)
		{
		}
		return 0;
	}

	[MonoPInvokeCallback(typeof(lua_CFunction))]
	public unsafe static int PreTickGamemode(lua_State* L)
	{
		try
		{
			Luau.lua_pushboolean(L, (PhotonNetwork.InRoom && WasInRoom) ? 1 : 0);
			Luau.lua_setglobal(L, "InRoom");
			foreach (KeyValuePair<int, IntPtr> luauPlayer in Bindings.LuauPlayerList)
			{
				Bindings.LuauPlayer* ptr = (Bindings.LuauPlayer*)(void*)luauPlayer.Value;
				Bindings.LuauVRRigList.TryGetValue(luauPlayer.Key, out var value);
				if (!value.IsNotNull())
				{
					LuauHud.Instance.LuauLog("Unknown Rig for player");
					continue;
				}
				if (luauPlayer.Key == PhotonNetwork.LocalPlayer.ActorNumber)
				{
					ptr->IsMasterClient = PhotonNetwork.LocalPlayer.IsMasterClient;
				}
				Bindings.PlayerFunctions.UpdatePlayer(L, value, ptr);
			}
			Luau.lua_getglobal(L, "AIAgents");
			List<GameAgent> list = CustomMapsGameManager.instance?.gameAgentManager?.GetAgents();
			for (int i = 0; i < list?.Count; i++)
			{
				GameAgent gameAgent = list[i];
				if (!gameAgent.IsNull() && !gameAgent.entity.IsNull())
				{
					if (Bindings.LuauAIAgentList.TryGetValue(gameAgent.entity.GetNetId(), out var value2))
					{
						Bindings.AIAgentFunctions.UpdateEntity(gameAgent.entity, (Bindings.LuauAIAgent*)(void*)value2);
						continue;
					}
					if (Bindings.LuauAIAgentList.Count + Bindings.LuauGrabbablesList.Count == GT_CustomMapSupportRuntime.Constants.aiAgentLimit)
					{
						Debug.Log("[CustomGameMode::PreTick] Custom Map AI Agent limit has been reached!");
						continue;
					}
					Bindings.LuauAIAgent* ptr2 = Luau.lua_class_push<Bindings.LuauAIAgent>(L);
					Bindings.AIAgentFunctions.UpdateEntity(gameAgent.entity, ptr2);
					Bindings.LuauAIAgentList[gameAgent.entity.GetNetId()] = (IntPtr)ptr2;
					Luau.lua_rawseti(L, -2, Bindings.LuauAIAgentList.Count);
				}
			}
			Luau.lua_pop(L, 1);
			foreach (KeyValuePair<GameObject, IntPtr> luauGameObject in Bindings.LuauGameObjectList)
			{
				GameObject key = luauGameObject.Key;
				if (key.IsNotNull())
				{
					Transform transform = key.transform;
					Bindings.LuauGameObject* ptr3 = (Bindings.LuauGameObject*)(void*)luauGameObject.Value;
					Vector3 position = transform.position;
					position = new Vector3((float)Math.Round(position.x, 4), (float)Math.Round(position.y, 4), (float)Math.Round(position.z, 4));
					ptr3->Position = position;
					ptr3->Rotation = transform.rotation;
					ptr3->Scale = transform.localScale;
				}
			}
			Bindings.UpdateInputs();
			WasInRoom = PhotonNetwork.InRoom;
		}
		catch (Exception)
		{
		}
		return 0;
	}

	private unsafe static void RunGamemodeScript(string script)
	{
		gameScriptRunner = new LuauScriptRunner(script, "GameMode", GameModeBindings, PreTickGamemode, AfterTickGamemode);
	}

	private static void RunGamemodeScriptFromFile(string filename)
	{
		RunGamemodeScript(File.ReadAllText(Path.Join(Application.persistentDataPath, "Scripts", filename)));
	}
}
