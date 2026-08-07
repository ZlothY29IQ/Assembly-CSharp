using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AOT;
using ExitGames.Client.Photon;
using GT_CustomMapSupportRuntime;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaTagScripts.VirtualStumpCustomMaps;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR;

[BurstCompile]
public static class Bindings
{
	public static class LuaEmit
	{
		private static float callTime = 0f;

		private static float callCount = 20f;

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Emit(lua_State* L)
		{
			if (callTime < Time.time - 1f)
			{
				callTime = Time.time - 1f;
			}
			callTime += 1f / callCount;
			if (callTime > Time.time)
			{
				LuauHud.Instance.LuauLog("Emit rate limit reached, event not sent");
				return 0;
			}
			RaiseEventOptions raiseEventOptions = new RaiseEventOptions
			{
				Receivers = ReceiverGroup.Others
			};
			if (Luau.lua_type(L, 2) != 6)
			{
				Luau.luaL_errorL(L, "Argument 2 must be a table");
				return 0;
			}
			Luau.lua_pushnil(L);
			int num = 0;
			List<object> list = new List<object>();
			list.Add(Marshal.PtrToStringAnsi((IntPtr)Luau.luaL_checkstring(L, 1)));
			while (Luau.lua_next(L, 2) != 0 && num++ < 10)
			{
				switch ((Luau.lua_Types)Luau.lua_type(L, -1))
				{
				case Luau.lua_Types.LUA_TNUMBER:
					list.Add(Luau.luaL_checknumber(L, -1));
					Luau.lua_pop(L, 1);
					break;
				case Luau.lua_Types.LUA_TBOOLEAN:
					list.Add(Luau.lua_toboolean(L, -1) == 1);
					Luau.lua_pop(L, 1);
					break;
				case Luau.lua_Types.LUA_TTABLE:
				case Luau.lua_Types.LUA_TUSERDATA:
				{
					Luau.luaL_getmetafield(L, -1, "metahash");
					if (!BurstClassInfo.ClassList.InfoFields.Data.TryGetValue((int)Luau.luaL_checknumber(L, -1), out var item))
					{
						FixedString64Bytes output2 = "\"Internal Class Info Error No Metatable Found\"";
						Luau.luaL_errorL(L, (sbyte*)UnsafeUtility.AddressOf(ref output2) + 2);
						return 0;
					}
					Luau.lua_pop(L, 1);
					if (item.Name == (FixedString32Bytes)"Vec3")
					{
						list.Add(*Luau.lua_class_get<Vector3>(L, -1));
						Luau.lua_pop(L, 1);
					}
					else if (item.Name == (FixedString32Bytes)"Quat")
					{
						list.Add(*Luau.lua_class_get<Quaternion>(L, -1));
						Luau.lua_pop(L, 1);
					}
					else if (item.Name == (FixedString32Bytes)"Player")
					{
						int playerID = Luau.lua_class_get<LuauPlayer>(L, -1)->PlayerID;
						NetPlayer netPlayer = null;
						foreach (NetPlayer item2 in RoomSystem.PlayersInRoom)
						{
							if (item2.ActorNumber == playerID)
							{
								netPlayer = item2;
							}
						}
						if (netPlayer == null)
						{
							list.Add(null);
						}
						else
						{
							list.Add(netPlayer.GetPlayerRef());
						}
						Luau.lua_pop(L, 1);
					}
					else
					{
						FixedString32Bytes output3 = "\"Unknown Type in table\"";
						Luau.luaL_errorL(L, (sbyte*)UnsafeUtility.AddressOf(ref output3) + 2);
					}
					break;
				}
				default:
				{
					FixedString32Bytes output = "\"Unknown Type in table\"";
					Luau.luaL_errorL(L, (sbyte*)UnsafeUtility.AddressOf(ref output) + 2);
					return 0;
				}
				}
			}
			if (PhotonNetwork.InRoom)
			{
				PhotonNetwork.RaiseEvent(180, list.ToArray(), raiseEventOptions, SendOptions.SendReliable);
			}
			return 0;
		}
	}

	[BurstCompile]
	public struct LuauGameObject
	{
		public Vector3 Position;

		public Quaternion Rotation;

		public Vector3 Scale;
	}

	[BurstCompile]
	public struct LuauGameObjectInitialState
	{
		public Vector3 Position;

		public Quaternion Rotation;

		public Vector3 Scale;

		public bool Visible;

		public bool Collidable;

		public bool Created;
	}

	[BurstCompile]
	public static class GameObjectFunctions
	{
		public static int GetDepth(GameObject gameObject)
		{
			int num = 0;
			Transform transform = gameObject.transform;
			while (transform.parent != null)
			{
				num++;
				transform = transform.parent;
			}
			return num;
		}

		public static void UpdateDepthList()
		{
			LuauGameObjectDepthList.Clear();
			LuauGameObjectDepthList = LuauGameObjectList.OrderByDescending((KeyValuePair<GameObject, IntPtr> kv) => GetDepth(kv.Key)).ToList();
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int New(lua_State* L)
		{
			GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
			LuauGameObject* ptr = Luau.lua_class_push<LuauGameObject>(L);
			ptr->Position = gameObject.transform.position;
			ptr->Rotation = gameObject.transform.rotation;
			ptr->Scale = gameObject.transform.localScale;
			LuauGameObjectList.TryAdd(gameObject, (IntPtr)ptr);
			LuauGameObjectListReverse.TryAdd((IntPtr)ptr, gameObject);
			return 1;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int FindGameObject(lua_State* L)
		{
			GameObject gameObject = GameObject.Find(new string((sbyte*)Luau.luaL_checkstring(L, 1)));
			if (gameObject != null)
			{
				if (!CustomMapLoader.IsCustomScene(gameObject.scene.name))
				{
					return 0;
				}
				if (LuauGameObjectList.TryGetValue(gameObject, out var value))
				{
					Luau.lua_class_push(L, "GameObject", value);
				}
				else
				{
					LuauGameObject* ptr = Luau.lua_class_push<LuauGameObject>(L);
					ptr->Position = gameObject.transform.position;
					ptr->Rotation = gameObject.transform.rotation;
					ptr->Scale = gameObject.transform.localScale;
					LuauGameObjectInitialState value2 = new LuauGameObjectInitialState
					{
						Position = gameObject.transform.localPosition,
						Rotation = gameObject.transform.localRotation,
						Scale = gameObject.transform.localScale,
						Visible = true,
						Collidable = true,
						Created = false
					};
					MeshRenderer component = gameObject.GetComponent<MeshRenderer>();
					Collider component2 = gameObject.GetComponent<Collider>();
					if (component2.IsNotNull())
					{
						value2.Collidable = component2.enabled;
					}
					if (component.IsNotNull())
					{
						value2.Visible = component.enabled;
					}
					LuauGameObjectList.TryAdd(gameObject, (IntPtr)ptr);
					LuauGameObjectListReverse.TryAdd((IntPtr)ptr, gameObject);
					LuauGameObjectStates.TryAdd(gameObject, value2);
					UpdateDepthList();
				}
				return 1;
			}
			return 0;
		}

		public static Transform FindChild(Transform parent, string name)
		{
			foreach (Transform item in parent)
			{
				if (item.name == name)
				{
					return item;
				}
				Transform transform2 = FindChild(item, name);
				if (transform2 != null)
				{
					return transform2;
				}
			}
			return null;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int FindChildGameObject(lua_State* L)
		{
			LuauGameObject* ptr = Luau.lua_class_get<LuauGameObject>(L, 1, "GameObject");
			if (LuauGameObjectListReverse.TryGetValue((IntPtr)ptr, out var value))
			{
				string name = new string((sbyte*)Luau.luaL_checkstring(L, 2));
				GameObject gameObject = FindChild(value.transform, name)?.gameObject;
				if (gameObject.IsNotNull())
				{
					if (LuauGameObjectList.TryGetValue(gameObject, out var value2))
					{
						Luau.lua_class_push(L, "GameObject", value2);
					}
					else
					{
						LuauGameObject* ptr2 = Luau.lua_class_push<LuauGameObject>(L);
						ptr2->Position = gameObject.transform.position;
						ptr2->Rotation = gameObject.transform.rotation;
						ptr2->Scale = gameObject.transform.localScale;
						LuauGameObjectInitialState value3 = new LuauGameObjectInitialState
						{
							Position = gameObject.transform.localPosition,
							Rotation = gameObject.transform.localRotation,
							Scale = gameObject.transform.localScale,
							Visible = true,
							Collidable = true,
							Created = false
						};
						MeshRenderer component = gameObject.GetComponent<MeshRenderer>();
						Collider component2 = gameObject.GetComponent<Collider>();
						if (component2.IsNotNull())
						{
							value3.Collidable = component2.enabled;
						}
						if (component.IsNotNull())
						{
							value3.Visible = component.enabled;
						}
						LuauGameObjectList.TryAdd(gameObject, (IntPtr)ptr2);
						LuauGameObjectListReverse.TryAdd((IntPtr)ptr2, gameObject);
						LuauGameObjectStates.TryAdd(gameObject, value3);
						UpdateDepthList();
					}
					return 1;
				}
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int FindComponent(lua_State* L)
		{
			LuauGameObject* ptr = Luau.lua_class_get<LuauGameObject>(L, 1, "GameObject");
			if (LuauGameObjectListReverse.TryGetValue((IntPtr)ptr, out var value))
			{
				if (value == null)
				{
					return 0;
				}
				switch (new string((sbyte*)Luau.luaL_checkstring(L, 2)))
				{
				case "ParticleSystem":
				{
					ParticleSystem component3 = value.GetComponent<ParticleSystem>();
					if (component3 == null)
					{
						return 0;
					}
					Components.LuauParticleSystemBindings.LuauParticleSystem* ptr4 = Luau.lua_class_push<Components.LuauParticleSystemBindings.LuauParticleSystem>(L);
					Components.ComponentList.TryAdd((IntPtr)ptr4, component3);
					return 1;
				}
				case "AudioSource":
				{
					AudioSource component2 = value.GetComponent<AudioSource>();
					if (component2 == null)
					{
						return 0;
					}
					Components.LuauAudioSourceBindings.LuauAudioSource* ptr3 = Luau.lua_class_push<Components.LuauAudioSourceBindings.LuauAudioSource>(L);
					Components.ComponentList.TryAdd((IntPtr)ptr3, component2);
					return 1;
				}
				case "Light":
				{
					Light component4 = value.GetComponent<Light>();
					if (component4 == null)
					{
						return 0;
					}
					Components.LuauLightBindings.LuauLight* ptr5 = Luau.lua_class_push<Components.LuauLightBindings.LuauLight>(L);
					Components.ComponentList.TryAdd((IntPtr)ptr5, component4);
					return 1;
				}
				case "Animator":
				{
					Animator component = value.GetComponent<Animator>();
					if (component == null)
					{
						return 0;
					}
					Components.LuauAnimatorBindings.LuauAnimator* ptr2 = Luau.lua_class_push<Components.LuauAnimatorBindings.LuauAnimator>(L);
					Components.ComponentList.TryAdd((IntPtr)ptr2, component);
					return 1;
				}
				}
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int CloneGameObject(lua_State* L)
		{
			LuauGameObject* ptr = Luau.lua_class_get<LuauGameObject>(L, 1, "GameObject");
			if (LuauGameObjectListReverse.TryGetValue((IntPtr)ptr, out var value))
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(value, value.transform.parent, worldPositionStays: false);
				LuauGameObject* ptr2 = Luau.lua_class_push<LuauGameObject>(L);
				ptr2->Position = gameObject.transform.position;
				ptr2->Rotation = gameObject.transform.rotation;
				ptr2->Scale = gameObject.transform.localScale;
				LuauGameObjectInitialState value2 = new LuauGameObjectInitialState
				{
					Position = gameObject.transform.localPosition,
					Rotation = gameObject.transform.localRotation,
					Scale = gameObject.transform.localScale,
					Visible = true,
					Collidable = true,
					Created = true
				};
				MeshRenderer component = gameObject.GetComponent<MeshRenderer>();
				Collider component2 = gameObject.GetComponent<Collider>();
				if (component2.IsNotNull())
				{
					value2.Collidable = component2.enabled;
				}
				if (component.IsNotNull())
				{
					value2.Visible = component.enabled;
				}
				LuauGameObjectList.TryAdd(gameObject, (IntPtr)ptr2);
				LuauGameObjectListReverse.TryAdd((IntPtr)ptr2, gameObject);
				LuauGameObjectStates.TryAdd(gameObject, value2);
				UpdateDepthList();
				return 1;
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int DestroyGameObject(lua_State* L)
		{
			LuauGameObject* ptr = Luau.lua_class_get<LuauGameObject>(L, 1, "GameObject");
			if (LuauGameObjectListReverse.TryGetValue((IntPtr)ptr, out var value) && LuauGameObjectStates.TryGetValue(value, out var value2))
			{
				if (!value2.Created)
				{
					Luau.luaL_errorL(L, "Cannot destroy a non-instantiated GameObject.");
					return 0;
				}
				Queue<GameObject> queue = new Queue<GameObject>();
				queue.Enqueue(value);
				while (queue.Count != 0)
				{
					GameObject gameObject = queue.Dequeue();
					if (!LuauGameObjectList.TryGetValue(gameObject, out var value3))
					{
						continue;
					}
					LuauGameObjectList.Remove(gameObject);
					LuauGameObjectListReverse.Remove(value3);
					LuauGameObjectStates.Remove(gameObject);
					foreach (Transform item in gameObject.transform)
					{
						queue.Enqueue(item.gameObject);
					}
				}
				UpdateDepthList();
				value.Destroy();
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int SetCollision(lua_State* L)
		{
			LuauGameObject* ptr = Luau.lua_class_get<LuauGameObject>(L, 1, "GameObject");
			if (LuauGameObjectListReverse.TryGetValue((IntPtr)ptr, out var value))
			{
				Collider component = value.GetComponent<Collider>();
				if (component.IsNotNull())
				{
					component.enabled = Luau.lua_toboolean(L, 2) == 1;
				}
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int SetVisibility(lua_State* L)
		{
			LuauGameObject* ptr = Luau.lua_class_get<LuauGameObject>(L, 1, "GameObject");
			if (LuauGameObjectListReverse.TryGetValue((IntPtr)ptr, out var value))
			{
				MeshRenderer component = value.GetComponent<MeshRenderer>();
				if (component.IsNotNull())
				{
					component.enabled = Luau.lua_toboolean(L, 2) == 1;
				}
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int SetActive(lua_State* L)
		{
			LuauGameObject* ptr = Luau.lua_class_get<LuauGameObject>(L, 1, "GameObject");
			if (LuauGameObjectListReverse.TryGetValue((IntPtr)ptr, out var value))
			{
				value.SetActive(Luau.lua_toboolean(L, 2) == 1);
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int SetText(lua_State* L)
		{
			LuauGameObject* ptr = Luau.lua_class_get<LuauGameObject>(L, 1, "GameObject");
			if (LuauGameObjectListReverse.TryGetValue((IntPtr)ptr, out var value))
			{
				string text = new string(Luau.lua_tostring(L, 2));
				TextMeshPro component = value.GetComponent<TextMeshPro>();
				if (component.IsNotNull())
				{
					component.text = text;
				}
				else
				{
					TextMesh component2 = value.GetComponent<TextMesh>();
					if (component2.IsNotNull())
					{
						component2.text = text;
					}
				}
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int OnTouched(lua_State* L)
		{
			LuauGameObject* ptr = Luau.lua_class_get<LuauGameObject>(L, 1, "GameObject");
			if (LuauGameObjectListReverse.TryGetValue((IntPtr)ptr, out var value))
			{
				if (LuauTriggerCallbacks.TryGetValue(value, out var value2))
				{
					Luau.lua_unref(L, value2);
					LuauTriggerCallbacks.Remove(value);
				}
				if (Luau.lua_type(L, 2) == 7)
				{
					int value3 = Luau.lua_ref(L, 2);
					LuauTriggerCallbacks.TryAdd(value, value3);
				}
				else
				{
					FixedString32Bytes output = "Callback must be a function";
					Luau.luaL_errorL(L, (sbyte*)UnsafeUtility.AddressOf(ref output) + 2);
				}
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int SetVelocity(lua_State* L)
		{
			LuauGameObject* ptr = Luau.lua_class_get<LuauGameObject>(L, 1, "GameObject");
			if (LuauGameObjectListReverse.TryGetValue((IntPtr)ptr, out var value))
			{
				Vector3 linearVelocity = *Luau.lua_class_get<Vector3>(L, 2);
				Rigidbody component = value.GetComponent<Rigidbody>();
				if (component.IsNotNull())
				{
					component.linearVelocity = linearVelocity;
				}
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int GetVelocity(lua_State* L)
		{
			LuauGameObject* ptr = Luau.lua_class_get<LuauGameObject>(L, 1, "GameObject");
			if (LuauGameObjectListReverse.TryGetValue((IntPtr)ptr, out var value))
			{
				if (value.IsNull())
				{
					return 0;
				}
				Rigidbody component = value.GetComponent<Rigidbody>();
				Vector3* ptr2 = Luau.lua_class_push<Vector3>(L, "Vec3");
				if (component.IsNotNull())
				{
					*ptr2 = component.linearVelocity;
				}
				else
				{
					*ptr2 = Vector3.zero;
				}
			}
			return 1;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int SetColor(lua_State* L)
		{
			LuauGameObject* ptr = Luau.lua_class_get<LuauGameObject>(L, 1, "GameObject");
			Vector3 vector = *Luau.lua_class_get<Vector3>(L, 2);
			if (LuauGameObjectListReverse.TryGetValue((IntPtr)ptr, out var value))
			{
				Color color = new Color(Mathf.Clamp01(vector.x / 255f), Mathf.Clamp01(vector.y / 255f), Mathf.Clamp01(vector.z / 255f), 1f);
				TextMeshPro component = value.GetComponent<TextMeshPro>();
				if (component != null)
				{
					component.color = color;
					return 0;
				}
				TextMesh component2 = value.GetComponent<TextMesh>();
				if (component2 != null)
				{
					component2.color = color;
					return 0;
				}
				Renderer component3 = value.GetComponent<Renderer>();
				if (component3 != null)
				{
					component3.material.color = color;
				}
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Equals(lua_State* L)
		{
			LuauGameObject* ptr = Luau.lua_class_get<LuauGameObject>(L, 1, "GameObject");
			if (LuauGameObjectListReverse.TryGetValue((IntPtr)ptr, out var value))
			{
				LuauGameObject* ptr2 = Luau.lua_class_get<LuauGameObject>(L, 2, "GameObject");
				if (LuauGameObjectListReverse.TryGetValue((IntPtr)ptr2, out var value2) && value == value2)
				{
					Luau.lua_pushboolean(L, 1);
					return 1;
				}
			}
			Luau.lua_pushboolean(L, 0);
			return 1;
		}
	}

	[BurstCompile]
	public struct LuauPlayer
	{
		public int PlayerID;

		public FixedString32Bytes PlayerName;

		public int PlayerMaterial;

		[MarshalAs(UnmanagedType.U1)]
		public bool IsMasterClient;

		public Vector3 BodyPosition;

		public Vector3 Velocity;

		[MarshalAs(UnmanagedType.U1)]
		public bool IsPCVR;

		public Vector3 LeftHandPosition;

		public Vector3 RightHandPosition;

		[MarshalAs(UnmanagedType.U1)]
		public bool IsEntityAuthority;

		public Quaternion HeadRotation;

		public Quaternion LeftHandRotation;

		public Quaternion RightHandRotation;

		[MarshalAs(UnmanagedType.U1)]
		public bool IsInVStump;
	}

	[BurstCompile]
	public static class PlayerFunctions
	{
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int GetPlayerByID(lua_State* L)
		{
			int num = (int)Luau.luaL_checknumber(L, 1);
			foreach (NetPlayer item in RoomSystem.PlayersInRoom)
			{
				if (item.ActorNumber != num)
				{
					continue;
				}
				if (LuauPlayerList.TryGetValue(item.ActorNumber, out var value))
				{
					Luau.lua_class_push(L, "Player", value);
					continue;
				}
				LuauPlayer* ptr = Luau.lua_class_push<LuauPlayer>(L);
				ptr->PlayerID = item.ActorNumber;
				ptr->PlayerMaterial = 0;
				ptr->IsMasterClient = item.IsMasterClient;
				LuauPlayerList[item.ActorNumber] = (IntPtr)ptr;
				VRRig vRRig = GorillaGameManager.instance?.FindPlayerVRRig(item);
				if (vRRig != null)
				{
					ptr->PlayerName = vRRig.playerNameVisible;
					LuauVRRigList[item.ActorNumber] = vRRig;
					UpdatePlayer(L, vRRig, ptr);
					LuauPlayerList[item.ActorNumber] = (IntPtr)ptr;
				}
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static void UpdatePlayer(lua_State* L, VRRig p, LuauPlayer* data)
		{
			data->BodyPosition = p.transform.position;
			data->Velocity = p.LatestVelocity();
			data->LeftHandPosition = p.leftHandTransform.position;
			data->RightHandPosition = p.rightHandTransform.position;
			data->HeadRotation = p.head.rigTarget.rotation;
			data->LeftHandRotation = p.leftHandTransform.rotation;
			data->RightHandRotation = p.rightHandTransform.rotation;
			if (p.isLocal)
			{
				data->IsInVStump = CustomMapManager.IsLocalPlayerInVirtualStump();
			}
			else if (p.creator != null)
			{
				data->IsInVStump = CustomMapManager.IsRemotePlayerInVirtualStump(p.creator.UserId);
			}
			else
			{
				data->IsInVStump = false;
			}
			data->IsEntityAuthority = CustomMapsGameManager.instance.IsNotNull() && CustomMapsGameManager.instance.gameEntityManager.IsNotNull() && CustomMapsGameManager.instance.gameEntityManager.IsZoneAuthority();
		}
	}

	[BurstCompile]
	public struct LuauAIAgent
	{
		public int EntityID;

		public Vector3 EntityPosition;

		public Quaternion EntityRotation;
	}

	[BurstCompile]
	public struct LuauGrabbableEntity
	{
		public int EntityID;

		public Vector3 EntityPosition;

		public Quaternion EntityRotation;
	}

	[BurstCompile]
	public static class GrabbableEntityFunctions
	{
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int ToString(lua_State* L)
		{
			string s = "NULL";
			LuauGrabbableEntity* ptr = Luau.lua_class_get<LuauGrabbableEntity>(L, 1);
			if (ptr != null)
			{
				s = "ID: " + ptr->EntityID + " | Pos: " + ptr->EntityPosition.ToString() + " | Rot: " + ptr->EntityRotation.ToString();
			}
			Luau.lua_pushstring(L, s);
			return 1;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int GetGrabbableEntityByEntityID(lua_State* L)
		{
			int num = (int)Luau.luaL_checknumber(L, 1);
			Debug.Log($"[LuauBindings::GetGrabbableEntityByEntityID] ID: {num}");
			GameEntityManager gameEntityManager = CustomMapsGameManager.instance.gameEntityManager;
			if (gameEntityManager.IsNotNull())
			{
				GameEntityId entityIdFromNetId = gameEntityManager.GetEntityIdFromNetId(num);
				GameEntity gameEntity = gameEntityManager.GetGameEntity(entityIdFromNetId);
				if (gameEntity.IsNotNull())
				{
					if (gameEntity.gameObject.IsNull())
					{
						return 0;
					}
					Debug.Log("[LuauBindings::GetGrabbableEntityByEntityID] Found agent: " + gameEntity.gameObject.name);
					if (LuauGrabbablesList.TryGetValue(num, out var value))
					{
						UpdateEntity(gameEntity, (LuauGrabbableEntity*)(void*)value);
						Luau.lua_class_push(L, "GrabbableEntity", value);
					}
					else
					{
						LuauGrabbableEntity* ptr = Luau.lua_class_push<LuauGrabbableEntity>(L);
						UpdateEntity(gameEntity, ptr);
						LuauGrabbablesList[num] = (IntPtr)ptr;
					}
					return 1;
				}
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int GetHoldingActorNumberByLuauID(lua_State* L)
		{
			short num = (short)Luau.luaL_checknumber(L, 1);
			Debug.Log($"[LuauBindings::GetHoldingActorNumberByLuauID] ID: {num}");
			GameEntityManager gameEntityManager = CustomMapsGameManager.instance.gameEntityManager;
			if (gameEntityManager.IsNull())
			{
				return 0;
			}
			List<GameEntity> gameEntities = gameEntityManager.GetGameEntities();
			for (int i = 0; i < gameEntities.Count; i++)
			{
				if (gameEntities[i].gameObject.IsNull())
				{
					continue;
				}
				CustomMapsGrabbablesController component = gameEntities[i].gameObject.GetComponent<CustomMapsGrabbablesController>();
				if (!component.IsNull())
				{
					Debug.Log("[LuauBindings::GetHoldingActorNumberByLuauID] checking GrabbableController on " + $"{component.gameObject.name}, id: {component.luaAgentID}");
					if (component.luaAgentID == num)
					{
						Luau.lua_pushnumber(L, component.GetGrabbingActor());
						return 1;
					}
				}
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int GetHoldingActorNumberByEntityID(lua_State* L)
		{
			int num = (int)Luau.luaL_checknumber(L, 1);
			Debug.Log($"[LuauBindings::GetHoldingActorNumberByEntityID] ID: {num}");
			GameEntityManager gameEntityManager = CustomMapsGameManager.instance.gameEntityManager;
			if (gameEntityManager.IsNull())
			{
				return 0;
			}
			GameEntityId entityIdFromNetId = gameEntityManager.GetEntityIdFromNetId(num);
			GameEntity gameEntity = gameEntityManager.GetGameEntity(entityIdFromNetId);
			if (gameEntity.IsNotNull() || gameEntity.gameObject.IsNull())
			{
				return 0;
			}
			Debug.Log("[LuauBindings::GetHoldingActorNumberByEntityID] Found agent: " + gameEntity.gameObject.name);
			CustomMapsGrabbablesController component = gameEntity.gameObject.GetComponent<CustomMapsGrabbablesController>();
			if (component.IsNull())
			{
				return 0;
			}
			Luau.lua_pushnumber(L, component.GetGrabbingActor());
			return 1;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int FindPrePlacedGrabbableEntityByID(lua_State* L)
		{
			short num = (short)Luau.luaL_checknumber(L, 1);
			Debug.Log($"[LuauBindings::FindPrePlacedGrabbableEntityByID] ID: {num}");
			GameEntityManager gameEntityManager = CustomMapsGameManager.instance.gameEntityManager;
			if (gameEntityManager.IsNotNull())
			{
				List<GameEntity> gameEntities = gameEntityManager.GetGameEntities();
				for (int i = 0; i < gameEntities.Count; i++)
				{
					if (gameEntities[i].gameObject.IsNull())
					{
						continue;
					}
					CustomMapsGrabbablesController component = gameEntities[i].gameObject.GetComponent<CustomMapsGrabbablesController>();
					if (component.IsNull())
					{
						continue;
					}
					Debug.Log("[LuauBindings::FindPrePlacedGrabbableEntityByID] checking GrabbableController on " + $"{component.gameObject.name}, id: {component.luaAgentID}");
					if (component.luaAgentID == num)
					{
						if (LuauGrabbablesList.TryGetValue(gameEntities[i].GetNetId(), out var value))
						{
							UpdateEntity(gameEntities[i], (LuauGrabbableEntity*)(void*)value);
							Luau.lua_class_push(L, "GrabbableEntity", value);
						}
						else
						{
							LuauGrabbableEntity* ptr = Luau.lua_class_push<LuauGrabbableEntity>(L);
							UpdateEntity(gameEntities[i], ptr);
							LuauGrabbablesList[gameEntities[i].GetNetId()] = (IntPtr)ptr;
						}
						return 1;
					}
				}
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int SpawnGrabbableEntity(lua_State* L)
		{
			Debug.Log("[LuauBindings::SpawnGrabbableEntity]");
			CustomMapsGameManager instance = CustomMapsGameManager.instance;
			GameEntityManager gameEntityManager = (instance.IsNotNull() ? instance.gameEntityManager : null);
			if (gameEntityManager.IsNull())
			{
				LuauHud.Instance.LuauLog("SpawnGrabbableEntity failed. EntityManager is null.");
				return 0;
			}
			if (!gameEntityManager.IsZoneAuthority())
			{
				LuauHud.Instance.LuauLog("SpawnGrabbableEntity failed. Local Player doesn't have Entity Authority.");
				return 0;
			}
			if (LuauAIAgentList.Count + LuauGrabbablesList.Count == GT_CustomMapSupportRuntime.Constants.aiAgentLimit)
			{
				LuauHud.Instance.LuauLog($"SpawnGrabbableEntity failed, EntityLimit of {GT_CustomMapSupportRuntime.Constants.aiAgentLimit}" + " has already been reached.");
				return 0;
			}
			int enemyTypeId = (int)Luau.luaL_checknumber(L, 1);
			Vector3 position = *Luau.lua_class_get<Vector3>(L, 2, "Vec3");
			Quaternion rotation = *Luau.lua_class_get<Quaternion>(L, 3, "Quat");
			GameEntityId id = instance.SpawnGrabbableAtLocation(enemyTypeId, position, rotation);
			Debug.Log("[LuauBindings::SpawnGrabbableEntity] spawnedGrabbable");
			if (id.IsValid())
			{
				Debug.Log("[LuauBindings::SpawnGrabbableEntity] spawnedGrabbable ID valid");
				GameEntity gameEntity = gameEntityManager.GetGameEntity(id);
				if (LuauGrabbablesList.TryGetValue(gameEntity.GetNetId(), out var value))
				{
					Debug.Log("[LuauBindings::SpawnGrabbableEntity] fround grabbable");
					UpdateEntity(gameEntity, (LuauGrabbableEntity*)(void*)value);
					Luau.lua_class_push(L, "GrabbableEntity", value);
					return 1;
				}
				Debug.Log("[LuauBindings::SpawnGrabbableEntity] grabbable not found");
				Luau.lua_getglobal(L, "GrabbableEntities");
				LuauGrabbableEntity* ptr = Luau.lua_class_push<LuauGrabbableEntity>(L);
				UpdateEntity(gameEntity, ptr);
				LuauGrabbablesList[gameEntity.GetNetId()] = (IntPtr)ptr;
				Debug.Log("[LuauBindings::SpawnGrabbableEntity] created new grabbable");
				Luau.lua_rawseti(L, -2, LuauGrabbablesList.Count);
				Luau.lua_pop(L, 1);
				Debug.Log("[LuauBindings::SpawnGrabbableEntity] pushing new grabbable");
				Luau.lua_class_push(L, "GrabbableEntity", (IntPtr)ptr);
				return 1;
			}
			LuauHud.Instance.LuauLog("SpawnGrabbableEntity failed to create entity.");
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static void UpdateEntity(GameEntity entity, LuauGrabbableEntity* luaAgent)
		{
			luaAgent->EntityID = entity.GetNetId();
			luaAgent->EntityPosition = entity.transform.position;
			luaAgent->EntityRotation = entity.transform.rotation;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int DestroyEntity(lua_State* L)
		{
			LuauGrabbableEntity* ptr = Luau.lua_class_get<LuauGrabbableEntity>(L, 1);
			if (ptr != null)
			{
				GameEntityManager entityManager = CustomMapsGameManager.GetEntityManager();
				if (entityManager.IsNotNull())
				{
					GameEntityId entityIdFromNetId = entityManager.GetEntityIdFromNetId(ptr->EntityID);
					entityManager.RequestDestroyItem(entityIdFromNetId);
				}
			}
			return 0;
		}
	}

	[BurstCompile]
	public static class AIAgentFunctions
	{
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int ToString(lua_State* L)
		{
			string s = "NULL";
			LuauAIAgent* ptr = Luau.lua_class_get<LuauAIAgent>(L, 1);
			if (ptr != null)
			{
				s = "ID: " + ptr->EntityID + " | Pos: " + ptr->EntityPosition.ToString() + " | Rot: " + ptr->EntityRotation.ToString();
			}
			Luau.lua_pushstring(L, s);
			return 1;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int GetAIAgentByEntityID(lua_State* L)
		{
			int num = (int)Luau.luaL_checknumber(L, 1);
			Debug.Log($"[LuauBindings::GetAIAgentByEntityID] ID: {num}");
			GameEntityManager gameEntityManager = CustomMapsGameManager.instance.gameEntityManager;
			if (gameEntityManager.IsNotNull())
			{
				GameEntityId entityIdFromNetId = gameEntityManager.GetEntityIdFromNetId(num);
				GameEntity gameEntity = gameEntityManager.GetGameEntity(entityIdFromNetId);
				if (gameEntity.IsNotNull())
				{
					if (gameEntity.gameObject.IsNull())
					{
						return 0;
					}
					if (gameEntity.gameObject.GetComponent<GameAgent>().IsNotNull())
					{
						Debug.Log("[LuauBindings::GetAIAgentByEntityID] Found agent: " + gameEntity.gameObject.name);
						if (LuauAIAgentList.TryGetValue(num, out var value))
						{
							UpdateEntity(gameEntity, (LuauAIAgent*)(void*)value);
							Luau.lua_class_push(L, "AIAgent", value);
						}
						else
						{
							LuauAIAgent* ptr = Luau.lua_class_push<LuauAIAgent>(L);
							UpdateEntity(gameEntity, ptr);
							LuauAIAgentList[num] = (IntPtr)ptr;
						}
					}
					return 1;
				}
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int FindPrePlacedAIAgentByID(lua_State* L)
		{
			short num = (short)Luau.luaL_checknumber(L, 1);
			GameAgentManager gameAgentManager = CustomMapsGameManager.instance.gameAgentManager;
			if (gameAgentManager.IsNotNull())
			{
				List<GameAgent> agents = gameAgentManager.GetAgents();
				for (int i = 0; i < agents.Count; i++)
				{
					if (agents[i].gameObject.IsNull())
					{
						continue;
					}
					CustomMapsAIBehaviourController component = agents[i].gameObject.GetComponent<CustomMapsAIBehaviourController>();
					if (!component.IsNull() && component.luaAgentID == num)
					{
						if (LuauAIAgentList.TryGetValue(agents[i].entity.GetNetId(), out var value))
						{
							UpdateEntity(agents[i].entity, (LuauAIAgent*)(void*)value);
							Luau.lua_class_push(L, "AIAgent", value);
						}
						else
						{
							LuauAIAgent* ptr = Luau.lua_class_push<LuauAIAgent>(L);
							UpdateEntity(agents[i].entity, ptr);
							LuauAIAgentList[agents[i].entity.GetNetId()] = (IntPtr)ptr;
						}
						return 1;
					}
				}
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int SpawnAIAgent(lua_State* L)
		{
			CustomMapsGameManager instance = CustomMapsGameManager.instance;
			GameEntityManager gameEntityManager = (instance.IsNotNull() ? instance.gameEntityManager : null);
			if (gameEntityManager.IsNull())
			{
				LuauHud.Instance.LuauLog("SpawnAIAgent failed. EntityManager is null.");
				return 0;
			}
			if (!gameEntityManager.IsZoneAuthority())
			{
				LuauHud.Instance.LuauLog("SpawnAIAgent failed. Local Player doesn't have Entity Authority.");
				return 0;
			}
			if (LuauAIAgentList.Count + LuauGrabbablesList.Count == GT_CustomMapSupportRuntime.Constants.aiAgentLimit)
			{
				LuauHud.Instance.LuauLog($"SpawnAIAgent failed, AIAgentLimit of {GT_CustomMapSupportRuntime.Constants.aiAgentLimit}" + " has already been reached.");
				return 0;
			}
			int enemyTypeId = (int)Luau.luaL_checknumber(L, 1);
			Vector3 position = *Luau.lua_class_get<Vector3>(L, 2, "Vec3");
			Quaternion rotation = *Luau.lua_class_get<Quaternion>(L, 3, "Quat");
			GameEntityId id = instance.SpawnEnemyAtLocation(enemyTypeId, position, rotation);
			if (id.IsValid())
			{
				GameEntity gameEntity = gameEntityManager.GetGameEntity(id);
				if ((gameEntity.IsNotNull() ? gameEntity.gameObject.GetComponent<GameAgent>() : null).IsNotNull())
				{
					if (LuauAIAgentList.TryGetValue(gameEntity.GetNetId(), out var value))
					{
						UpdateEntity(gameEntity, (LuauAIAgent*)(void*)value);
						Luau.lua_class_push(L, "AIAgent", value);
						return 1;
					}
					Luau.lua_getglobal(L, "AIAgents");
					LuauAIAgent* ptr = Luau.lua_class_push<LuauAIAgent>(L);
					UpdateEntity(gameEntity, ptr);
					LuauAIAgentList[gameEntity.GetNetId()] = (IntPtr)ptr;
					Luau.lua_rawseti(L, -2, LuauAIAgentList.Count);
					Luau.lua_pop(L, 1);
					Luau.lua_class_push(L, "AIAgent", (IntPtr)ptr);
					return 1;
				}
			}
			LuauHud.Instance.LuauLog("SpawnAIAgent failed to create entity.");
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int SetDestination(lua_State* L)
		{
			LuauAIAgent* ptr = Luau.lua_class_get<LuauAIAgent>(L, 1);
			Vector3* ptr2 = Luau.lua_class_get<Vector3>(L, 2);
			GameEntityManager gameEntityManager = CustomMapsGameManager.instance.gameEntityManager;
			if (gameEntityManager.IsNotNull())
			{
				CustomMapsAIBehaviourController component = gameEntityManager.GetGameEntity(gameEntityManager.GetEntityIdFromNetId(ptr->EntityID)).gameObject.GetComponent<CustomMapsAIBehaviourController>();
				if (component.IsNotNull())
				{
					component.RequestDestination(*ptr2);
				}
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int PlayAgentAnimation(lua_State* L)
		{
			LuauAIAgent* ptr = Luau.lua_class_get<LuauAIAgent>(L, 1);
			string stateName = Marshal.PtrToStringAnsi((IntPtr)Luau.luaL_checkstring(L, 2));
			if (ptr != null)
			{
				GameEntityManager entityManager = CustomMapsGameManager.GetEntityManager();
				if (entityManager.IsNotNull())
				{
					CustomMapsAIBehaviourController behaviorControllerForEntity = CustomMapsGameManager.GetBehaviorControllerForEntity(entityManager.GetEntityIdFromNetId(ptr->EntityID));
					if (behaviorControllerForEntity.IsNotNull())
					{
						behaviorControllerForEntity.PlayAnimation(stateName);
					}
				}
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int SetTarget(lua_State* L)
		{
			LuauAIAgent* ptr = Luau.lua_class_get<LuauAIAgent>(L, 1);
			if (ptr == null)
			{
				return 0;
			}
			GameEntityManager entityManager = CustomMapsGameManager.GetEntityManager();
			if (entityManager.IsNull() || !entityManager.IsAuthority())
			{
				return 0;
			}
			int num = (int)Luau.luaL_checknumber(L, 2);
			if (!VRRigCache.Instance.TryGetVrrig(num, out var playerRig))
			{
				num = -1;
			}
			CustomMapsAIBehaviourController behaviorControllerForEntity = CustomMapsGameManager.GetBehaviorControllerForEntity(entityManager.GetEntityIdFromNetId(ptr->EntityID));
			if (behaviorControllerForEntity.IsNull())
			{
				return 0;
			}
			if (num == -1)
			{
				behaviorControllerForEntity.ClearTarget();
			}
			else
			{
				GRPlayer component = playerRig.Rig.GetComponent<GRPlayer>();
				behaviorControllerForEntity.SetTarget(component);
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int GetTarget(lua_State* L)
		{
			LuauAIAgent* ptr = Luau.lua_class_get<LuauAIAgent>(L, 1);
			if (ptr != null)
			{
				GameEntityManager entityManager = CustomMapsGameManager.GetEntityManager();
				if (entityManager.IsNotNull() && entityManager.IsAuthority())
				{
					CustomMapsAIBehaviourController behaviorControllerForEntity = CustomMapsGameManager.GetBehaviorControllerForEntity(entityManager.GetEntityIdFromNetId(ptr->EntityID));
					if (behaviorControllerForEntity.IsNotNull() && behaviorControllerForEntity.TargetPlayer.IsNotNull() && behaviorControllerForEntity.TargetPlayer.MyRig.IsNotNull() && !behaviorControllerForEntity.TargetPlayer.MyRig.OwningNetPlayer.IsNull)
					{
						Luau.lua_pushnumber(L, behaviorControllerForEntity.TargetPlayer.MyRig.OwningNetPlayer.ActorNumber);
						return 1;
					}
				}
			}
			Luau.lua_pushnumber(L, -1.0);
			return 1;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static void UpdateEntity(GameEntity entity, LuauAIAgent* luaAgent)
		{
			luaAgent->EntityID = entity.GetNetId();
			luaAgent->EntityPosition = entity.transform.position;
			luaAgent->EntityRotation = entity.transform.rotation;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int DestroyEntity(lua_State* L)
		{
			LuauAIAgent* ptr = Luau.lua_class_get<LuauAIAgent>(L, 1);
			if (ptr != null)
			{
				GameEntityManager entityManager = CustomMapsGameManager.GetEntityManager();
				if (entityManager.IsNotNull())
				{
					GameEntityId entityIdFromNetId = entityManager.GetEntityIdFromNetId(ptr->EntityID);
					entityManager.RequestDestroyItem(entityIdFromNetId);
				}
			}
			return 0;
		}
	}

	[BurstCompile]
	public static class Vec3Functions
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int New_00004D10_0024PostfixBurstDelegate(lua_State* L);

		internal static class New_00004D10_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<New_00004D10_0024PostfixBurstDelegate>(New).Value;
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
				return New_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int Add_00004D11_0024PostfixBurstDelegate(lua_State* L);

		internal static class Add_00004D11_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<Add_00004D11_0024PostfixBurstDelegate>(Add).Value;
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
				return Add_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int Sub_00004D12_0024PostfixBurstDelegate(lua_State* L);

		internal static class Sub_00004D12_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<Sub_00004D12_0024PostfixBurstDelegate>(Sub).Value;
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
				return Sub_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int Mul_00004D13_0024PostfixBurstDelegate(lua_State* L);

		internal static class Mul_00004D13_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<Mul_00004D13_0024PostfixBurstDelegate>(Mul).Value;
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
				return Mul_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int Div_00004D14_0024PostfixBurstDelegate(lua_State* L);

		internal static class Div_00004D14_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<Div_00004D14_0024PostfixBurstDelegate>(Div).Value;
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
				return Div_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int Unm_00004D15_0024PostfixBurstDelegate(lua_State* L);

		internal static class Unm_00004D15_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<Unm_00004D15_0024PostfixBurstDelegate>(Unm).Value;
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
				return Unm_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int Eq_00004D16_0024PostfixBurstDelegate(lua_State* L);

		internal static class Eq_00004D16_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<Eq_00004D16_0024PostfixBurstDelegate>(Eq).Value;
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
				return Eq_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int Dot_00004D18_0024PostfixBurstDelegate(lua_State* L);

		internal static class Dot_00004D18_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<Dot_00004D18_0024PostfixBurstDelegate>(Dot).Value;
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
				return Dot_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int Cross_00004D19_0024PostfixBurstDelegate(lua_State* L);

		internal static class Cross_00004D19_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<Cross_00004D19_0024PostfixBurstDelegate>(Cross).Value;
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
				return Cross_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int Project_00004D1A_0024PostfixBurstDelegate(lua_State* L);

		internal static class Project_00004D1A_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<Project_00004D1A_0024PostfixBurstDelegate>(Project).Value;
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
				return Project_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int Length_00004D1B_0024PostfixBurstDelegate(lua_State* L);

		internal static class Length_00004D1B_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<Length_00004D1B_0024PostfixBurstDelegate>(Length).Value;
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
				return Length_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int Normalize_00004D1C_0024PostfixBurstDelegate(lua_State* L);

		internal static class Normalize_00004D1C_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<Normalize_00004D1C_0024PostfixBurstDelegate>(Normalize).Value;
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
				return Normalize_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int SafeNormal_00004D1D_0024PostfixBurstDelegate(lua_State* L);

		internal static class SafeNormal_00004D1D_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<SafeNormal_00004D1D_0024PostfixBurstDelegate>(SafeNormal).Value;
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
				return SafeNormal_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int Distance_00004D1E_0024PostfixBurstDelegate(lua_State* L);

		internal static class Distance_00004D1E_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<Distance_00004D1E_0024PostfixBurstDelegate>(Distance).Value;
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
				return Distance_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int Lerp_00004D1F_0024PostfixBurstDelegate(lua_State* L);

		internal static class Lerp_00004D1F_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<Lerp_00004D1F_0024PostfixBurstDelegate>(Lerp).Value;
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
				return Lerp_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int Rotate_00004D20_0024PostfixBurstDelegate(lua_State* L);

		internal static class Rotate_00004D20_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<Rotate_00004D20_0024PostfixBurstDelegate>(Rotate).Value;
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
				return Rotate_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int ZeroVector_00004D21_0024PostfixBurstDelegate(lua_State* L);

		internal static class ZeroVector_00004D21_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<ZeroVector_00004D21_0024PostfixBurstDelegate>(ZeroVector).Value;
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
				return ZeroVector_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int OneVector_00004D22_0024PostfixBurstDelegate(lua_State* L);

		internal static class OneVector_00004D22_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<OneVector_00004D22_0024PostfixBurstDelegate>(OneVector).Value;
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
				return OneVector_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int NearlyEqual_00004D23_0024PostfixBurstDelegate(lua_State* L);

		internal static class NearlyEqual_00004D23_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<NearlyEqual_00004D23_0024PostfixBurstDelegate>(NearlyEqual).Value;
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
				return NearlyEqual_0024BurstManaged(L);
			}
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int New(lua_State* L)
		{
			return New_00004D10_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Add(lua_State* L)
		{
			return Add_00004D11_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Sub(lua_State* L)
		{
			return Sub_00004D12_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Mul(lua_State* L)
		{
			return Mul_00004D13_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Div(lua_State* L)
		{
			return Div_00004D14_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Unm(lua_State* L)
		{
			return Unm_00004D15_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Eq(lua_State* L)
		{
			return Eq_00004D16_0024BurstDirectCall.Invoke(L);
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int ToString(lua_State* L)
		{
			Vector3 vector = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			Luau.lua_pushstring(L, vector.ToString());
			return 1;
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Dot(lua_State* L)
		{
			return Dot_00004D18_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Cross(lua_State* L)
		{
			return Cross_00004D19_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Project(lua_State* L)
		{
			return Project_00004D1A_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Length(lua_State* L)
		{
			return Length_00004D1B_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Normalize(lua_State* L)
		{
			return Normalize_00004D1C_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int SafeNormal(lua_State* L)
		{
			return SafeNormal_00004D1D_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Distance(lua_State* L)
		{
			return Distance_00004D1E_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Lerp(lua_State* L)
		{
			return Lerp_00004D1F_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Rotate(lua_State* L)
		{
			return Rotate_00004D20_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int ZeroVector(lua_State* L)
		{
			return ZeroVector_00004D21_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int OneVector(lua_State* L)
		{
			return OneVector_00004D22_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int NearlyEqual(lua_State* L)
		{
			return NearlyEqual_00004D23_0024BurstDirectCall.Invoke(L);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int New_0024BurstManaged(lua_State* L)
		{
			Vector3* intPtr = Luau.lua_class_push<Vector3>(L, "Vec3");
			intPtr->x = (float)Luau.luaL_optnumber(L, 1, 0.0);
			intPtr->y = (float)Luau.luaL_optnumber(L, 2, 0.0);
			intPtr->z = (float)Luau.luaL_optnumber(L, 3, 0.0);
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int Add_0024BurstManaged(lua_State* L)
		{
			Vector3 vector = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			Vector3 vector2 = *Luau.lua_class_get<Vector3>(L, 2, "Vec3");
			*Luau.lua_class_push<Vector3>(L, "Vec3") = vector + vector2;
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int Sub_0024BurstManaged(lua_State* L)
		{
			Vector3 vector = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			Vector3 vector2 = *Luau.lua_class_get<Vector3>(L, 2, "Vec3");
			*Luau.lua_class_push<Vector3>(L, "Vec3") = vector - vector2;
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int Mul_0024BurstManaged(lua_State* L)
		{
			Vector3 vector = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			float num = (float)Luau.luaL_checknumber(L, 2);
			*Luau.lua_class_push<Vector3>(L, "Vec3") = vector * num;
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int Div_0024BurstManaged(lua_State* L)
		{
			Vector3 vector = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			float num = (float)Luau.luaL_checknumber(L, 2);
			*Luau.lua_class_push<Vector3>(L, "Vec3") = vector / num;
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int Unm_0024BurstManaged(lua_State* L)
		{
			Vector3 vector = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			*Luau.lua_class_push<Vector3>(L, "Vec3") = -vector;
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int Eq_0024BurstManaged(lua_State* L)
		{
			Vector3 vector = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			Vector3 vector2 = *Luau.lua_class_get<Vector3>(L, 2, "Vec3");
			int num = ((vector == vector2) ? 1 : 0);
			Luau.lua_pushnumber(L, num);
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int Dot_0024BurstManaged(lua_State* L)
		{
			Vector3 lhs = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			Vector3 rhs = *Luau.lua_class_get<Vector3>(L, 2, "Vec3");
			double n = Vector3.Dot(lhs, rhs);
			Luau.lua_pushnumber(L, n);
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int Cross_0024BurstManaged(lua_State* L)
		{
			Vector3 lhs = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			Vector3 rhs = *Luau.lua_class_get<Vector3>(L, 2, "Vec3");
			*Luau.lua_class_push<Vector3>(L, "Vec3") = Vector3.Cross(lhs, rhs);
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int Project_0024BurstManaged(lua_State* L)
		{
			Vector3 vector = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			Vector3 onNormal = *Luau.lua_class_get<Vector3>(L, 2, "Vec3");
			*Luau.lua_class_push<Vector3>(L, "Vec3") = Vector3.Project(vector, onNormal);
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int Length_0024BurstManaged(lua_State* L)
		{
			Vector3 vector = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			Luau.lua_pushnumber(L, Vector3.Magnitude(vector));
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int Normalize_0024BurstManaged(lua_State* L)
		{
			Luau.lua_class_get<Vector3>(L, 1, "Vec3")->Normalize();
			return 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int SafeNormal_0024BurstManaged(lua_State* L)
		{
			Vector3 vector = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			*Luau.lua_class_push<Vector3>(L, "Vec3") = vector.normalized;
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int Distance_0024BurstManaged(lua_State* L)
		{
			Vector3 a = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			Vector3 b = *Luau.lua_class_get<Vector3>(L, 2, "Vec3");
			Luau.lua_pushnumber(L, Vector3.Distance(a, b));
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int Lerp_0024BurstManaged(lua_State* L)
		{
			Vector3 a = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			Vector3 b = *Luau.lua_class_get<Vector3>(L, 2, "Vec3");
			double num = Luau.luaL_checknumber(L, 3);
			*Luau.lua_class_push<Vector3>(L, "Vec3") = Vector3.Lerp(a, b, (float)num);
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int Rotate_0024BurstManaged(lua_State* L)
		{
			Vector3 vector = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			Quaternion quaternion = *Luau.lua_class_get<Quaternion>(L, 2, "Quat");
			*Luau.lua_class_push<Vector3>(L, "Vec3") = quaternion * vector;
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int ZeroVector_0024BurstManaged(lua_State* L)
		{
			Vector3* intPtr = Luau.lua_class_push<Vector3>(L, "Vec3");
			intPtr->x = 0f;
			intPtr->y = 0f;
			intPtr->z = 0f;
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int OneVector_0024BurstManaged(lua_State* L)
		{
			Vector3* intPtr = Luau.lua_class_push<Vector3>(L, "Vec3");
			intPtr->x = 1f;
			intPtr->y = 1f;
			intPtr->z = 1f;
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int NearlyEqual_0024BurstManaged(lua_State* L)
		{
			Vector3 vector = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			Vector3 vector2 = *Luau.lua_class_get<Vector3>(L, 2, "Vec3");
			float num = (float)Luau.luaL_optnumber(L, 3, 0.0001);
			bool flag = !(Math.Abs(vector.x - vector2.x) > num);
			if (flag && Math.Abs(vector.y - vector2.y) > num)
			{
				flag = false;
			}
			if (flag && Math.Abs(vector.z - vector2.z) > num)
			{
				flag = false;
			}
			Luau.lua_pushboolean(L, flag ? 1 : 0);
			return 1;
		}
	}

	[BurstCompile]
	public static class QuatFunctions
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int New_00004D24_0024PostfixBurstDelegate(lua_State* L);

		internal static class New_00004D24_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<New_00004D24_0024PostfixBurstDelegate>(New).Value;
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
				return New_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int Mul_00004D25_0024PostfixBurstDelegate(lua_State* L);

		internal static class Mul_00004D25_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<Mul_00004D25_0024PostfixBurstDelegate>(Mul).Value;
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
				return Mul_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int Eq_00004D26_0024PostfixBurstDelegate(lua_State* L);

		internal static class Eq_00004D26_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<Eq_00004D26_0024PostfixBurstDelegate>(Eq).Value;
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
				return Eq_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int FromEuler_00004D28_0024PostfixBurstDelegate(lua_State* L);

		internal static class FromEuler_00004D28_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<FromEuler_00004D28_0024PostfixBurstDelegate>(FromEuler).Value;
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
				return FromEuler_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int FromDirection_00004D29_0024PostfixBurstDelegate(lua_State* L);

		internal static class FromDirection_00004D29_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<FromDirection_00004D29_0024PostfixBurstDelegate>(FromDirection).Value;
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
				return FromDirection_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int GetUpVector_00004D2A_0024PostfixBurstDelegate(lua_State* L);

		internal static class GetUpVector_00004D2A_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<GetUpVector_00004D2A_0024PostfixBurstDelegate>(GetUpVector).Value;
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
				return GetUpVector_0024BurstManaged(L);
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate int Euler_00004D2B_0024PostfixBurstDelegate(lua_State* L);

		internal static class Euler_00004D2B_0024BurstDirectCall
		{
			private static IntPtr Pointer;

			[BurstDiscard]
			private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
			{
				if (Pointer == (IntPtr)0)
				{
					Pointer = BurstCompiler.CompileFunctionPointer<Euler_00004D2B_0024PostfixBurstDelegate>(Euler).Value;
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
				return Euler_0024BurstManaged(L);
			}
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int New(lua_State* L)
		{
			return New_00004D24_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Mul(lua_State* L)
		{
			return Mul_00004D25_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Eq(lua_State* L)
		{
			return Eq_00004D26_0024BurstDirectCall.Invoke(L);
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int ToString(lua_State* L)
		{
			Quaternion quaternion = *Luau.lua_class_get<Quaternion>(L, 1, "Quat");
			Luau.lua_pushstring(L, quaternion.ToString());
			return 1;
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int FromEuler(lua_State* L)
		{
			return FromEuler_00004D28_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int FromDirection(lua_State* L)
		{
			return FromDirection_00004D29_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int GetUpVector(lua_State* L)
		{
			return GetUpVector_00004D2A_0024BurstDirectCall.Invoke(L);
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int Euler(lua_State* L)
		{
			return Euler_00004D2B_0024BurstDirectCall.Invoke(L);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int New_0024BurstManaged(lua_State* L)
		{
			Quaternion* intPtr = Luau.lua_class_push<Quaternion>(L, "Quat");
			intPtr->x = (float)Luau.luaL_optnumber(L, 1, 0.0);
			intPtr->y = (float)Luau.luaL_optnumber(L, 2, 0.0);
			intPtr->z = (float)Luau.luaL_optnumber(L, 3, 0.0);
			intPtr->w = (float)Luau.luaL_optnumber(L, 4, 0.0);
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int Mul_0024BurstManaged(lua_State* L)
		{
			Quaternion quaternion = *Luau.lua_class_get<Quaternion>(L, 1, "Quat");
			Quaternion quaternion2 = *Luau.lua_class_get<Quaternion>(L, 2, "Quat");
			*Luau.lua_class_push<Quaternion>(L, "Quat") = quaternion * quaternion2;
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int Eq_0024BurstManaged(lua_State* L)
		{
			Quaternion quaternion = *Luau.lua_class_get<Quaternion>(L, 1, "Quat");
			Quaternion quaternion2 = *Luau.lua_class_get<Quaternion>(L, 2, "Quat");
			int num = ((quaternion == quaternion2) ? 1 : 0);
			Luau.lua_pushnumber(L, num);
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int FromEuler_0024BurstManaged(lua_State* L)
		{
			float x = (float)Luau.luaL_optnumber(L, 1, 0.0);
			float y = (float)Luau.luaL_optnumber(L, 2, 0.0);
			float z = (float)Luau.luaL_optnumber(L, 3, 0.0);
			Luau.lua_class_push<Quaternion>(L, "Quat")->eulerAngles = new Vector3(x, y, z);
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int FromDirection_0024BurstManaged(lua_State* L)
		{
			Vector3 lookRotation = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			Luau.lua_class_push<Quaternion>(L, "Quat")->SetLookRotation(lookRotation);
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int GetUpVector_0024BurstManaged(lua_State* L)
		{
			Quaternion quaternion = *Luau.lua_class_get<Quaternion>(L, 1, "Quat");
			*Luau.lua_class_push<Vector3>(L, "Vec3") = quaternion * Vector3.up;
			return 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[BurstCompile]
		internal unsafe static int Euler_0024BurstManaged(lua_State* L)
		{
			Quaternion quaternion = *Luau.lua_class_get<Quaternion>(L, 1, "Quat");
			*Luau.lua_class_push<Vector3>(L, "Vec3") = quaternion.eulerAngles;
			return 1;
		}
	}

	public struct GorillaLocomotionSettings
	{
		public float velocityLimit;

		public float slideVelocityLimit;

		public float maxJumpSpeed;

		public float jumpMultiplier;
	}

	[BurstCompile]
	public struct PlayerInput
	{
		public float leftXAxis;

		[MarshalAs(UnmanagedType.U1)]
		public bool leftPrimaryButton;

		public float rightXAxis;

		[MarshalAs(UnmanagedType.U1)]
		public bool rightPrimaryButton;

		public float leftYAxis;

		[MarshalAs(UnmanagedType.U1)]
		public bool leftSecondaryButton;

		public float rightYAxis;

		[MarshalAs(UnmanagedType.U1)]
		public bool rightSecondaryButton;

		public float leftTrigger;

		public float rightTrigger;

		public float leftGrip;

		public float rightGrip;
	}

	public static class JSON
	{
		private static string ModIODirectory = ((Application.platform == RuntimePlatform.WindowsPlayer) ? Path.Join(Application.persistentDataPath, "luauDataSaves") : Path.Join(Path.Join(Application.persistentDataPath, "mod.io", "06657"), "data"));

		public unsafe static Dictionary<object, object> ConsumeTable(lua_State* L, int tableIndex)
		{
			Dictionary<object, object> dictionary = new Dictionary<object, object>();
			Luau.lua_pushnil(L);
			if (tableIndex < 0)
			{
				tableIndex--;
			}
			while (Luau.lua_next(L, tableIndex) != 0)
			{
				Luau.lua_Types lua_Types = (Luau.lua_Types)Luau.lua_type(L, -1);
				Luau.lua_Types lua_Types2 = (Luau.lua_Types)Luau.lua_type(L, -2);
				object obj = null;
				switch (lua_Types2)
				{
				case Luau.lua_Types.LUA_TSTRING:
					obj = new string(Luau.lua_tostring(L, -2));
					break;
				case Luau.lua_Types.LUA_TNUMBER:
					obj = Luau.lua_tonumber(L, -2);
					break;
				default:
				{
					FixedString64Bytes output = "Invalid key in table, key must be a string or a number";
					Luau.luaL_errorL(L, (sbyte*)UnsafeUtility.AddressOf(ref output) + 2);
					return null;
				}
				}
				switch (lua_Types)
				{
				case Luau.lua_Types.LUA_TNUMBER:
					dictionary.Add(obj, Luau.luaL_checknumber(L, -1));
					Luau.lua_pop(L, 1);
					break;
				case Luau.lua_Types.LUA_TBOOLEAN:
					dictionary.Add(obj, Luau.lua_toboolean(L, -1) == 1);
					Luau.lua_pop(L, 1);
					break;
				case Luau.lua_Types.LUA_TSTRING:
					dictionary.Add(obj, new string(Luau.lua_tostring(L, -1)));
					Luau.lua_pop(L, 1);
					break;
				case Luau.lua_Types.LUA_TTABLE:
				case Luau.lua_Types.LUA_TUSERDATA:
					if (Luau.luaL_getmetafield(L, -1, "metahash") == 1)
					{
						if (!BurstClassInfo.ClassList.InfoFields.Data.TryGetValue((int)Luau.luaL_checknumber(L, -1), out var item))
						{
							FixedString64Bytes output3 = "\"Internal Class Info Error No Metatable Found\"";
							Luau.luaL_errorL(L, (sbyte*)UnsafeUtility.AddressOf(ref output3) + 2);
							return null;
						}
						Luau.lua_pop(L, 1);
						if (item.Name == (FixedString32Bytes)"Vec3")
						{
							dictionary.Add(obj, *Luau.lua_class_get<Vector3>(L, -1));
							Luau.lua_pop(L, 1);
							break;
						}
						if (!(item.Name == (FixedString32Bytes)"Quat"))
						{
							FixedString32Bytes output4 = "Invalid type in table";
							Luau.luaL_errorL(L, (sbyte*)UnsafeUtility.AddressOf(ref output4) + 2);
							return null;
						}
						dictionary.Add(obj, *Luau.lua_class_get<Quaternion>(L, -1));
						Luau.lua_pop(L, 1);
					}
					else
					{
						object obj2 = ConsumeTable(L, -1);
						Luau.lua_pop(L, 1);
						if (obj2 == null)
						{
							return null;
						}
						dictionary.Add(obj, obj2);
					}
					break;
				default:
				{
					FixedString32Bytes output2 = "Unknown type in table";
					Luau.luaL_errorL(L, (sbyte*)UnsafeUtility.AddressOf(ref output2) + 2);
					return null;
				}
				}
			}
			return dictionary;
		}

		private static int ParseStrictInt(string input)
		{
			if (string.IsNullOrEmpty(input) || input != input.Trim())
			{
				return -1;
			}
			if (!int.TryParse(input, out var result))
			{
				return -1;
			}
			return result;
		}

		private static bool CompareKeys(JObject obj, HashSet<string> set)
		{
			HashSet<string> hashSet = new HashSet<string>(from p in obj.Properties()
				select p.Name);
			return set.SetEquals(hashSet);
		}

		public unsafe static bool PushTable(lua_State* L, JObject table)
		{
			Luau.lua_createtable(L, 0, 0);
			foreach (KeyValuePair<string, JToken> item in table)
			{
				if (item.Key == null || item.Value == null)
				{
					continue;
				}
				int num = ParseStrictInt(item.Key);
				if (num == -1)
				{
					Luau.lua_pushstring(L, item.Key);
				}
				if (item.Value is JObject)
				{
					if (CompareKeys((JObject)item.Value, new HashSet<string> { "x", "y", "z" }))
					{
						JObject obj = item.Value as JObject;
						float x = obj["x"].ToObject<float>();
						float y = obj["y"].ToObject<float>();
						float z = obj["z"].ToObject<float>();
						Vector3 vector = new Vector3(x, y, z);
						*Luau.lua_class_push<Vector3>(L) = vector;
					}
					else if (CompareKeys((JObject)item.Value, new HashSet<string> { "x", "y", "z", "w" }))
					{
						JObject obj2 = item.Value as JObject;
						float x2 = obj2["x"].ToObject<float>();
						float y2 = obj2["y"].ToObject<float>();
						float z2 = obj2["z"].ToObject<float>();
						float w = obj2["w"].ToObject<float>();
						Quaternion quaternion = new Quaternion(x2, y2, z2, w);
						*Luau.lua_class_push<Quaternion>(L) = quaternion;
					}
					else
					{
						PushTable(L, (JObject)item.Value);
					}
				}
				else if (item.Value is JValue)
				{
					JTokenType type = item.Value.Type;
					if (type == JTokenType.Integer)
					{
						Luau.lua_pushnumber(L, item.Value.ToObject<int>());
					}
					else if (type == JTokenType.Boolean)
					{
						Luau.lua_pushboolean(L, item.Value.ToObject<bool>() ? 1 : 0);
					}
					else if (type == JTokenType.Float)
					{
						Luau.lua_pushnumber(L, item.Value.ToObject<double>());
					}
					else
					{
						if (type != JTokenType.String)
						{
							continue;
						}
						Luau.lua_pushstring(L, item.Value.ToString());
					}
				}
				if (num == -1)
				{
					Luau.lua_rawset(L, -3);
				}
				else
				{
					Luau.lua_rawseti(L, -2, num);
				}
			}
			return true;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int DataSave(lua_State* L)
		{
			try
			{
				string text = JsonConvert.SerializeObject(ConsumeTable(L, 1), Formatting.Indented);
				if (text.Length > 10000)
				{
					Luau.luaL_errorL(L, "Save exceeds 10000 bytes");
					return 0;
				}
				DirectoryInfo directoryInfo = new DirectoryInfo(Path.Join(ModIODirectory, "saves", CustomMapLoader.LoadedMapModId.ToString()));
				if (!directoryInfo.Exists)
				{
					directoryInfo.Create();
				}
				File.WriteAllText(Path.Join(directoryInfo.FullName, "luau.json"), text);
				return 0;
			}
			catch
			{
				Luau.luaL_errorL(L, "Argument 2 must be a table");
				return 0;
			}
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int DataLoad(lua_State* L)
		{
			try
			{
				DirectoryInfo directoryInfo = new DirectoryInfo(Path.Join(ModIODirectory, "saves", CustomMapLoader.LoadedMapModId.ToString()));
				if (!directoryInfo.Exists)
				{
					Luau.lua_createtable(L, 0, 0);
					return 1;
				}
				FileInfo[] files = directoryInfo.GetFiles("luau.json");
				if (files.Length == 0)
				{
					Luau.lua_createtable(L, 0, 0);
					return 1;
				}
				JObject table = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(files[0].FullName));
				if (PushTable(L, table))
				{
					return 1;
				}
				return 0;
			}
			catch
			{
				Luau.luaL_errorL(L, "Error while loading data");
				return 0;
			}
		}
	}

	[BurstCompile]
	public struct LuauRoomState
	{
		[MarshalAs(UnmanagedType.U1)]
		public bool IsQuest;

		public float FPS;

		[MarshalAs(UnmanagedType.U1)]
		public bool IsPrivate;

		public FixedString32Bytes RoomCode;
	}

	public static class PlayerUtils
	{
		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int TeleportPlayer(lua_State* L)
		{
			Vector3 vector = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			bool keepVelocity = Luau.lua_toboolean(L, 2) == 1;
			if (GTPlayer.hasInstance)
			{
				GTPlayer instance = GTPlayer.Instance;
				Vector3 position = instance.transform.position;
				Vector3 vector2 = instance.mainCamera.transform.position - position;
				Vector3 position2 = vector - vector2;
				instance.TeleportTo(position2, instance.transform.rotation, keepVelocity);
			}
			return 0;
		}

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int SetVelocity(lua_State* L)
		{
			Vector3 velocity = *Luau.lua_class_get<Vector3>(L, 1);
			if (GTPlayer.hasInstance)
			{
				GTPlayer.Instance.SetVelocity(velocity);
			}
			return 0;
		}
	}

	public static class RayCastUtils
	{
		public static RaycastHit rayHit;

		[MonoPInvokeCallback(typeof(lua_CFunction))]
		public unsafe static int RayCast(lua_State* L)
		{
			Vector3 origin = *Luau.lua_class_get<Vector3>(L, 1, "Vec3");
			Vector3 direction = *Luau.lua_class_get<Vector3>(L, 2, "Vec3");
			if (!Physics.Raycast(origin, direction, out rayHit))
			{
				return 0;
			}
			Luau.lua_createtable(L, 0, 0);
			Luau.lua_pushstring(L, "distance");
			Luau.lua_pushnumber(L, rayHit.distance);
			Luau.lua_rawset(L, -3);
			Luau.lua_pushstring(L, "point");
			*Luau.lua_class_push<Vector3>(L, "Vec3") = rayHit.point;
			Luau.lua_rawset(L, -3);
			Luau.lua_pushstring(L, "normal");
			*Luau.lua_class_push<Vector3>(L, "Vec3") = rayHit.normal;
			Luau.lua_rawset(L, -3);
			Luau.lua_pushstring(L, "object");
			if (LuauGameObjectList.TryGetValue(rayHit.transform.gameObject, out var value))
			{
				Luau.lua_class_push(L, "GameObject", value);
			}
			else
			{
				Luau.lua_pushnil(L);
			}
			Luau.lua_rawset(L, -3);
			Luau.lua_pushstring(L, "player");
			VRRig vRRig = rayHit.collider?.GetComponentInParent<VRRig>();
			if (vRRig != null)
			{
				NetPlayer creator = vRRig.creator;
				if (creator != null)
				{
					if (LuauPlayerList.TryGetValue(creator.ActorNumber, out var value2))
					{
						Luau.lua_class_push(L, "Player", value2);
					}
					else
					{
						Luau.lua_pushnil(L);
					}
				}
				else
				{
					Luau.lua_pushnil(L);
				}
			}
			else
			{
				Luau.lua_pushnil(L);
			}
			Luau.lua_rawset(L, -3);
			return 1;
		}
	}

	public static class Components
	{
		public static class LuauParticleSystemBindings
		{
			public struct LuauParticleSystem
			{
				public int x;
			}

			public unsafe static void Builder(lua_State* L)
			{
				LuauVm.ClassBuilders.Append(new LuauClassBuilder<LuauParticleSystem>("ParticleSystem").AddFunction("play", play).AddFunction("stop", stop).AddFunction("clear", clear)
					.Build(L, global: false));
			}

			public unsafe static ParticleSystem GetParticleSystem(lua_State* L)
			{
				LuauParticleSystem* ptr = Luau.lua_class_get<LuauParticleSystem>(L, 1);
				if (ComponentList.TryGetValue((IntPtr)ptr, out var value) && value is ParticleSystem result)
				{
					return result;
				}
				return null;
			}

			[MonoPInvokeCallback(typeof(lua_CFunction))]
			public unsafe static int play(lua_State* L)
			{
				ParticleSystem particleSystem = GetParticleSystem(L);
				if (particleSystem != null)
				{
					particleSystem.Play();
				}
				return 0;
			}

			[MonoPInvokeCallback(typeof(lua_CFunction))]
			public unsafe static int stop(lua_State* L)
			{
				ParticleSystem particleSystem = GetParticleSystem(L);
				if (particleSystem != null)
				{
					particleSystem.Stop();
				}
				return 0;
			}

			[MonoPInvokeCallback(typeof(lua_CFunction))]
			public unsafe static int clear(lua_State* L)
			{
				ParticleSystem particleSystem = GetParticleSystem(L);
				if (particleSystem != null)
				{
					particleSystem.Clear();
				}
				return 0;
			}
		}

		public static class LuauAudioSourceBindings
		{
			public struct LuauAudioSource
			{
				public int x;
			}

			public unsafe static void Builder(lua_State* L)
			{
				LuauVm.ClassBuilders.Append(new LuauClassBuilder<LuauAudioSource>("AudioSource").AddFunction("play", play).AddFunction("setVolume", setVolume).AddFunction("setLoop", setLoop)
					.AddFunction("setPitch", setPitch)
					.AddFunction("setMinDistance", setMinDistance)
					.AddFunction("setMaxDistance", setMaxDistance)
					.Build(L, global: false));
			}

			[MonoPInvokeCallback(typeof(lua_CFunction))]
			public unsafe static AudioSource GetAudioSource(lua_State* L)
			{
				LuauAudioSource* ptr = Luau.lua_class_get<LuauAudioSource>(L, 1);
				if (ComponentList.TryGetValue((IntPtr)ptr, out var value) && value is AudioSource result)
				{
					return result;
				}
				return null;
			}

			[MonoPInvokeCallback(typeof(lua_CFunction))]
			public unsafe static int play(lua_State* L)
			{
				AudioSource audioSource = GetAudioSource(L);
				if (audioSource != null)
				{
					audioSource.Play();
				}
				return 0;
			}

			[MonoPInvokeCallback(typeof(lua_CFunction))]
			public unsafe static int setVolume(lua_State* L)
			{
				AudioSource audioSource = GetAudioSource(L);
				double num = Luau.luaL_checknumber(L, 2);
				if (audioSource != null)
				{
					audioSource.volume = (float)num;
				}
				return 0;
			}

			[MonoPInvokeCallback(typeof(lua_CFunction))]
			public unsafe static int setLoop(lua_State* L)
			{
				AudioSource audioSource = GetAudioSource(L);
				bool loop = Luau.lua_toboolean(L, 2) == 1;
				if (audioSource != null)
				{
					audioSource.loop = loop;
				}
				return 0;
			}

			[MonoPInvokeCallback(typeof(lua_CFunction))]
			public unsafe static int setPitch(lua_State* L)
			{
				AudioSource audioSource = GetAudioSource(L);
				double num = Luau.luaL_checknumber(L, 2);
				if (audioSource != null)
				{
					audioSource.pitch = (float)num;
				}
				return 0;
			}

			[MonoPInvokeCallback(typeof(lua_CFunction))]
			public unsafe static int setMinDistance(lua_State* L)
			{
				AudioSource audioSource = GetAudioSource(L);
				double num = Luau.luaL_checknumber(L, 2);
				if (audioSource != null)
				{
					audioSource.minDistance = (float)num;
				}
				return 0;
			}

			[MonoPInvokeCallback(typeof(lua_CFunction))]
			public unsafe static int setMaxDistance(lua_State* L)
			{
				AudioSource audioSource = GetAudioSource(L);
				double num = Luau.luaL_checknumber(L, 2);
				if (audioSource != null)
				{
					audioSource.maxDistance = (float)num;
				}
				return 0;
			}
		}

		public static class LuauLightBindings
		{
			public struct LuauLight
			{
				public int x;
			}

			public unsafe static void Builder(lua_State* L)
			{
				LuauVm.ClassBuilders.Append(new LuauClassBuilder<LuauLight>("Light").AddFunction("setColor", setColor).AddFunction("setIntensity", setIntensity).AddFunction("setRange", setRange)
					.Build(L, global: false));
			}

			public unsafe static Light GetLight(lua_State* L)
			{
				LuauLight* ptr = Luau.lua_class_get<LuauLight>(L, 1);
				if (ComponentList.TryGetValue((IntPtr)ptr, out var value) && value is Light result)
				{
					return result;
				}
				return null;
			}

			[MonoPInvokeCallback(typeof(lua_CFunction))]
			public unsafe static int setColor(lua_State* L)
			{
				Light light = GetLight(L);
				Vector3 vector = *Luau.lua_class_get<Vector3>(L, 2);
				if (light != null)
				{
					light.color = new Color(vector.x, vector.y, vector.z);
				}
				return 0;
			}

			[MonoPInvokeCallback(typeof(lua_CFunction))]
			public unsafe static int setIntensity(lua_State* L)
			{
				Light light = GetLight(L);
				double num = Luau.luaL_checknumber(L, 2);
				if (light != null)
				{
					light.intensity = (float)num;
				}
				return 0;
			}

			[MonoPInvokeCallback(typeof(lua_CFunction))]
			public unsafe static int setRange(lua_State* L)
			{
				Light light = GetLight(L);
				double num = Luau.luaL_checknumber(L, 2);
				if (light != null)
				{
					light.range = (float)num;
				}
				return 0;
			}
		}

		public static class LuauAnimatorBindings
		{
			public struct LuauAnimator
			{
				public int x;
			}

			public unsafe static void Builder(lua_State* L)
			{
				LuauVm.ClassBuilders.Append(new LuauClassBuilder<LuauAnimator>("Animator").AddFunction("setSpeed", setSpeed).AddFunction("startPlayback", startPlayback).AddFunction("stopPlayback", stopPlayback)
					.AddFunction("reset", reset)
					.Build(L, global: false));
			}

			public unsafe static Animator GetAnimator(lua_State* L)
			{
				LuauAnimator* ptr = Luau.lua_class_get<LuauAnimator>(L, 1);
				if (ComponentList.TryGetValue((IntPtr)ptr, out var value) && value is Animator result)
				{
					return result;
				}
				return null;
			}

			[MonoPInvokeCallback(typeof(lua_CFunction))]
			public unsafe static int setSpeed(lua_State* L)
			{
				Animator animator = GetAnimator(L);
				double num = Luau.luaL_checknumber(L, 2);
				if (animator != null)
				{
					animator.speed = (float)num;
				}
				return 0;
			}

			[MonoPInvokeCallback(typeof(lua_CFunction))]
			public unsafe static int startPlayback(lua_State* L)
			{
				Animator animator = GetAnimator(L);
				if (animator != null)
				{
					animator.StartPlayback();
				}
				return 0;
			}

			[MonoPInvokeCallback(typeof(lua_CFunction))]
			public unsafe static int stopPlayback(lua_State* L)
			{
				Animator animator = GetAnimator(L);
				if (animator != null)
				{
					animator.StopPlayback();
				}
				return 0;
			}

			[MonoPInvokeCallback(typeof(lua_CFunction))]
			public unsafe static int reset(lua_State* L)
			{
				Animator animator = GetAnimator(L);
				if (animator != null)
				{
					animator.ResetToEntryState();
				}
				return 0;
			}
		}

		public static Dictionary<IntPtr, object> ComponentList = new Dictionary<IntPtr, object>();

		public unsafe static void Build(lua_State* L)
		{
			LuauParticleSystemBindings.Builder(L);
			LuauAudioSourceBindings.Builder(L);
			LuauLightBindings.Builder(L);
			LuauAnimatorBindings.Builder(L);
		}
	}

	public static Dictionary<GameObject, IntPtr> LuauGameObjectList = new Dictionary<GameObject, IntPtr>();

	public static List<KeyValuePair<GameObject, IntPtr>> LuauGameObjectDepthList = new List<KeyValuePair<GameObject, IntPtr>>();

	public static Dictionary<IntPtr, GameObject> LuauGameObjectListReverse = new Dictionary<IntPtr, GameObject>();

	public static Dictionary<GameObject, LuauGameObjectInitialState> LuauGameObjectStates = new Dictionary<GameObject, LuauGameObjectInitialState>();

	public static Dictionary<GameObject, int> LuauTriggerCallbacks = new Dictionary<GameObject, int>();

	public static Dictionary<int, IntPtr> LuauPlayerList = new Dictionary<int, IntPtr>();

	public static Dictionary<int, VRRig> LuauVRRigList = new Dictionary<int, VRRig>();

	public unsafe static GorillaLocomotionSettings* LocomotionSettings;

	public unsafe static PlayerInput* LocalPlayerInput;

	public unsafe static LuauRoomState* RoomState;

	public static Dictionary<int, IntPtr> LuauAIAgentList = new Dictionary<int, IntPtr>();

	public static Dictionary<int, IntPtr> LuauGrabbablesList = new Dictionary<int, IntPtr>();

	public unsafe static void GameObjectBuilder(lua_State* L)
	{
		LuauVm.ClassBuilders.Append(new LuauClassBuilder<LuauGameObject>("GameObject").AddField("position", "Position").AddField("rotation", "Rotation").AddField("scale", "Scale")
			.AddStaticFunction("findGameObject", GameObjectFunctions.FindGameObject)
			.AddFunction("setCollision", GameObjectFunctions.SetCollision)
			.AddFunction("setVisibility", GameObjectFunctions.SetVisibility)
			.AddFunction("setActive", GameObjectFunctions.SetActive)
			.AddFunction("setText", GameObjectFunctions.SetText)
			.AddFunction("onTouched", GameObjectFunctions.OnTouched)
			.AddFunction("setVelocity", GameObjectFunctions.SetVelocity)
			.AddFunction("getVelocity", GameObjectFunctions.GetVelocity)
			.AddFunction("setColor", GameObjectFunctions.SetColor)
			.AddFunction("findChild", GameObjectFunctions.FindChildGameObject)
			.AddFunction("clone", GameObjectFunctions.CloneGameObject)
			.AddFunction("destroy", GameObjectFunctions.DestroyGameObject)
			.AddFunction("findComponent", GameObjectFunctions.FindComponent)
			.AddFunction("equals", GameObjectFunctions.Equals)
			.Build(L, global: true));
	}

	public unsafe static void GorillaLocomotionSettingsBuilder(lua_State* L)
	{
		LuauVm.ClassBuilders.Append(new LuauClassBuilder<GorillaLocomotionSettings>("PSettings").AddField("velocityLimit").AddField("slideVelocityLimit").AddField("maxJumpSpeed")
			.AddField("jumpMultiplier")
			.Build(L, global: false));
		LocomotionSettings = Luau.lua_class_push<GorillaLocomotionSettings>(L);
		LocomotionSettings->velocityLimit = GTPlayer.Instance.velocityLimit;
		LocomotionSettings->slideVelocityLimit = GTPlayer.Instance.slideVelocityLimit;
		LocomotionSettings->maxJumpSpeed = 6.5f;
		LocomotionSettings->jumpMultiplier = 1.1f;
		Luau.lua_setglobal(L, "PlayerSettings");
	}

	public unsafe static void PlayerInputBuilder(lua_State* L)
	{
		LuauVm.ClassBuilders.Append(new LuauClassBuilder<PlayerInput>("PInput").AddField("leftXAxis").AddField("rightXAxis").AddField("leftYAxis")
			.AddField("rightYAxis")
			.AddField("leftTrigger")
			.AddField("rightTrigger")
			.AddField("leftGrip")
			.AddField("rightGrip")
			.AddField("leftPrimaryButton")
			.AddField("rightPrimaryButton")
			.AddField("leftSecondaryButton")
			.AddField("rightSecondaryButton")
			.Build(L, global: false));
		LocalPlayerInput = Luau.lua_class_push<PlayerInput>(L);
		UpdateInputs();
		Luau.lua_setglobal(L, "PlayerInput");
	}

	public unsafe static void UpdateInputs()
	{
		if (LocalPlayerInput != null)
		{
			LocalPlayerInput->leftPrimaryButton = ControllerInputPoller.PrimaryButtonPress(XRNode.LeftHand);
			LocalPlayerInput->rightPrimaryButton = ControllerInputPoller.PrimaryButtonPress(XRNode.RightHand);
			LocalPlayerInput->leftSecondaryButton = ControllerInputPoller.SecondaryButtonPress(XRNode.LeftHand);
			LocalPlayerInput->rightSecondaryButton = ControllerInputPoller.SecondaryButtonPress(XRNode.RightHand);
			LocalPlayerInput->leftGrip = ControllerInputPoller.GripFloat(XRNode.LeftHand);
			LocalPlayerInput->rightGrip = ControllerInputPoller.GripFloat(XRNode.RightHand);
			LocalPlayerInput->leftTrigger = ControllerInputPoller.TriggerFloat(XRNode.LeftHand);
			LocalPlayerInput->rightTrigger = ControllerInputPoller.TriggerFloat(XRNode.RightHand);
			Vector2 vector = ControllerInputPoller.Primary2DAxis(XRNode.LeftHand);
			Vector2 vector2 = ControllerInputPoller.Primary2DAxis(XRNode.RightHand);
			LocalPlayerInput->leftXAxis = vector.x;
			LocalPlayerInput->leftYAxis = vector.y;
			LocalPlayerInput->rightXAxis = vector2.x;
			LocalPlayerInput->rightYAxis = vector2.y;
		}
	}

	public unsafe static void Vec3Builder(lua_State* L)
	{
		LuauVm.ClassBuilders.Append(new LuauClassBuilder<Vector3>("Vec3").AddField("x").AddField("y").AddField("z")
			.AddStaticFunction("new", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.New))
			.AddFunction("__add", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.Add))
			.AddFunction("__sub", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.Sub))
			.AddFunction("__mul", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.Mul))
			.AddFunction("__div", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.Div))
			.AddFunction("__unm", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.Unm))
			.AddFunction("__eq", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.Eq))
			.AddFunction("__tostring", Vec3Functions.ToString)
			.AddFunction("toString", Vec3Functions.ToString)
			.AddFunction("dot", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.Dot))
			.AddFunction("cross", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.Cross))
			.AddFunction("projectOnTo", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.Project))
			.AddFunction("length", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.Length))
			.AddFunction("normalize", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.Normalize))
			.AddFunction("getSafeNormal", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.SafeNormal))
			.AddStaticFunction("rotate", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.Rotate))
			.AddFunction("rotate", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.Rotate))
			.AddStaticFunction("distance", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.Distance))
			.AddFunction("distance", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.Distance))
			.AddStaticFunction("lerp", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.Lerp))
			.AddFunction("lerp", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.Lerp))
			.AddProperty("zeroVector", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.ZeroVector))
			.AddProperty("oneVector", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.OneVector))
			.AddStaticFunction("nearlyEqual", BurstCompiler.CompileFunctionPointer<lua_CFunction>(Vec3Functions.NearlyEqual))
			.Build(L, global: true));
	}

	public unsafe static void QuatBuilder(lua_State* L)
	{
		LuauVm.ClassBuilders.Append(new LuauClassBuilder<Quaternion>("Quat").AddField("x").AddField("y").AddField("z")
			.AddField("w")
			.AddStaticFunction("new", BurstCompiler.CompileFunctionPointer<lua_CFunction>(QuatFunctions.New))
			.AddFunction("__mul", BurstCompiler.CompileFunctionPointer<lua_CFunction>(QuatFunctions.Mul))
			.AddFunction("__eq", BurstCompiler.CompileFunctionPointer<lua_CFunction>(QuatFunctions.Eq))
			.AddFunction("__tostring", QuatFunctions.ToString)
			.AddFunction("toString", QuatFunctions.ToString)
			.AddStaticFunction("fromEuler", BurstCompiler.CompileFunctionPointer<lua_CFunction>(QuatFunctions.FromEuler))
			.AddStaticFunction("fromDirection", BurstCompiler.CompileFunctionPointer<lua_CFunction>(QuatFunctions.FromDirection))
			.AddFunction("getUpVector", BurstCompiler.CompileFunctionPointer<lua_CFunction>(QuatFunctions.GetUpVector))
			.AddFunction("euler", BurstCompiler.CompileFunctionPointer<lua_CFunction>(QuatFunctions.Euler))
			.Build(L, global: true));
	}

	public unsafe static void PlayerBuilder(lua_State* L)
	{
		LuauVm.ClassBuilders.Append(new LuauClassBuilder<LuauPlayer>("Player").AddField("playerID", "PlayerID").AddField("playerName", "PlayerName").AddField("playerMaterial", "PlayerMaterial")
			.AddField("isMasterClient", "IsMasterClient")
			.AddField("bodyPosition", "BodyPosition")
			.AddField("velocity", "Velocity")
			.AddField("isPCVR", "IsPCVR")
			.AddField("leftHandPosition", "LeftHandPosition")
			.AddField("rightHandPosition", "RightHandPosition")
			.AddField("headRotation", "HeadRotation")
			.AddField("leftHandRotation", "LeftHandRotation")
			.AddField("rightHandRotation", "RightHandRotation")
			.AddField("isInVStump", "IsInVStump")
			.AddField("isEntityAuthority", "IsEntityAuthority")
			.AddStaticFunction("getPlayerByID", PlayerFunctions.GetPlayerByID)
			.Build(L, global: true));
	}

	public unsafe static void AIAgentBuilder(lua_State* L)
	{
		LuauVm.ClassBuilders.Append(new LuauClassBuilder<LuauAIAgent>("AIAgent").AddField("entityID", "EntityID").AddField("agentPosition", "EntityPosition").AddField("agentRotation", "EntityRotation")
			.AddFunction("__tostring", AIAgentFunctions.ToString)
			.AddFunction("toString", AIAgentFunctions.ToString)
			.AddFunction("setDestination", AIAgentFunctions.SetDestination)
			.AddFunction("destroyAgent", AIAgentFunctions.DestroyEntity)
			.AddFunction("playAgentAnimation", AIAgentFunctions.PlayAgentAnimation)
			.AddFunction("getTargetPlayer", AIAgentFunctions.GetTarget)
			.AddFunction("setTargetPlayer", AIAgentFunctions.SetTarget)
			.AddStaticFunction("findPrePlacedAIAgentByID", AIAgentFunctions.FindPrePlacedAIAgentByID)
			.AddStaticFunction("getAIAgentByEntityID", AIAgentFunctions.GetAIAgentByEntityID)
			.AddStaticFunction("spawnAIAgent", AIAgentFunctions.SpawnAIAgent)
			.Build(L, global: true));
	}

	public unsafe static void GrabbableEntityBuilder(lua_State* L)
	{
		LuauVm.ClassBuilders.Append(new LuauClassBuilder<LuauGrabbableEntity>("GrabbableEntity").AddField("entityID", "EntityID").AddField("entityPosition", "EntityPosition").AddField("entityRotation", "EntityRotation")
			.AddFunction("__tostring", GrabbableEntityFunctions.ToString)
			.AddFunction("toString", GrabbableEntityFunctions.ToString)
			.AddFunction("destroyGrabbable", GrabbableEntityFunctions.DestroyEntity)
			.AddStaticFunction("findPrePlacedGrabbableEntityByID", GrabbableEntityFunctions.FindPrePlacedGrabbableEntityByID)
			.AddStaticFunction("getGrabbableEntityByEntityID", GrabbableEntityFunctions.GetGrabbableEntityByEntityID)
			.AddStaticFunction("getHoldingActorNumberByEntityID", GrabbableEntityFunctions.GetHoldingActorNumberByEntityID)
			.AddStaticFunction("getHoldingActorNumberByLuauID", GrabbableEntityFunctions.GetHoldingActorNumberByLuauID)
			.AddStaticFunction("spawnGrabbableEntity", GrabbableEntityFunctions.SpawnGrabbableEntity)
			.Build(L, global: true));
	}

	[MonoPInvokeCallback(typeof(lua_CFunction))]
	public unsafe static int LuaStartVibration(lua_State* L)
	{
		bool forLeftController = Luau.lua_toboolean(L, 1) == 1;
		float amplitude = (float)Luau.luaL_checknumber(L, 2);
		float duration = (float)Luau.luaL_checknumber(L, 3);
		GorillaTagger.Instance.StartVibration(forLeftController, amplitude, duration);
		return 0;
	}

	[MonoPInvokeCallback(typeof(lua_CFunction))]
	public unsafe static int LuaPlaySound(lua_State* L)
	{
		int num = (int)Luau.luaL_checknumber(L, 1);
		Vector3 position = *Luau.lua_class_get<Vector3>(L, 2, "Vec3");
		float volume = (float)Luau.luaL_checknumber(L, 3);
		if (num < 0 || num >= VRRig.LocalRig.clipToPlay.Length)
		{
			return 0;
		}
		AudioSource.PlayClipAtPoint(VRRig.LocalRig.clipToPlay[num], position, volume);
		return 0;
	}

	public unsafe static void RoomStateBuilder(lua_State* L)
	{
		LuauVm.ClassBuilders.Append(new LuauClassBuilder<LuauRoomState>("RState").AddField("isQuest", "IsQuest").AddField("fps", "FPS").AddField("isPrivate", "IsPrivate")
			.AddField("code", "RoomCode")
			.Build(L, global: false));
		RoomState = Luau.lua_class_push<LuauRoomState>(L);
		UpdateRoomState();
		RoomState->IsQuest = false;
		RoomState->IsPrivate = !PhotonNetwork.CurrentRoom.IsVisible;
		RoomState->RoomCode = PhotonNetwork.CurrentRoom.Name;
		Luau.lua_setglobal(L, "Room");
	}

	public unsafe static void UpdateRoomState()
	{
		RoomState->FPS = 1f / Time.smoothDeltaTime;
	}
}
