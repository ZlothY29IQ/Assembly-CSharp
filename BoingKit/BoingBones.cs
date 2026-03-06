using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BoingKit;

public class BoingBones : BoingReactor
{
	[Serializable]
	public class Bone
	{
		internal BoingWork.Params.InstanceData Instance;

		internal Transform Transform;

		internal Vector3 ScaleWs;

		internal Vector3 CachedScaleLs;

		internal Vector3 BlendedPositionWs;

		internal Vector3 BlendedScaleLs;

		internal Vector3 CachedPositionWs;

		internal Vector3 CachedPositionLs;

		internal Bounds Bounds;

		internal Quaternion RotationInverseWs;

		internal Quaternion SpringRotationWs;

		internal Quaternion SpringRotationInverseWs;

		internal Quaternion CachedRotationWs;

		internal Quaternion CachedRotationLs;

		internal Quaternion BlendedRotationWs;

		internal Quaternion RotationBackPropDeltaPs;

		internal int ParentIndex;

		internal int[] ChildIndices;

		internal float LengthFromRoot;

		internal float AnimationBlend;

		internal float LengthStiffness;

		internal float LengthStiffnessT;

		internal float FullyStiffToParentLength;

		internal float PoseStiffness;

		internal float BendAngleCap;

		internal float CollisionRadius;

		internal float SquashAndStretch;

		private bool updatedPos;

		private bool updatedRot;

		private bool updatedScale;

		private Vector3 position;

		private Quaternion rotation;

		private Vector3 localScale;

		internal Vector3 Position
		{
			get
			{
				CheckResetFlags();
				if (updatedPos)
				{
					updatedPos = false;
					position = Transform.position;
				}
				return position;
			}
		}

		internal Quaternion Rotation
		{
			get
			{
				CheckResetFlags();
				if (updatedRot)
				{
					updatedRot = false;
					rotation = Transform.rotation;
				}
				return rotation;
			}
		}

		internal Vector3 LocalScale
		{
			get
			{
				CheckResetFlags();
				if (updatedScale)
				{
					updatedScale = false;
					localScale = Transform.localScale;
				}
				return localScale;
			}
		}

		private void CheckResetFlags()
		{
			if (Transform.hasChanged)
			{
				updatedPos = (updatedRot = (updatedScale = true));
				Transform.hasChanged = false;
			}
		}

		internal void UpdateBounds()
		{
			Bounds = new Bounds(Instance.PositionSpring.Value, 2f * CollisionRadius * Vector3.one);
		}

		internal Bone(Transform transform, int iParent, float lengthFromRoot)
		{
			Transform = transform;
			RotationInverseWs = Quaternion.identity;
			ParentIndex = iParent;
			LengthFromRoot = lengthFromRoot;
			Instance.Reset();
			CachedPositionWs = transform.position;
			CachedPositionLs = transform.localPosition;
			CachedRotationWs = transform.rotation;
			CachedRotationLs = transform.localRotation;
			CachedScaleLs = transform.localScale;
			AnimationBlend = 0f;
			LengthStiffness = 0f;
			PoseStiffness = 0f;
			BendAngleCap = 180f;
			CollisionRadius = 0f;
		}
	}

	[Serializable]
	public class Chain
	{
		public enum CurveType
		{
			ConstantOne,
			ConstantHalf,
			ConstantZero,
			RootOneTailHalf,
			RootOneTailZero,
			RootHalfTailOne,
			RootZeroTailOne,
			Custom
		}

		[Tooltip("Root Transform object from which to build a chain (or tree if a bone has multiple children) of bouncy boing bones.")]
		public Transform Root;

		[Tooltip("List of Transform objects to exclude from chain building.")]
		public Transform[] Exclusion;

		[Tooltip("Enable to allow reaction to boing effectors.")]
		public bool EffectorReaction = true;

		[Tooltip("Enable to allow root Transform object to be sprung around as well. Otherwise, no effects will be applied to the root Transform object.")]
		public bool LooseRoot;

		[Tooltip("Assign a SharedParamsOverride asset to override the parameters for this chain. Useful for chains using different parameters than that of the BoingBones component.")]
		public SharedBoingParams ParamsOverride;

		[ConditionalField(null, null, null, null, null, null, null, Label = "Animation Blend", Tooltip = "Animation blend determines each bone's final transform between the original raw transform and its corresponding boing bone. 1.0 means 100% contribution from raw (or animated) transform. 0.0 means 100% contribution from boing bone.\n\nEach curve type provides a type of mapping for each bone's percentage down the chain (0.0 at root & 1.0 at maximum chain length) to the bone's animation blend:\n\n - Constant One: 1.0 all the way.\n - Constant Half: 0.5 all the way.\n - Constant Zero: 0.0 all the way.\n - Root One Tail Half: 1.0 at 0% chain length and 0.5 at 100% chain length.\n - Root One Tail Zero: 1.0 at 0% chain length and 0.0 at 100% chain length.\n - Root Half Tail One: 0.5 at 0% chain length and 1.0 at 100% chain length.\n - Root Zero Tail One: 0.0 at 0% chain length and 1.0 at 100% chain length.\n - Custom: Custom curve.")]
		public CurveType AnimationBlendCurveType = CurveType.RootOneTailZero;

		[ConditionalField("AnimationBlendCurveType", CurveType.Custom, null, null, null, null, null, Label = "  Custom Curve")]
		public AnimationCurve AnimationBlendCustomCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

		[ConditionalField(null, null, null, null, null, null, null, Label = "Length Stiffness", Tooltip = "Length stiffness determines how much each target bone (target transform each boing bone is sprung towards) tries to maintain original distance from its parent. 1.0 means 100% distance maintenance. 0.0 means 0% distance maintenance.\n\nEach curve type provides a type of mapping for each bone's percentage down the chain (0.0 at root & 1.0 at maximum chain length) to the bone's length stiffness:\n\n - Constant One: 1.0 all the way.\n - Constant Half: 0.5 all the way.\n - Constant Zero: 0.0 all the way.\n - Root One Tail Half: 1.0 at 0% chain length and 0.5 at 100% chain length.\n - Root One Tail Zero: 1.0 at 0% chain length and 0.0 at 100% chain length.\n - Root Half Tail One: 0.5 at 0% chain length and 1.0 at 100% chain length.\n - Root Zero Tail One: 0.0 at 0% chain length and 1.0 at 100% chain length.\n - Custom: Custom curve.")]
		public CurveType LengthStiffnessCurveType;

		[ConditionalField("LengthStiffnessCurveType", CurveType.Custom, null, null, null, null, null, Label = "  Custom Curve")]
		public AnimationCurve LengthStiffnessCustomCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

		[ConditionalField(null, null, null, null, null, null, null, Label = "Pose Stiffness", Tooltip = "Pose stiffness determines how much each target bone (target transform each boing bone is sprung towards) tries to maintain original transform. 1.0 means 100% original transform maintenance. 0.0 means 0% original transform maintenance.\n\nEach curve type provides a type of mapping for each bone's percentage down the chain (0.0 at root & 1.0 at maximum chain length) to the bone's pose stiffness:\n\n - Constant One: 1.0 all the way.\n - Constant Half: 0.5 all the way.\n - Constant Zero: 0.0 all the way.\n - Root One Tail Half: 1.0 at 0% chain length and 0.5 at 100% chain length.\n - Root One Tail Zero: 1.0 at 0% chain length and 0.0 at 100% chain length.\n - Root Half Tail One: 0.5 at 0% chain length and 1.0 at 100% chain length.\n - Root Zero Tail One: 0.0 at 0% chain length and 1.0 at 100% chain length.\n - Custom: Custom curve.")]
		public CurveType PoseStiffnessCurveType;

		[ConditionalField("PoseStiffnessCurveType", CurveType.Custom, null, null, null, null, null, Label = "  Custom Curve")]
		public AnimationCurve PoseStiffnessCustomCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

		[ConditionalField(null, null, null, null, null, null, null, Label = "Bend Angle Cap", Tooltip = "Maximum bone bend angle cap.", Min = 0f, Max = 180f)]
		public float MaxBendAngleCap = 180f;

		[ConditionalField(null, null, null, null, null, null, null, Label = "  Curve Type", Tooltip = "Percentage(0.0 = 0 %; 1.0 = 100 %) of maximum bone bend angle cap.Bend angle cap limits how much each bone can bend relative to the root (in degrees). 1.0 means 100% maximum bend angle cap. 0.0 means 0% maximum bend angle cap.\n\nEach curve type provides a type of mapping for each bone's percentage down the chain (0.0 at root & 1.0 at maximum chain length) to the bone's pose stiffness:\n\n - Constant One: 1.0 all the way.\n - Constant Half: 0.5 all the way.\n - Constant Zero: 0.0 all the way.\n - Root One Tail Half: 1.0 at 0% chain length and 0.5 at 100% chain length.\n - Root One Tail Zero: 1.0 at 0% chain length and 0.0 at 100% chain length.\n - Root Half Tail One: 0.5 at 0% chain length and 1.0 at 100% chain length.\n - Root Zero Tail One: 0.0 at 0% chain length and 1.0 at 100% chain length.\n - Custom: Custom curve.")]
		public CurveType BendAngleCapCurveType;

		[ConditionalField("BendAngleCapCurveType", CurveType.Custom, null, null, null, null, null, Label = "    Custom Curve")]
		public AnimationCurve BendAngleCapCustomCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

		[ConditionalField(null, null, null, null, null, null, null, Label = "Collision Radius", Tooltip = "Maximum bone collision radius.")]
		public float MaxCollisionRadius = 0.1f;

		[ConditionalField(null, null, null, null, null, null, null, Label = "  Curve Type", Tooltip = "Percentage (0.0 = 0%; 1.0 = 100%) of maximum bone collision radius.\n\nEach curve type provides a type of mapping for each bone's percentage down the chain (0.0 at root & 1.0 at maximum chain length) to the bone's collision radius:\n\n - Constant One: 1.0 all the way.\n - Constant Half: 0.5 all the way.\n - Constant Zero: 0.0 all the way.\n - Root One Tail Half: 1.0 at 0% chain length and 0.5 at 100% chain length.\n - Root One Tail Zero: 1.0 at 0% chain length and 0.0 at 100% chain length.\n - Root Half Tail One: 0.5 at 0% chain length and 1.0 at 100% chain length.\n - Root Zero Tail One: 0.0 at 0% chain length and 1.0 at 100% chain length.\n - Custom: Custom curve.")]
		public CurveType CollisionRadiusCurveType;

		[ConditionalField("CollisionRadiusCurveType", CurveType.Custom, null, null, null, null, null, Label = "    Custom Curve")]
		public AnimationCurve CollisionRadiusCustomCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

		[ConditionalField(null, null, null, null, null, null, null, Label = "Boing Kit Collision", Tooltip = "Enable to allow this chain to collide with Boing Kit's own implementation of lightweight colliders")]
		public bool EnableBoingKitCollision;

		[ConditionalField(null, null, null, null, null, null, null, Label = "Unity Collision", Tooltip = "Enable to allow this chain to collide with Unity colliders.")]
		public bool EnableUnityCollision;

		[ConditionalField(null, null, null, null, null, null, null, Label = "Inter-Chain Collision", Tooltip = "Enable to allow this chain to collide with other chain (under the same BoingBones component) with inter-chain collision enabled.")]
		public bool EnableInterChainCollision;

		public Vector3 Gravity = Vector3.zero;

		internal Bounds Bounds;

		[ConditionalField(null, null, null, null, null, null, null, Label = "Squash & Stretch", Tooltip = "Percentage (0.0 = 0%; 1.0 = 100%) of each bone's squash & stretch effect. Squash & stretch is the effect of volume preservation by scaling bones based on how compressed or stretched the distances between bones become.\n\nEach curve type provides a type of mapping for each bone's percentage down the chain (0.0 at root & 1.0 at maximum chain length) to the bone's squash & stretch effect amount:\n\n - Constant One: 1.0 all the way.\n - Constant Half: 0.5 all the way.\n - Constant Zero: 0.0 all the way.\n - Root One Tail Half: 1.0 at 0% chain length and 0.5 at 100% chain length.\n - Root One Tail Zero: 1.0 at 0% chain length and 0.0 at 100% chain length.\n - Root Half Tail One: 0.5 at 0% chain length and 1.0 at 100% chain length.\n - Root Zero Tail One: 0.0 at 0% chain length and 1.0 at 100% chain length.\n - Custom: Custom curve.")]
		public CurveType SquashAndStretchCurveType = CurveType.ConstantZero;

		[ConditionalField("SquashAndStretchCurveType", CurveType.Custom, null, null, null, null, null, Label = "  Custom Curve")]
		public AnimationCurve SquashAndStretchCustomCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);

		[ConditionalField(null, null, null, null, null, null, null, Label = "  Max Squash", Tooltip = "Maximum squash amount. For example, 2.0 means a maximum scale of 200% when squashed.", Min = 1f, Max = 5f)]
		public float MaxSquash = 1.1f;

		[ConditionalField(null, null, null, null, null, null, null, Label = "  Max Stretch", Tooltip = "Maximum stretch amount. For example, 2.0 means a minimum scale of 50% when stretched (200% stretched).", Min = 1f, Max = 5f)]
		public float MaxStretch = 2f;

		internal Transform m_scannedRoot;

		internal Transform[] m_scannedExclusion;

		internal int m_hierarchyHash = -1;

		internal float MaxLengthFromRoot;

		public static float EvaluateCurve(CurveType type, float t, AnimationCurve curve)
		{
			return type switch
			{
				CurveType.ConstantOne => 1f, 
				CurveType.ConstantHalf => 0.5f, 
				CurveType.ConstantZero => 0f, 
				CurveType.RootOneTailHalf => 1f - 0.5f * Mathf.Clamp01(t), 
				CurveType.RootOneTailZero => 1f - Mathf.Clamp01(t), 
				CurveType.RootHalfTailOne => 0.5f + 0.5f * Mathf.Clamp01(t), 
				CurveType.RootZeroTailOne => Mathf.Clamp01(t), 
				CurveType.Custom => curve.Evaluate(t), 
				_ => 0f, 
			};
		}
	}

	private class RescanEntry
	{
		internal Transform Transform;

		internal int ParentIndex;

		internal float LengthFromRoot;

		internal RescanEntry(Transform transform, int iParent, float lengthFromRoot)
		{
			Transform = transform;
			ParentIndex = iParent;
			LengthFromRoot = lengthFromRoot;
		}
	}

	[SerializeField]
	internal Bone[][] BoneData;

	public Chain[] BoneChains = new Chain[1];

	public bool TwistPropagation = true;

	[Range(0.1f, 20f)]
	public float MaxCollisionResolutionSpeed = 3f;

	public BoingBoneCollider[] BoingColliders = new BoingBoneCollider[0];

	public Collider[] UnityColliders = new Collider[0];

	public bool DebugDrawRawBones;

	public bool DebugDrawTargetBones;

	public bool DebugDrawBoingBones;

	public bool DebugDrawFinalBones;

	public bool DebugDrawColliders;

	public bool DebugDrawChainBounds;

	public bool DebugDrawBoneNames;

	public bool DebugDrawLengthFromRoot;

	private float m_minScale = 1f;

	internal float MinScale => m_minScale;

	protected override void Register()
	{
		BoingManager.Register(this);
	}

	protected override void Unregister()
	{
		BoingManager.Unregister(this);
	}

	protected override void OnUpgrade(Version oldVersion, Version newVersion)
	{
		base.OnUpgrade(oldVersion, newVersion);
		if (oldVersion.Revision < 33)
		{
			TwistPropagation = false;
		}
	}

	public void OnValidate()
	{
		RescanBoneChains();
		UpdateCollisionRadius();
	}

	public override void OnEnable()
	{
		base.OnEnable();
		RescanBoneChains();
		Reboot();
	}

	public override void OnDisable()
	{
		base.OnDisable();
		Restore();
	}

	public void RescanBoneChains()
	{
		if (BoneChains == null)
		{
			return;
		}
		int num = BoneChains.Length;
		if (BoneData == null || BoneData.Length != num)
		{
			Bone[][] array = new Bone[num][];
			if (BoneData != null)
			{
				int i = 0;
				for (int num2 = Mathf.Min(BoneData.Length, num); i < num2; i++)
				{
					array[i] = BoneData[i];
				}
			}
			BoneData = array;
		}
		Queue<RescanEntry> queue = new Queue<RescanEntry>();
		for (int j = 0; j < num; j++)
		{
			Chain chain = BoneChains[j];
			bool flag = false;
			if (BoneData[j] == null)
			{
				flag = true;
			}
			if (!flag && chain.m_scannedRoot == null)
			{
				flag = true;
			}
			if (!flag && chain.m_scannedRoot != chain.Root)
			{
				flag = true;
			}
			if (!flag && chain.m_scannedExclusion != null != (chain.Exclusion != null))
			{
				flag = true;
			}
			if (!flag && chain.Exclusion != null)
			{
				if (chain.m_scannedExclusion.Length != chain.Exclusion.Length)
				{
					flag = true;
				}
				else
				{
					for (int k = 0; k < chain.m_scannedExclusion.Length; k++)
					{
						if (!(chain.m_scannedExclusion[k] == chain.Exclusion[k]))
						{
							flag = true;
							break;
						}
					}
				}
			}
			Transform transform = chain?.Root;
			int num3 = ((transform != null) ? Codec.HashTransformHierarchy(transform) : (-1));
			if (!flag && transform != null && chain.m_hierarchyHash != num3)
			{
				flag = true;
			}
			if (!flag)
			{
				continue;
			}
			if (transform == null)
			{
				BoneData[j] = null;
				continue;
			}
			chain.m_scannedRoot = chain.Root;
			chain.m_scannedExclusion = chain.Exclusion.ToArray();
			chain.m_hierarchyHash = num3;
			chain.MaxLengthFromRoot = 0f;
			List<Bone> list = new List<Bone>();
			queue.Enqueue(new RescanEntry(transform, -1, 0f));
			while (queue.Count > 0)
			{
				RescanEntry rescanEntry = queue.Dequeue();
				if (Enumerable.Contains(chain.Exclusion, rescanEntry.Transform))
				{
					continue;
				}
				int count = list.Count;
				Transform transform2 = rescanEntry.Transform;
				int[] array2 = new int[transform2.childCount];
				for (int l = 0; l < array2.Length; l++)
				{
					array2[l] = -1;
				}
				int num4 = 0;
				int m = 0;
				for (int childCount = transform2.childCount; m < childCount; m++)
				{
					Transform child = transform2.GetChild(m);
					if (!Enumerable.Contains(chain.Exclusion, child))
					{
						float num5 = Vector3.Distance(rescanEntry.Transform.position, child.position);
						float lengthFromRoot = rescanEntry.LengthFromRoot + num5;
						queue.Enqueue(new RescanEntry(child, count, lengthFromRoot));
						num4++;
					}
				}
				chain.MaxLengthFromRoot = Mathf.Max(rescanEntry.LengthFromRoot, chain.MaxLengthFromRoot);
				Bone bone = new Bone(transform2, rescanEntry.ParentIndex, rescanEntry.LengthFromRoot);
				if (num4 > 0)
				{
					bone.ChildIndices = array2;
				}
				list.Add(bone);
			}
			for (int n = 0; n < list.Count; n++)
			{
				Bone bone2 = list[n];
				if (bone2.ParentIndex >= 0)
				{
					Bone bone3 = list[bone2.ParentIndex];
					int num6;
					for (num6 = 0; bone3.ChildIndices[num6] >= 0; num6++)
					{
					}
					if (num6 < bone3.ChildIndices.Length)
					{
						bone3.ChildIndices[num6] = n;
					}
				}
			}
			if (list.Count != 0)
			{
				float num7 = MathUtil.InvSafe(chain.MaxLengthFromRoot);
				for (int num8 = 0; num8 < list.Count; num8++)
				{
					Bone bone4 = list[num8];
					float t = Mathf.Clamp01(bone4.LengthFromRoot * num7);
					bone4.CollisionRadius = chain.MaxCollisionRadius * Chain.EvaluateCurve(chain.CollisionRadiusCurveType, t, chain.CollisionRadiusCustomCurve);
				}
				BoneData[j] = list.ToArray();
				Reboot(j);
			}
		}
	}

	private void UpdateCollisionRadius()
	{
		for (int i = 0; i < BoneData.Length; i++)
		{
			Chain chain = BoneChains[i];
			Bone[] array = BoneData[i];
			if (array != null)
			{
				float num = MathUtil.InvSafe(chain.MaxLengthFromRoot);
				foreach (Bone obj in array)
				{
					float t = Mathf.Clamp01(obj.LengthFromRoot * num);
					obj.CollisionRadius = chain.MaxCollisionRadius * Chain.EvaluateCurve(chain.CollisionRadiusCurveType, t, chain.CollisionRadiusCustomCurve);
				}
			}
		}
	}

	public override void Reboot()
	{
		base.Reboot();
		for (int i = 0; i < BoneData.Length; i++)
		{
			Reboot(i);
		}
	}

	public void Reboot(int iChain)
	{
		Bone[] array = BoneData[iChain];
		if (array != null)
		{
			foreach (Bone bone in array)
			{
				bone.Instance.PositionSpring.Reset(bone.Position);
				bone.Instance.RotationSpring.Reset(bone.Rotation);
				bone.CachedPositionWs = bone.Position;
				bone.CachedPositionLs = bone.Transform.localPosition;
				bone.CachedRotationWs = bone.Rotation;
				bone.CachedRotationLs = bone.Transform.localRotation;
				bone.CachedScaleLs = bone.LocalScale;
			}
			CachedTransformValid = true;
		}
	}

	public override void PrepareExecute()
	{
		base.PrepareExecute();
		Params.Bits.SetBit(4, value: false);
		float fixedDeltaTime = Time.fixedDeltaTime;
		float num = ((UpdateMode == BoingManager.UpdateMode.FixedUpdate) ? fixedDeltaTime : Time.deltaTime);
		m_minScale = Mathf.Min(base.transform.localScale.x, Mathf.Min(base.transform.localScale.y, base.transform.localScale.z));
		for (int i = 0; i < BoneData.Length; i++)
		{
			Chain chain = BoneChains[i];
			Bone[] array = BoneData[i];
			if (array == null || chain.Root == null || array.Length == 0)
			{
				continue;
			}
			Vector3 vector = chain.Gravity * num;
			float num2 = 0f;
			foreach (Bone bone in array)
			{
				if (bone.ParentIndex < 0)
				{
					if (!chain.LooseRoot)
					{
						bone.Instance.PositionSpring.Reset(bone.Position);
						bone.Instance.RotationSpring.Reset(bone.Rotation);
					}
					bone.LengthFromRoot = 0f;
				}
				else
				{
					Bone bone2 = array[bone.ParentIndex];
					float num3 = Vector3.Distance(bone.Position, bone2.Position);
					bone.LengthFromRoot = bone2.LengthFromRoot + num3;
					num2 = Mathf.Max(num2, bone.LengthFromRoot);
				}
			}
			float num4 = MathUtil.InvSafe(num2);
			foreach (Bone bone3 in array)
			{
				float t = bone3.LengthFromRoot * num4;
				bone3.AnimationBlend = Chain.EvaluateCurve(chain.AnimationBlendCurveType, t, chain.AnimationBlendCustomCurve);
				bone3.LengthStiffness = Chain.EvaluateCurve(chain.LengthStiffnessCurveType, t, chain.LengthStiffnessCustomCurve);
				bone3.LengthStiffnessT = 1f - Mathf.Pow(1f - bone3.LengthStiffness, 30f * fixedDeltaTime);
				bone3.FullyStiffToParentLength = ((bone3.ParentIndex >= 0) ? Vector3.Distance(array[bone3.ParentIndex].Position, bone3.Position) : 0f);
				bone3.PoseStiffness = Chain.EvaluateCurve(chain.PoseStiffnessCurveType, t, chain.PoseStiffnessCustomCurve);
				bone3.BendAngleCap = chain.MaxBendAngleCap * MathUtil.Deg2Rad * Chain.EvaluateCurve(chain.BendAngleCapCurveType, t, chain.BendAngleCapCustomCurve);
				bone3.CollisionRadius = chain.MaxCollisionRadius * Chain.EvaluateCurve(chain.CollisionRadiusCurveType, t, chain.CollisionRadiusCustomCurve);
				bone3.SquashAndStretch = Chain.EvaluateCurve(chain.SquashAndStretchCurveType, t, chain.SquashAndStretchCustomCurve);
			}
			Vector3 position = array[0].Position;
			for (int l = 0; l < array.Length; l++)
			{
				Bone bone4 = array[l];
				float t2 = bone4.LengthFromRoot * num4;
				bone4.AnimationBlend = Chain.EvaluateCurve(chain.AnimationBlendCurveType, t2, chain.AnimationBlendCustomCurve);
				bone4.LengthStiffness = Chain.EvaluateCurve(chain.LengthStiffnessCurveType, t2, chain.LengthStiffnessCustomCurve);
				bone4.PoseStiffness = Chain.EvaluateCurve(chain.PoseStiffnessCurveType, t2, chain.PoseStiffnessCustomCurve);
				bone4.BendAngleCap = chain.MaxBendAngleCap * MathUtil.Deg2Rad * Chain.EvaluateCurve(chain.BendAngleCapCurveType, t2, chain.BendAngleCapCustomCurve);
				bone4.CollisionRadius = chain.MaxCollisionRadius * Chain.EvaluateCurve(chain.CollisionRadiusCurveType, t2, chain.CollisionRadiusCustomCurve);
				bone4.SquashAndStretch = Chain.EvaluateCurve(chain.SquashAndStretchCurveType, t2, chain.SquashAndStretchCustomCurve);
				if (l > 0)
				{
					bone4.Instance.PositionSpring.Velocity += vector;
				}
				bone4.RotationInverseWs = Quaternion.Inverse(bone4.Rotation);
				bone4.SpringRotationWs = bone4.Instance.RotationSpring.ValueQuat;
				bone4.SpringRotationInverseWs = Quaternion.Inverse(bone4.SpringRotationWs);
				Vector3 vector2 = bone4.Position;
				Quaternion rotation = bone4.Rotation;
				Vector3 localScale = bone4.LocalScale;
				if (bone4.ParentIndex >= 0)
				{
					Bone bone5 = array[bone4.ParentIndex];
					Vector3 position2 = bone5.Position;
					Vector3 value = bone5.Instance.PositionSpring.Value;
					Vector3 a = bone5.SpringRotationInverseWs * (bone4.Instance.PositionSpring.Value - value);
					Quaternion a2 = bone5.SpringRotationInverseWs * bone4.Instance.RotationSpring.ValueQuat;
					Vector3 position3 = bone4.Position;
					Quaternion rotation2 = bone4.Rotation;
					Vector3 b = bone5.RotationInverseWs * (position3 - position2);
					Quaternion b2 = bone5.RotationInverseWs * rotation2;
					float poseStiffness = bone4.PoseStiffness;
					Vector3 vector3 = Vector3.Lerp(a, b, poseStiffness);
					Quaternion quaternion = Quaternion.Slerp(a2, b2, poseStiffness);
					vector2 = value + bone5.SpringRotationWs * vector3;
					rotation = bone5.SpringRotationWs * quaternion;
					if (bone4.BendAngleCap < MathUtil.Pi - MathUtil.Epsilon)
					{
						Vector3 vector4 = vector2 - position;
						vector4 = VectorUtil.ClampBend(vector4, position3 - position, bone4.BendAngleCap);
						vector2 = position + vector4;
					}
				}
				if (chain.ParamsOverride == null)
				{
					bone4.Instance.PrepareExecute(ref Params, vector2, rotation, localScale, accumulateEffectors: true);
				}
				else
				{
					bone4.Instance.PrepareExecute(ref chain.ParamsOverride.Params, vector2, rotation, localScale, accumulateEffectors: true);
				}
			}
		}
	}

	public void AccumulateTarget(ref BoingEffector.Params effector, float dt)
	{
		for (int i = 0; i < BoneData.Length; i++)
		{
			Chain chain = BoneChains[i];
			Bone[] array = BoneData[i];
			if (array == null || !chain.EffectorReaction)
			{
				continue;
			}
			Bone[] array2 = array;
			foreach (Bone bone in array2)
			{
				if (chain.ParamsOverride == null)
				{
					bone.Instance.AccumulateTarget(ref Params, ref effector, dt);
					continue;
				}
				Bits32 bits = chain.ParamsOverride.Params.Bits;
				chain.ParamsOverride.Params.Bits = Params.Bits;
				bone.Instance.AccumulateTarget(ref chain.ParamsOverride.Params, ref effector, dt);
				chain.ParamsOverride.Params.Bits = bits;
			}
		}
	}

	public void EndAccumulateTargets()
	{
		for (int i = 0; i < BoneData.Length; i++)
		{
			Chain chain = BoneChains[i];
			Bone[] array = BoneData[i];
			if (array == null)
			{
				continue;
			}
			foreach (Bone bone in array)
			{
				if (chain.ParamsOverride == null)
				{
					bone.Instance.EndAccumulateTargets(ref Params);
				}
				else
				{
					bone.Instance.EndAccumulateTargets(ref chain.ParamsOverride.Params);
				}
			}
		}
	}

	public override void Restore()
	{
		if (!CachedTransformValid)
		{
			return;
		}
		for (int i = 0; i < BoneData.Length; i++)
		{
			Chain chain = BoneChains[i];
			Bone[] array = BoneData[i];
			if (array == null)
			{
				continue;
			}
			for (int j = 0; j < array.Length; j++)
			{
				Bone bone = array[j];
				if (j != 0 || chain.LooseRoot)
				{
					bone.Transform.SetLocalPositionAndRotation(bone.CachedPositionLs, bone.CachedRotationLs);
					bone.Transform.localScale = bone.CachedScaleLs;
				}
			}
		}
	}
}
