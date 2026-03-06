using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GorillaExtensions;
using UnityEngine;

namespace GorillaTag.CosmeticSystem;

public static class GTHardCodedBones
{
	public enum EBone
	{
		None,
		rig,
		body,
		head,
		head_end,
		shoulder_L,
		upper_arm_L,
		forearm_L,
		hand_L,
		palm_01_L,
		palm_02_L,
		thumb_01_L,
		thumb_02_L,
		thumb_03_L,
		thumb_03_L_end,
		f_index_01_L,
		f_index_02_L,
		f_index_03_L,
		f_index_03_L_end,
		f_middle_01_L,
		f_middle_02_L,
		f_middle_03_L,
		f_middle_03_L_end,
		shoulder_R,
		upper_arm_R,
		forearm_R,
		hand_R,
		palm_01_R,
		palm_02_R,
		thumb_01_R,
		thumb_02_R,
		thumb_03_R,
		thumb_03_R_end,
		f_index_01_R,
		f_index_02_R,
		f_index_03_R,
		f_index_03_R_end,
		f_middle_01_R,
		f_middle_02_R,
		f_middle_03_R,
		f_middle_03_R_end,
		body_AnchorTop_Neck,
		body_AnchorFront_StowSlot,
		body_AnchorFrontLeft_Badge,
		body_AnchorFrontRight_NameTag,
		body_AnchorBack,
		body_AnchorBackLeft_StowSlot,
		body_AnchorBackRight_StowSlot,
		body_AnchorBottom,
		body_AnchorBackBottom_Tail,
		hand_L_AnchorBack,
		hand_R_AnchorBack,
		hand_L_AnchorFront_GameModeItemSlot
	}

	public enum EStowSlots
	{
		None = 0,
		forearm_L = 7,
		forearm_R = 25,
		body_AnchorFront_Chest = 42,
		body_AnchorBackLeft = 46,
		body_AnchorBackRight = 47
	}

	public enum EHandAndStowSlots
	{
		None = 0,
		forearm_L = 7,
		hand_L = 8,
		forearm_R = 25,
		hand_R = 26,
		body_AnchorFront_Chest = 42,
		body_AnchorBackLeft = 46,
		body_AnchorBackRight = 47
	}

	public enum ECosmeticSlots
	{
		Hat = 4,
		Badge = 43,
		Face = 3,
		ArmLeft = 6,
		ArmRight = 24,
		BackLeft = 46,
		BackRight = 47,
		HandLeft = 8,
		HandRight = 26,
		Chest = 42,
		Fur = 1,
		Shirt = 2,
		Pants = 48,
		Back = 45,
		Arms = 2,
		TagEffect = 0
	}

	[Serializable]
	public struct SturdyEBone : ISerializationCallbackReceiver
	{
		[SerializeField]
		private EBone _bone;

		[SerializeField]
		private string _boneName;

		public EBone Bone
		{
			get
			{
				return _bone;
			}
			set
			{
				_bone = value;
				_boneName = GetBoneName(_bone);
			}
		}

		public SturdyEBone(EBone bone)
		{
			_bone = bone;
			_boneName = null;
		}

		public SturdyEBone(string boneName)
		{
			_bone = GetBone(boneName);
			_boneName = null;
		}

		public static implicit operator EBone(SturdyEBone sturdyBone)
		{
			return sturdyBone.Bone;
		}

		public static implicit operator SturdyEBone(EBone bone)
		{
			return new SturdyEBone(bone);
		}

		public static explicit operator int(SturdyEBone sturdyBone)
		{
			return (int)sturdyBone.Bone;
		}

		public override string ToString()
		{
			return _boneName;
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			if (string.IsNullOrEmpty(_boneName))
			{
				_bone = EBone.None;
				_boneName = "None";
				return;
			}
			EBone bone = GetBone(_boneName);
			if (bone != EBone.None)
			{
				_bone = bone;
			}
		}
	}

	public const int kBoneCount = 53;

	public static readonly string[] kBoneNames = new string[53]
	{
		"None", "rig", "body", "head", "head_end", "shoulder.L", "upper_arm.L", "forearm.L", "hand.L", "palm.01.L",
		"palm.02.L", "thumb.01.L", "thumb.02.L", "thumb.03.L", "thumb.03.L_end", "f_index.01.L", "f_index.02.L", "f_index.03.L", "f_index.03.L_end", "f_middle.01.L",
		"f_middle.02.L", "f_middle.03.L", "f_middle.03.L_end", "shoulder.R", "upper_arm.R", "forearm.R", "hand.R", "palm.01.R", "palm.02.R", "thumb.01.R",
		"thumb.02.R", "thumb.03.R", "thumb.03.R_end", "f_index.01.R", "f_index.02.R", "f_index.03.R", "f_index.03.R_end", "f_middle.01.R", "f_middle.02.R", "f_middle.03.R",
		"f_middle.03.R_end", "body_AnchorTop_Neck", "body_AnchorFront_StowSlot", "body_AnchorFrontLeft_Badge", "body_AnchorFrontRight_NameTag", "body_AnchorBack", "body_AnchorBackLeft_StowSlot", "body_AnchorBackRight_StowSlot", "body_AnchorBottom", "body_AnchorBackBottom_Tail",
		"hand_L_AnchorBack", "hand_R_AnchorBack", "hand_L_AnchorFront_GameModeItemSlot"
	};

	private const long kLeftSideMask = 1728432283058160L;

	private const long kRightSideMask = 1769114204897280L;

	private static readonly Dictionary<BodyDockPositions.DropPositions, EBone> _k_bodyDockDropPosition_to_eBone = new Dictionary<BodyDockPositions.DropPositions, EBone>
	{
		{
			BodyDockPositions.DropPositions.None,
			EBone.None
		},
		{
			BodyDockPositions.DropPositions.LeftArm,
			EBone.forearm_L
		},
		{
			BodyDockPositions.DropPositions.RightArm,
			EBone.forearm_R
		},
		{
			BodyDockPositions.DropPositions.Chest,
			EBone.body_AnchorFront_StowSlot
		},
		{
			BodyDockPositions.DropPositions.LeftBack,
			EBone.body_AnchorBackLeft_StowSlot
		},
		{
			BodyDockPositions.DropPositions.RightBack,
			EBone.body_AnchorBackRight_StowSlot
		}
	};

	private static readonly Dictionary<TransferrableObject.PositionState, EBone> _k_transferrablePosState_to_eBone = new Dictionary<TransferrableObject.PositionState, EBone>
	{
		{
			TransferrableObject.PositionState.None,
			EBone.None
		},
		{
			TransferrableObject.PositionState.OnLeftArm,
			EBone.forearm_L
		},
		{
			TransferrableObject.PositionState.OnRightArm,
			EBone.forearm_R
		},
		{
			TransferrableObject.PositionState.InLeftHand,
			EBone.hand_L
		},
		{
			TransferrableObject.PositionState.InRightHand,
			EBone.hand_R
		},
		{
			TransferrableObject.PositionState.OnChest,
			EBone.body_AnchorFront_StowSlot
		},
		{
			TransferrableObject.PositionState.OnLeftShoulder,
			EBone.body_AnchorBackLeft_StowSlot
		},
		{
			TransferrableObject.PositionState.OnRightShoulder,
			EBone.body_AnchorBackRight_StowSlot
		},
		{
			TransferrableObject.PositionState.Dropped,
			EBone.None
		}
	};

	private static readonly Dictionary<EBone, TransferrableObject.PositionState> _k_eBone_to_transferrablePosState = new Dictionary<EBone, TransferrableObject.PositionState>
	{
		{
			EBone.None,
			TransferrableObject.PositionState.None
		},
		{
			EBone.forearm_L,
			TransferrableObject.PositionState.OnLeftArm
		},
		{
			EBone.forearm_R,
			TransferrableObject.PositionState.OnRightArm
		},
		{
			EBone.hand_L,
			TransferrableObject.PositionState.InLeftHand
		},
		{
			EBone.hand_R,
			TransferrableObject.PositionState.InRightHand
		},
		{
			EBone.body_AnchorFront_StowSlot,
			TransferrableObject.PositionState.OnChest
		},
		{
			EBone.body_AnchorBackLeft_StowSlot,
			TransferrableObject.PositionState.OnLeftShoulder
		},
		{
			EBone.body_AnchorBackRight_StowSlot,
			TransferrableObject.PositionState.OnRightShoulder
		}
	};

	[OnEnterPlay_Clear]
	[OnExitPlay_Clear]
	private static readonly List<int> _gMissingBonesReport = new List<int>(53);

	[OnEnterPlay_Clear]
	[OnExitPlay_Clear]
	private static readonly Dictionary<int, Transform[]> _gInstIds_To_boneXforms = new Dictionary<int, Transform[]>(20);

	[OnEnterPlay_Clear]
	[OnExitPlay_Clear]
	private static readonly Dictionary<int, Transform[]> _gInstIds_To_slotXforms = new Dictionary<int, Transform[]>(20);

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void HandleRuntimeInitialize_OnBeforeSceneLoad()
	{
		VRRigCache.OnPostInitialize += HandleVRRigCache_OnPostInitialize;
	}

	private static void HandleVRRigCache_OnPostInitialize()
	{
		VRRigCache.OnPostInitialize -= HandleVRRigCache_OnPostInitialize;
		HandleVRRigCache_OnPostSpawnRig();
		VRRigCache.OnPostSpawnRig += HandleVRRigCache_OnPostSpawnRig;
	}

	private static void HandleVRRigCache_OnPostSpawnRig()
	{
		if (VRRigCache.isInitialized)
		{
			_ = ApplicationQuittingState.IsQuitting;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetBoneIndex(EBone bone)
	{
		return (int)bone;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetBoneIndex(string name)
	{
		for (int i = 0; i < kBoneNames.Length; i++)
		{
			if (kBoneNames[i] == name)
			{
				return i;
			}
		}
		return 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGetBoneIndexByName(string name, out int out_index)
	{
		for (int i = 0; i < kBoneNames.Length; i++)
		{
			if (kBoneNames[i] == name)
			{
				out_index = i;
				return true;
			}
		}
		out_index = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EBone GetBone(string name)
	{
		return (EBone)GetBoneIndex(name);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGetBoneByName(string name, out EBone out_eBone)
	{
		if (TryGetBoneIndexByName(name, out var out_index))
		{
			out_eBone = (EBone)out_index;
			return true;
		}
		out_eBone = EBone.None;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GetBoneName(int boneIndex)
	{
		return kBoneNames[boneIndex];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGetBoneName(int boneIndex, out string out_name)
	{
		if (boneIndex >= 0 && boneIndex < kBoneNames.Length)
		{
			out_name = kBoneNames[boneIndex];
			return true;
		}
		out_name = "None";
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GetBoneName(EBone bone)
	{
		return GetBoneName((int)bone);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGetBoneName(EBone bone, out string out_name)
	{
		return TryGetBoneName((int)bone, out out_name);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long GetBoneBitFlag(string name)
	{
		if (name == "None")
		{
			return 0L;
		}
		for (int i = 0; i < kBoneNames.Length; i++)
		{
			if (kBoneNames[i] == name)
			{
				return 1L << i - 1;
			}
		}
		return 0L;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long GetBoneBitFlag(EBone bone)
	{
		if (bone == EBone.None)
		{
			return 0L;
		}
		return 1L << (int)(bone - 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EHandedness GetHandednessFromBone(EBone bone)
	{
		if ((GetBoneBitFlag(bone) & 0x62400003FFFF0L) == 0L)
		{
			if ((GetBoneBitFlag(bone) & 0x648FFFFC00000L) == 0L)
			{
				return EHandedness.None;
			}
			return EHandedness.Right;
		}
		return EHandedness.Left;
	}

	public static bool TryGetBoneXforms(VRRig vrRig, out Transform[] outBoneXforms, out string outErrorMsg)
	{
		outErrorMsg = string.Empty;
		if (vrRig == null)
		{
			outErrorMsg = "The VRRig is null.";
			outBoneXforms = Array.Empty<Transform>();
			return false;
		}
		int instanceID = vrRig.GetInstanceID();
		if (_gInstIds_To_boneXforms.TryGetValue(instanceID, out outBoneXforms))
		{
			return true;
		}
		if (!TryGetBoneXforms(vrRig.mainSkin, out outBoneXforms, out outErrorMsg))
		{
			return false;
		}
		VRRigAnchorOverrides componentInChildren = vrRig.GetComponentInChildren<VRRigAnchorOverrides>(includeInactive: true);
		BodyDockPositions componentInChildren2 = vrRig.GetComponentInChildren<BodyDockPositions>(includeInactive: true);
		outBoneXforms[46] = componentInChildren2.leftBackTransform;
		outBoneXforms[47] = componentInChildren2.rightBackTransform;
		outBoneXforms[42] = componentInChildren2.chestTransform;
		outBoneXforms[43] = componentInChildren.CurrentBadgeTransform;
		outBoneXforms[44] = componentInChildren.nameTransform;
		outBoneXforms[52] = componentInChildren.huntComputer;
		outBoneXforms[50] = componentInChildren.friendshipBraceletLeftAnchor;
		outBoneXforms[51] = componentInChildren.friendshipBraceletRightAnchor;
		_gInstIds_To_boneXforms[instanceID] = outBoneXforms;
		return true;
	}

	public static bool TryGetSlotAnchorXforms(VRRig vrRig, out Transform[] outSlotXforms, out string outErrorMsg)
	{
		outErrorMsg = string.Empty;
		if (vrRig == null)
		{
			outErrorMsg = "The VRRig is null.";
			outSlotXforms = Array.Empty<Transform>();
			return false;
		}
		int instanceID = vrRig.GetInstanceID();
		if (_gInstIds_To_slotXforms.TryGetValue(instanceID, out outSlotXforms))
		{
			return true;
		}
		if (!TryGetBoneXforms(vrRig.mainSkin, out var outBoneXforms, out outErrorMsg))
		{
			return false;
		}
		outSlotXforms = new Transform[outBoneXforms.Length];
		for (int i = 0; i < outBoneXforms.Length; i++)
		{
			outSlotXforms[i] = outBoneXforms[i];
		}
		BodyDockPositions componentInChildren = vrRig.GetComponentInChildren<BodyDockPositions>(includeInactive: true);
		outSlotXforms[7] = componentInChildren.leftArmTransform;
		outSlotXforms[25] = componentInChildren.rightArmTransform;
		outSlotXforms[8] = componentInChildren.leftHandTransform;
		outSlotXforms[26] = componentInChildren.rightHandTransform;
		_gInstIds_To_slotXforms[instanceID] = outSlotXforms;
		return true;
	}

	public static bool TryGetBoneXforms(SkinnedMeshRenderer skinnedMeshRenderer, out Transform[] outBoneXforms, out string outErrorMsg)
	{
		outErrorMsg = string.Empty;
		if (skinnedMeshRenderer == null)
		{
			outErrorMsg = "The SkinnedMeshRenderer was null.";
			outBoneXforms = Array.Empty<Transform>();
			return false;
		}
		int instanceID = skinnedMeshRenderer.GetInstanceID();
		if (_gInstIds_To_boneXforms.TryGetValue(instanceID, out outBoneXforms))
		{
			return true;
		}
		_gMissingBonesReport.Clear();
		Transform[] bones = skinnedMeshRenderer.bones;
		for (int i = 0; i < bones.Length; i++)
		{
			if (bones[i] == null)
			{
				Debug.LogError($"this should never happen -- skinned mesh bone index {i} is null in component: " + "\"" + skinnedMeshRenderer.GetComponentPath() + "\"", skinnedMeshRenderer);
			}
			else if (bones[i].parent == null)
			{
				Debug.LogError($"unexpected and unhandled scenario -- skinned mesh bone at index {i} has no parent in " + "component: \"" + skinnedMeshRenderer.GetComponentPath() + "\"", skinnedMeshRenderer);
			}
			else
			{
				bones[i] = (bones[i].name.EndsWith("_new") ? bones[i].parent : bones[i]);
			}
		}
		outBoneXforms = new Transform[kBoneNames.Length];
		for (int j = 1; j < kBoneNames.Length; j++)
		{
			string text = kBoneNames[j];
			if (text == "None" || text.EndsWith("_end") || text.Contains("Anchor") || j == 1)
			{
				continue;
			}
			bool flag = false;
			Transform[] array = bones;
			foreach (Transform transform in array)
			{
				if (!(transform == null) && !(transform.name != text))
				{
					outBoneXforms[j] = transform;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				_gMissingBonesReport.Add(j);
			}
		}
		for (int l = 1; l < kBoneNames.Length; l++)
		{
			string text2 = kBoneNames[l];
			if (!text2.EndsWith("_end"))
			{
				continue;
			}
			string text3 = text2;
			int boneIndex = GetBoneIndex(text3.Substring(0, text3.Length - 4));
			if (boneIndex < 0)
			{
				_gMissingBonesReport.Add(l);
				continue;
			}
			Transform transform2 = outBoneXforms[boneIndex];
			if (transform2 == null)
			{
				_gMissingBonesReport.Add(l);
				continue;
			}
			Transform transform3 = transform2.Find(text2);
			if (transform3 == null)
			{
				_gMissingBonesReport.Add(l);
			}
			else
			{
				outBoneXforms[l] = transform3;
			}
		}
		Transform transform4 = outBoneXforms[2];
		if ((object)transform4 != null && (object)transform4.parent != null)
		{
			outBoneXforms[1] = transform4.parent.parent;
		}
		else
		{
			_gMissingBonesReport.Add(1);
		}
		for (int m = 1; m < kBoneNames.Length; m++)
		{
			string text4 = kBoneNames[m];
			if (text4.Contains("Anchor"))
			{
				if (transform4.TryFindByPath("/**/" + text4, out var result))
				{
					outBoneXforms[m] = result;
					continue;
				}
				GameObject gameObject = new GameObject(text4);
				gameObject.transform.SetParent(transform4, worldPositionStays: false);
				outBoneXforms[m] = gameObject.transform;
			}
		}
		_gInstIds_To_boneXforms[instanceID] = outBoneXforms;
		if (_gMissingBonesReport.Count == 0)
		{
			return true;
		}
		string text5 = "The SkinnedMeshRenderer on \"" + skinnedMeshRenderer.name + "\" did not have these expected bones: ";
		foreach (int item in _gMissingBonesReport)
		{
			text5 = text5 + "\n- " + kBoneNames[item];
		}
		outErrorMsg = text5;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGetBoneXform(Transform[] boneXforms, string boneName, out Transform boneXform)
	{
		boneXform = boneXforms[GetBoneIndex(boneName)];
		return boneXform != null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGetBoneXform(Transform[] boneXforms, EBone eBone, out Transform boneXform)
	{
		boneXform = boneXforms[GetBoneIndex(eBone)];
		return boneXform != null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGetFirstBoneInParents(Transform transform, out EBone eBone, out Transform boneXform)
	{
		while (transform != null)
		{
			string name = transform.name;
			if (name == "DropZoneAnchor" && (object)transform.parent != null)
			{
				switch (transform.parent.name)
				{
				case "Slingshot Chest Snap":
					eBone = EBone.body_AnchorFront_StowSlot;
					boneXform = transform;
					return true;
				case "TransferrableItemLeftArm":
					eBone = EBone.forearm_L;
					boneXform = transform;
					return true;
				case "TransferrableItemLeftShoulder":
					eBone = EBone.body_AnchorBackLeft_StowSlot;
					boneXform = transform;
					return true;
				case "TransferrableItemRightShoulder":
					eBone = EBone.body_AnchorBackRight_StowSlot;
					boneXform = transform;
					return true;
				}
			}
			else
			{
				if (name == "TransferrableItemLeftHand")
				{
					eBone = EBone.hand_L;
					boneXform = transform;
					return true;
				}
				if (name == "TransferrableItemRightHand")
				{
					eBone = EBone.hand_R;
					boneXform = transform;
					return true;
				}
			}
			EBone bone = GetBone(transform.name);
			if (bone != EBone.None)
			{
				eBone = bone;
				boneXform = transform;
				return true;
			}
			transform = transform.parent;
		}
		eBone = EBone.None;
		boneXform = null;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EBone GetBoneEnumOfCosmeticPosStateFlag(TransferrableObject.PositionState positionState)
	{
		switch (positionState)
		{
		case TransferrableObject.PositionState.OnLeftArm:
			return EBone.forearm_L;
		case TransferrableObject.PositionState.OnRightArm:
			return EBone.forearm_R;
		case TransferrableObject.PositionState.InLeftHand:
			return EBone.hand_L;
		case TransferrableObject.PositionState.InRightHand:
			return EBone.hand_R;
		case TransferrableObject.PositionState.OnChest:
			return EBone.body_AnchorFront_StowSlot;
		case TransferrableObject.PositionState.OnLeftShoulder:
			return EBone.body_AnchorBackLeft_StowSlot;
		case TransferrableObject.PositionState.OnRightShoulder:
			return EBone.body_AnchorBackRight_StowSlot;
		case TransferrableObject.PositionState.None:
		case TransferrableObject.PositionState.Dropped:
			return EBone.None;
		default:
			throw new ArgumentOutOfRangeException(positionState.ToString());
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static List<EBone> GetBoneEnumsFromCosmeticBodyDockDropPosFlags(BodyDockPositions.DropPositions enumFlags)
	{
		BodyDockPositions.DropPositions[] values = EnumData<BodyDockPositions.DropPositions>.Shared.Values;
		List<EBone> list = new List<EBone>(32);
		BodyDockPositions.DropPositions[] array = values;
		foreach (BodyDockPositions.DropPositions dropPositions in array)
		{
			if (dropPositions != BodyDockPositions.DropPositions.All && dropPositions != BodyDockPositions.DropPositions.None && dropPositions != BodyDockPositions.DropPositions.MaxDropPostions && (enumFlags & dropPositions) != BodyDockPositions.DropPositions.None)
			{
				list.Add(_k_bodyDockDropPosition_to_eBone[dropPositions]);
			}
		}
		return list;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static List<EBone> GetBoneEnumsFromCosmeticTransferrablePosStateFlags(TransferrableObject.PositionState enumFlags)
	{
		TransferrableObject.PositionState[] values = EnumData<TransferrableObject.PositionState>.Shared.Values;
		List<EBone> list = new List<EBone>(32);
		TransferrableObject.PositionState[] array = values;
		foreach (TransferrableObject.PositionState positionState in array)
		{
			if (positionState != TransferrableObject.PositionState.None && positionState != TransferrableObject.PositionState.Dropped && (enumFlags & positionState) != TransferrableObject.PositionState.None)
			{
				list.Add(_k_transferrablePosState_to_eBone[positionState]);
			}
		}
		return list;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGetTransferrablePosStateFromBoneEnum(EBone eBone, out TransferrableObject.PositionState outPosState)
	{
		return _k_eBone_to_transferrablePosState.TryGetValue(eBone, out outPosState);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform GetBoneXformOfCosmeticPosStateFlag(TransferrableObject.PositionState anchorPosState, Transform[] bones)
	{
		if (bones.Length != 53)
		{
			throw new Exception(string.Format("{0}: Supplied bones array length is {1} but requires ", "GTHardCodedBones", bones.Length) + $"{53}.");
		}
		int boneIndex = GetBoneIndex(GetBoneEnumOfCosmeticPosStateFlag(anchorPosState));
		if (boneIndex != -1)
		{
			return bones[boneIndex];
		}
		return null;
	}
}
