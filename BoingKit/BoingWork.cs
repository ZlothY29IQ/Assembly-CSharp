using System;
using System.Collections.Generic;
using UnityEngine;

namespace BoingKit;

public static class BoingWork
{
	public enum EffectorFlags
	{
		ContinuousMotion
	}

	public enum ReactorFlags
	{
		TwoDDistanceCheck,
		TwoDPositionInfluence,
		TwoDRotationInfluence,
		EnablePositionEffect,
		EnableRotationEffect,
		EnableScaleEffect,
		GlobalReactionUpVector,
		EnablePropagation,
		AnchorPropagationAtBorder,
		FixedUpdate,
		EarlyUpdate,
		LateUpdate
	}

	[Serializable]
	public struct Params
	{
		public struct InstanceData
		{
			public static readonly int Stride = 144 + 2 * Vector3Spring.Stride + QuaternionSpring.Stride;

			public Vector3 PositionTarget;

			private float m_padding0;

			public Vector3 PositionOrigin;

			private float m_padding1;

			public Vector4 RotationTarget;

			public Vector4 RotationOrigin;

			public Vector3 ScaleTarget;

			private float m_padding2;

			private int m_numEffectors;

			private int m_instantAccumulation;

			private int m_padding3;

			private int m_padding4;

			private Vector3 m_upWs;

			private float m_minScale;

			public Vector3Spring PositionSpring;

			public QuaternionSpring RotationSpring;

			public Vector3Spring ScaleSpring;

			public Vector3 PositionPropagationWorkData;

			private float m_padding5;

			public Vector4 RotationPropagationWorkData;

			public void Reset()
			{
				PositionSpring.Reset();
				RotationSpring.Reset();
				ScaleSpring.Reset(Vector3.one, Vector3.zero);
				PositionPropagationWorkData = Vector3.zero;
				RotationPropagationWorkData = Vector3.zero;
			}

			public void Reset(Vector3 position, bool instantAccumulation)
			{
				PositionSpring.Reset(position);
				RotationSpring.Reset();
				ScaleSpring.Reset(Vector3.one, Vector3.zero);
				PositionPropagationWorkData = Vector3.zero;
				RotationPropagationWorkData = Vector3.zero;
				m_instantAccumulation = (instantAccumulation ? 1 : 0);
			}

			public void PrepareExecute(ref Params p, Vector3 position, Quaternion rotation, Vector3 scale, bool accumulateEffectors)
			{
				PositionTarget = (PositionOrigin = position);
				RotationTarget = (RotationOrigin = QuaternionUtil.ToVector4(rotation));
				ScaleTarget = scale;
				m_minScale = VectorUtil.MinComponent(scale);
				if (accumulateEffectors)
				{
					PositionTarget = Vector3.zero;
					RotationTarget = Vector4.zero;
					m_numEffectors = 0;
					m_upWs = (p.Bits.IsBitSet(6) ? p.RotationReactionUp : (rotation * VectorUtil.NormalizeSafe(p.RotationReactionUp, Vector3.up)));
				}
				else
				{
					m_numEffectors = -1;
					m_upWs = Vector3.zero;
				}
			}

			public void PrepareExecute(ref Params p, Vector3 gridCenter, Quaternion gridRotation, Vector3 cellOffset)
			{
				PositionOrigin = gridCenter + cellOffset;
				RotationOrigin = QuaternionUtil.ToVector4(Quaternion.identity);
				PositionTarget = Vector3.zero;
				RotationTarget = Vector4.zero;
				m_numEffectors = 0;
				m_upWs = (p.Bits.IsBitSet(6) ? p.RotationReactionUp : (gridRotation * VectorUtil.NormalizeSafe(p.RotationReactionUp, Vector3.up)));
				m_minScale = 1f;
			}

			public void AccumulateTarget(ref Params p, ref BoingEffector.Params effector, float dt)
			{
				Vector3 vector = (effector.Bits.IsBitSet(0) ? VectorUtil.GetClosestPointOnSegment(PositionOrigin, effector.PrevPosition, effector.CurrPosition) : effector.CurrPosition);
				Vector3 vector2 = PositionOrigin - vector;
				Vector3 vector3 = vector2;
				if (p.Bits.IsBitSet(0))
				{
					switch (p.TwoDPlane)
					{
					case TwoDPlaneEnum.XY:
						vector2.z = 0f;
						break;
					case TwoDPlaneEnum.XZ:
						vector2.y = 0f;
						break;
					case TwoDPlaneEnum.YZ:
						vector2.x = 0f;
						break;
					}
				}
				if (!(Mathf.Abs(vector2.x) <= effector.Radius) || !(Mathf.Abs(vector2.y) <= effector.Radius) || !(Mathf.Abs(vector2.z) <= effector.Radius) || !(vector2.sqrMagnitude <= effector.Radius * effector.Radius))
				{
					return;
				}
				float magnitude = vector2.magnitude;
				float num = ((effector.Radius - effector.FullEffectRadius > MathUtil.Epsilon) ? (1f - Mathf.Clamp01((magnitude - effector.FullEffectRadius) / (effector.Radius - effector.FullEffectRadius))) : 1f);
				Vector3 vector4 = m_upWs;
				Vector3 vector5 = m_upWs;
				Vector3 vector6 = VectorUtil.NormalizeSafe(vector3, m_upWs);
				Vector3 vector7 = vector6;
				if (p.Bits.IsBitSet(1))
				{
					switch (p.TwoDPlane)
					{
					case TwoDPlaneEnum.XY:
						vector6.z = 0f;
						vector4.z = 0f;
						break;
					case TwoDPlaneEnum.XZ:
						vector6.y = 0f;
						vector4.y = 0f;
						break;
					case TwoDPlaneEnum.YZ:
						vector6.x = 0f;
						vector4.x = 0f;
						break;
					}
					if (vector4.sqrMagnitude < MathUtil.Epsilon)
					{
						switch (p.TwoDPlane)
						{
						case TwoDPlaneEnum.XY:
							vector4 = Vector3.up;
							break;
						case TwoDPlaneEnum.XZ:
							vector4 = Vector3.forward;
							break;
						case TwoDPlaneEnum.YZ:
							vector4 = Vector3.up;
							break;
						}
					}
					else
					{
						vector4.Normalize();
					}
					vector6 = VectorUtil.NormalizeSafe(vector6, vector4);
				}
				if (p.Bits.IsBitSet(2))
				{
					switch (p.TwoDPlane)
					{
					case TwoDPlaneEnum.XY:
						vector7.z = 0f;
						vector5.z = 0f;
						break;
					case TwoDPlaneEnum.XZ:
						vector7.y = 0f;
						vector5.y = 0f;
						break;
					case TwoDPlaneEnum.YZ:
						vector7.x = 0f;
						vector5.x = 0f;
						break;
					}
					if (vector5.sqrMagnitude < MathUtil.Epsilon)
					{
						switch (p.TwoDPlane)
						{
						case TwoDPlaneEnum.XY:
							vector5 = Vector3.up;
							break;
						case TwoDPlaneEnum.XZ:
							vector5 = Vector3.forward;
							break;
						case TwoDPlaneEnum.YZ:
							vector5 = Vector3.up;
							break;
						}
					}
					else
					{
						vector5.Normalize();
					}
					vector7 = VectorUtil.NormalizeSafe(vector7, vector5);
				}
				if (p.Bits.IsBitSet(3))
				{
					Vector3 vector8 = num * p.MoveReactionMultiplier * effector.MoveDistance * vector6;
					PositionTarget += vector8;
					PositionSpring.Velocity += num * p.LinearImpulseMultiplier * effector.LinearImpulse * effector.LinearVelocityDir * (60f * dt);
				}
				if (p.Bits.IsBitSet(4))
				{
					Vector3 vector9 = VectorUtil.NormalizeSafe(Vector3.Cross(vector5, vector7), VectorUtil.FindOrthogonal(vector5));
					Vector3 v = num * p.RotationReactionMultiplier * effector.RotateAngle * vector9;
					RotationTarget += QuaternionUtil.ToVector4(QuaternionUtil.FromAngularVector(v));
					Vector3 v2 = VectorUtil.NormalizeSafe(Vector3.Cross(effector.LinearVelocityDir, vector7 - 0.01f * Vector3.up), vector9);
					float num2 = num * p.AngularImpulseMultiplier * effector.AngularImpulse * (60f * dt);
					Vector4 vector10 = QuaternionUtil.ToVector4(QuaternionUtil.FromAngularVector(v2));
					RotationSpring.VelocityVec += num2 * vector10;
				}
				m_numEffectors++;
			}

			public void EndAccumulateTargets(ref Params p)
			{
				if (m_numEffectors > 0)
				{
					PositionTarget *= m_minScale / (float)m_numEffectors;
					PositionTarget += PositionOrigin;
					RotationTarget /= (float)m_numEffectors;
					RotationTarget = QuaternionUtil.ToVector4(QuaternionUtil.FromVector4(RotationTarget) * QuaternionUtil.FromVector4(RotationOrigin));
				}
				else
				{
					PositionTarget = PositionOrigin;
					RotationTarget = RotationOrigin;
				}
			}

			public void Execute(ref Params p, float dt)
			{
				bool flag = m_numEffectors >= 0;
				bool flag2 = ((!flag) ? p.Bits.IsBitSet(3) : (PositionSpring.Velocity.sqrMagnitude > MathUtil.Epsilon || (PositionSpring.Value - PositionTarget).sqrMagnitude > MathUtil.Epsilon));
				bool flag3 = ((!flag) ? p.Bits.IsBitSet(4) : (RotationSpring.VelocityVec.sqrMagnitude > MathUtil.Epsilon || (RotationSpring.ValueVec - RotationTarget).sqrMagnitude > MathUtil.Epsilon));
				bool flag4 = p.Bits.IsBitSet(5) && (ScaleSpring.Value - ScaleTarget).sqrMagnitude > MathUtil.Epsilon;
				if (m_numEffectors == 0)
				{
					bool flag5 = true;
					if (flag2)
					{
						flag5 = false;
					}
					else
					{
						PositionSpring.Reset(PositionTarget);
					}
					if (flag3)
					{
						flag5 = false;
					}
					else
					{
						RotationSpring.Reset(QuaternionUtil.FromVector4(RotationTarget));
					}
					if (flag5)
					{
						return;
					}
				}
				if (m_instantAccumulation != 0)
				{
					PositionSpring.Value = PositionTarget;
					RotationSpring.ValueVec = RotationTarget;
					ScaleSpring.Value = ScaleTarget;
					m_instantAccumulation = 0;
				}
				else
				{
					if (flag2)
					{
						switch (p.PositionParameterMode)
						{
						case ParameterMode.Exponential:
							PositionSpring.TrackExponential(PositionTarget, p.PositionExponentialHalfLife, dt);
							break;
						case ParameterMode.OscillationByHalfLife:
							PositionSpring.TrackHalfLife(PositionTarget, p.PositionOscillationFrequency, p.PositionOscillationHalfLife, dt);
							break;
						case ParameterMode.OscillationByDampingRatio:
							PositionSpring.TrackDampingRatio(PositionTarget, p.PositionOscillationFrequency * MathUtil.TwoPi, p.PositionOscillationDampingRatio, dt);
							break;
						}
					}
					else
					{
						PositionSpring.Value = PositionTarget;
						PositionSpring.Velocity = Vector3.zero;
					}
					if (flag3)
					{
						switch (p.RotationParameterMode)
						{
						case ParameterMode.Exponential:
							RotationSpring.TrackExponential(RotationTarget, p.RotationExponentialHalfLife, dt);
							break;
						case ParameterMode.OscillationByHalfLife:
							RotationSpring.TrackHalfLife(RotationTarget, p.RotationOscillationFrequency, p.RotationOscillationHalfLife, dt);
							break;
						case ParameterMode.OscillationByDampingRatio:
							RotationSpring.TrackDampingRatio(RotationTarget, p.RotationOscillationFrequency * MathUtil.TwoPi, p.RotationOscillationDampingRatio, dt);
							break;
						}
					}
					else
					{
						RotationSpring.ValueVec = RotationTarget;
						RotationSpring.VelocityVec = Vector4.zero;
					}
					if (flag4)
					{
						switch (p.ScaleParameterMode)
						{
						case ParameterMode.Exponential:
							ScaleSpring.TrackExponential(ScaleTarget, p.ScaleExponentialHalfLife, dt);
							break;
						case ParameterMode.OscillationByHalfLife:
							ScaleSpring.TrackHalfLife(ScaleTarget, p.ScaleOscillationFrequency, p.ScaleOscillationHalfLife, dt);
							break;
						case ParameterMode.OscillationByDampingRatio:
							ScaleSpring.TrackDampingRatio(ScaleTarget, p.ScaleOscillationFrequency * MathUtil.TwoPi, p.ScaleOscillationDampingRatio, dt);
							break;
						}
					}
					else
					{
						ScaleSpring.Value = ScaleTarget;
						ScaleSpring.Velocity = Vector3.zero;
					}
				}
				if (!flag)
				{
					if (!flag2)
					{
						PositionSpring.Reset(PositionTarget);
					}
					if (!flag3)
					{
						RotationSpring.Reset(RotationTarget);
					}
				}
			}

			public void PullResults(BoingBones bones)
			{
				for (int i = 0; i < bones.BoneData.Length; i++)
				{
					BoingBones.Chain chain = bones.BoneChains[i];
					BoingBones.Bone[] array = bones.BoneData[i];
					if (array == null)
					{
						continue;
					}
					BoingBones.Bone[] array2 = array;
					foreach (BoingBones.Bone obj in array2)
					{
						obj.CachedPositionWs = obj.Position;
						obj.CachedPositionLs = obj.Transform.localPosition;
						obj.CachedRotationWs = obj.Rotation;
						obj.CachedRotationLs = obj.Transform.localRotation;
						obj.CachedScaleLs = obj.LocalScale;
					}
					for (int k = 0; k < array.Length; k++)
					{
						BoingBones.Bone bone = array[k];
						if (k == 0 && !chain.LooseRoot)
						{
							bone.BlendedPositionWs = bone.CachedPositionWs;
						}
						else
						{
							bone.BlendedPositionWs = Vector3.Lerp(bone.Instance.PositionSpring.Value, bone.CachedPositionWs, bone.AnimationBlend);
						}
					}
					for (int l = 0; l < array.Length; l++)
					{
						BoingBones.Bone bone2 = array[l];
						if (l == 0 && !chain.LooseRoot)
						{
							bone2.BlendedRotationWs = bone2.CachedRotationWs;
							continue;
						}
						if (bone2.ChildIndices == null)
						{
							if (bone2.ParentIndex >= 0)
							{
								BoingBones.Bone bone3 = array[bone2.ParentIndex];
								bone2.BlendedRotationWs = bone3.BlendedRotationWs * (bone3.RotationInverseWs * bone2.CachedRotationWs);
							}
							continue;
						}
						Vector3 cachedPositionWs = bone2.CachedPositionWs;
						Vector3 vector = ComputeTranslationalResults(bone2.Transform, cachedPositionWs, bone2.BlendedPositionWs, bones);
						Quaternion quaternion = (bones.TwistPropagation ? bone2.SpringRotationWs : bone2.CachedRotationWs);
						Quaternion quaternion2 = Quaternion.Inverse(quaternion);
						if (!bones.EnableRotationEffect)
						{
							continue;
						}
						Vector4 vector2 = Vector3.zero;
						float num = 0f;
						int[] childIndices = bone2.ChildIndices;
						foreach (int num2 in childIndices)
						{
							if (num2 >= 0)
							{
								BoingBones.Bone bone4 = array[num2];
								Vector3 cachedPositionWs2 = bone4.CachedPositionWs;
								Vector3 fromDirection = VectorUtil.NormalizeSafe(cachedPositionWs2 - cachedPositionWs, Vector3.zero);
								Vector3 toDirection = VectorUtil.NormalizeSafe(ComputeTranslationalResults(bone4.Transform, cachedPositionWs2, bone4.BlendedPositionWs, bones) - vector, Vector3.zero);
								Quaternion quaternion3 = Quaternion.FromToRotation(fromDirection, toDirection);
								Vector4 vector3 = QuaternionUtil.ToVector4(quaternion2 * quaternion3);
								float num3 = Mathf.Max(MathUtil.Epsilon, chain.MaxLengthFromRoot - bone4.LengthFromRoot);
								vector2 += num3 * vector3;
								num += num3;
							}
						}
						if (num > 0f)
						{
							Vector4 v = vector2 / num;
							bone2.RotationBackPropDeltaPs = QuaternionUtil.FromVector4(v);
							bone2.BlendedRotationWs = quaternion * bone2.RotationBackPropDeltaPs * quaternion;
						}
						else if (bone2.ParentIndex >= 0)
						{
							BoingBones.Bone bone5 = array[bone2.ParentIndex];
							bone2.BlendedRotationWs = bone5.BlendedRotationWs * (bone5.RotationInverseWs * quaternion);
						}
					}
					for (int m = 0; m < array.Length; m++)
					{
						BoingBones.Bone bone6 = array[m];
						if (m == 0 && !chain.LooseRoot)
						{
							bone6.Instance.PositionSpring.Reset(bone6.CachedPositionWs);
							bone6.Instance.RotationSpring.Reset(bone6.CachedRotationWs);
						}
						else
						{
							bone6.Transform.SetPositionAndRotation(ComputeTranslationalResults(bone6.Transform, bone6.Position, bone6.BlendedPositionWs, bones), bone6.BlendedRotationWs);
							bone6.Transform.localScale = bone6.BlendedScaleLs;
						}
					}
				}
			}

			private void SuppressWarnings()
			{
				m_padding0 = 0f;
				m_padding1 = 0f;
				m_padding2 = 0f;
				m_padding3 = 0;
				m_padding4 = 0;
				m_padding5 = 0f;
				m_padding0 = m_padding1;
				m_padding1 = m_padding2;
				m_padding2 = m_padding3;
				m_padding3 = m_padding4;
				m_padding4 = (int)m_padding0;
				m_padding5 = m_padding0;
			}
		}

		public static readonly int Stride = 112 + InstanceData.Stride;

		public int InstanceID;

		public Bits32 Bits;

		public TwoDPlaneEnum TwoDPlane;

		private int m_padding0;

		public ParameterMode PositionParameterMode;

		public ParameterMode RotationParameterMode;

		public ParameterMode ScaleParameterMode;

		private int m_padding1;

		[Range(0f, 5f)]
		public float PositionExponentialHalfLife;

		[Range(0f, 5f)]
		public float PositionOscillationHalfLife;

		[Range(0f, 10f)]
		public float PositionOscillationFrequency;

		[Range(0f, 1f)]
		public float PositionOscillationDampingRatio;

		[Range(0f, 10f)]
		public float MoveReactionMultiplier;

		[Range(0f, 10f)]
		public float LinearImpulseMultiplier;

		[Range(0f, 5f)]
		public float RotationExponentialHalfLife;

		[Range(0f, 5f)]
		public float RotationOscillationHalfLife;

		[Range(0f, 10f)]
		public float RotationOscillationFrequency;

		[Range(0f, 1f)]
		public float RotationOscillationDampingRatio;

		[Range(0f, 10f)]
		public float RotationReactionMultiplier;

		[Range(0f, 10f)]
		public float AngularImpulseMultiplier;

		[Range(0f, 5f)]
		public float ScaleExponentialHalfLife;

		[Range(0f, 5f)]
		public float ScaleOscillationHalfLife;

		[Range(0f, 10f)]
		public float ScaleOscillationFrequency;

		[Range(0f, 1f)]
		public float ScaleOscillationDampingRatio;

		public Vector3 RotationReactionUp;

		private float m_padding2;

		public InstanceData Instance;

		public static void Copy(ref Params from, ref Params to)
		{
			to.PositionParameterMode = from.PositionParameterMode;
			to.RotationParameterMode = from.RotationParameterMode;
			to.PositionExponentialHalfLife = from.PositionExponentialHalfLife;
			to.PositionOscillationHalfLife = from.PositionOscillationHalfLife;
			to.PositionOscillationFrequency = from.PositionOscillationFrequency;
			to.PositionOscillationDampingRatio = from.PositionOscillationDampingRatio;
			to.MoveReactionMultiplier = from.MoveReactionMultiplier;
			to.LinearImpulseMultiplier = from.LinearImpulseMultiplier;
			to.RotationExponentialHalfLife = from.RotationExponentialHalfLife;
			to.RotationOscillationHalfLife = from.RotationOscillationHalfLife;
			to.RotationOscillationFrequency = from.RotationOscillationFrequency;
			to.RotationOscillationDampingRatio = from.RotationOscillationDampingRatio;
			to.RotationReactionMultiplier = from.RotationReactionMultiplier;
			to.AngularImpulseMultiplier = from.AngularImpulseMultiplier;
			to.ScaleExponentialHalfLife = from.ScaleExponentialHalfLife;
			to.ScaleOscillationHalfLife = from.ScaleOscillationHalfLife;
			to.ScaleOscillationFrequency = from.ScaleOscillationFrequency;
			to.ScaleOscillationDampingRatio = from.ScaleOscillationDampingRatio;
		}

		public void Init()
		{
			InstanceID = -1;
			Bits.Clear();
			TwoDPlane = TwoDPlaneEnum.XZ;
			PositionParameterMode = ParameterMode.OscillationByHalfLife;
			RotationParameterMode = ParameterMode.OscillationByHalfLife;
			ScaleParameterMode = ParameterMode.OscillationByHalfLife;
			PositionExponentialHalfLife = 0.02f;
			PositionOscillationHalfLife = 0.1f;
			PositionOscillationFrequency = 5f;
			PositionOscillationDampingRatio = 0.5f;
			MoveReactionMultiplier = 1f;
			LinearImpulseMultiplier = 1f;
			RotationExponentialHalfLife = 0.02f;
			RotationOscillationHalfLife = 0.1f;
			RotationOscillationFrequency = 5f;
			RotationOscillationDampingRatio = 0.5f;
			RotationReactionMultiplier = 1f;
			AngularImpulseMultiplier = 1f;
			ScaleExponentialHalfLife = 0.02f;
			ScaleOscillationHalfLife = 0.1f;
			ScaleOscillationFrequency = 5f;
			ScaleOscillationDampingRatio = 0.5f;
			Instance.Reset();
		}

		public void AccumulateTarget(ref BoingEffector.Params effector, float dt)
		{
			Instance.AccumulateTarget(ref this, ref effector, dt);
		}

		public void EndAccumulateTargets()
		{
			Instance.EndAccumulateTargets(ref this);
		}

		public void Execute(float dt)
		{
			Instance.Execute(ref this, dt);
		}

		public void Execute(BoingBones bones, float dt)
		{
			float maxLen = bones.MaxCollisionResolutionSpeed * dt;
			for (int i = 0; i < bones.BoneData.Length; i++)
			{
				BoingBones.Chain chain = bones.BoneChains[i];
				BoingBones.Bone[] array = bones.BoneData[i];
				if (array == null)
				{
					continue;
				}
				foreach (BoingBones.Bone bone in array)
				{
					if (chain.ParamsOverride == null)
					{
						bone.Instance.Execute(ref bones.Params, dt);
					}
					else
					{
						bone.Instance.Execute(ref chain.ParamsOverride.Params, dt);
					}
				}
				BoingBones.Bone bone2 = array[0];
				bone2.ScaleWs = (bone2.BlendedScaleLs = bone2.CachedScaleLs);
				bone2.UpdateBounds();
				chain.Bounds = bone2.Bounds;
				Vector3 position = bone2.Position;
				for (int k = 1; k < array.Length; k++)
				{
					BoingBones.Bone bone3 = array[k];
					BoingBones.Bone bone4 = array[bone3.ParentIndex];
					Vector3 vector = bone4.Instance.PositionSpring.Value - bone3.Instance.PositionSpring.Value;
					Vector3 vector2 = VectorUtil.NormalizeSafe(vector, Vector3.zero);
					float magnitude = vector.magnitude;
					float num = magnitude - bone3.FullyStiffToParentLength;
					float num2 = bone3.LengthStiffnessT * num;
					bone3.Instance.PositionSpring.Value += num2 * vector2;
					Vector3 vector3 = Vector3.Project(bone3.Instance.PositionSpring.Velocity, vector2);
					bone3.Instance.PositionSpring.Velocity -= bone3.LengthStiffnessT * vector3;
					if (bone3.BendAngleCap < MathUtil.Pi - MathUtil.Epsilon)
					{
						Vector3 position2 = bone3.Position;
						Vector3 vector4 = bone3.Instance.PositionSpring.Value - position;
						vector4 = VectorUtil.ClampBend(vector4, position2 - position, bone3.BendAngleCap);
						bone3.Instance.PositionSpring.Value = position + vector4;
					}
					if (bone3.SquashAndStretch > 0f)
					{
						float num3 = magnitude * MathUtil.InvSafe(bone3.FullyStiffToParentLength);
						Vector3 b = VectorUtil.ComponentWiseDivSafe(Mathf.Clamp(Mathf.Sqrt(1f / num3), 1f / Mathf.Max(1f, chain.MaxStretch), Mathf.Max(1f, chain.MaxSquash)) * Vector3.one, bone4.ScaleWs);
						bone3.BlendedScaleLs = Vector3.Lerp(Vector3.Lerp(bone3.CachedScaleLs, b, bone3.SquashAndStretch), bone3.CachedScaleLs, bone3.AnimationBlend);
					}
					else
					{
						bone3.BlendedScaleLs = bone3.CachedScaleLs;
					}
					bone3.ScaleWs = VectorUtil.ComponentWiseMult(bone4.ScaleWs, bone3.BlendedScaleLs);
					bone3.UpdateBounds();
					chain.Bounds.Encapsulate(bone3.Bounds);
				}
				chain.Bounds.Expand(0.2f * Vector3.one);
				BoingBones.Bone[] array2;
				if (chain.EnableBoingKitCollision)
				{
					BoingBoneCollider[] boingColliders = bones.BoingColliders;
					foreach (BoingBoneCollider boingBoneCollider in boingColliders)
					{
						if (boingBoneCollider == null || !chain.Bounds.Intersects(boingBoneCollider.Bounds))
						{
							continue;
						}
						array2 = array;
						foreach (BoingBones.Bone bone5 in array2)
						{
							if (bone5.Bounds.Intersects(boingBoneCollider.Bounds) && boingBoneCollider.Collide(bone5.Instance.PositionSpring.Value, bones.MinScale * bone5.CollisionRadius, out var push))
							{
								bone5.Instance.PositionSpring.Value += VectorUtil.ClampLength(push, 0f, maxLen);
								bone5.Instance.PositionSpring.Velocity -= Vector3.Project(bone5.Instance.PositionSpring.Velocity, push);
							}
						}
					}
				}
				SphereCollider sharedSphereCollider = BoingManager.SharedSphereCollider;
				if (chain.EnableUnityCollision && sharedSphereCollider != null)
				{
					sharedSphereCollider.enabled = true;
					Collider[] unityColliders = bones.UnityColliders;
					foreach (Collider collider in unityColliders)
					{
						if (collider == null || !chain.Bounds.Intersects(collider.bounds))
						{
							continue;
						}
						array2 = array;
						foreach (BoingBones.Bone bone6 in array2)
						{
							if (bone6.Bounds.Intersects(collider.bounds))
							{
								sharedSphereCollider.center = bone6.Instance.PositionSpring.Value;
								sharedSphereCollider.radius = bone6.CollisionRadius;
								if (Physics.ComputePenetration(sharedSphereCollider, Vector3.zero, Quaternion.identity, collider, collider.transform.position, collider.transform.rotation, out var direction, out var distance))
								{
									bone6.Instance.PositionSpring.Value += VectorUtil.ClampLength(direction * distance, 0f, maxLen);
									bone6.Instance.PositionSpring.Velocity -= Vector3.Project(bone6.Instance.PositionSpring.Velocity, direction);
								}
							}
						}
					}
					sharedSphereCollider.enabled = false;
				}
				if (!chain.EnableInterChainCollision)
				{
					continue;
				}
				array2 = array;
				foreach (BoingBones.Bone bone7 in array2)
				{
					for (int n = i + 1; n < bones.BoneData.Length; n++)
					{
						BoingBones.Chain chain2 = bones.BoneChains[n];
						BoingBones.Bone[] array3 = bones.BoneData[n];
						if (array3 == null || !chain2.EnableInterChainCollision || !chain.Bounds.Intersects(chain2.Bounds))
						{
							continue;
						}
						BoingBones.Bone[] array4 = array3;
						foreach (BoingBones.Bone bone8 in array4)
						{
							if (Collision.SphereSphere(bone7.Instance.PositionSpring.Value, bones.MinScale * bone7.CollisionRadius, bone8.Instance.PositionSpring.Value, bones.MinScale * bone8.CollisionRadius, out var push2))
							{
								push2 = VectorUtil.ClampLength(push2, 0f, maxLen);
								float num4 = bone8.CollisionRadius * MathUtil.InvSafe(bone7.CollisionRadius + bone8.CollisionRadius);
								bone7.Instance.PositionSpring.Value += num4 * push2;
								bone8.Instance.PositionSpring.Value -= (1f - num4) * push2;
								bone7.Instance.PositionSpring.Velocity -= Vector3.Project(bone7.Instance.PositionSpring.Velocity, push2);
								bone8.Instance.PositionSpring.Velocity -= Vector3.Project(bone8.Instance.PositionSpring.Velocity, push2);
							}
						}
					}
				}
			}
		}

		public void PullResults(BoingBones bones)
		{
			Instance.PullResults(bones);
		}

		private void SuppressWarnings()
		{
			m_padding0 = 0;
			m_padding1 = 0;
			m_padding2 = 0f;
			m_padding0 = m_padding1;
			m_padding1 = m_padding0;
			m_padding2 = m_padding0;
		}
	}

	public struct Output(int instanceID, ref Vector3Spring positionSpring, ref QuaternionSpring rotationSpring, ref Vector3Spring scaleSpring)
	{
		public static readonly int Stride = 16 + Vector3Spring.Stride + QuaternionSpring.Stride;

		public int InstanceID = instanceID;

		public int m_padding0 = (m_padding1 = (m_padding2 = 0));

		public int m_padding1;

		public int m_padding2;

		public Vector3Spring PositionSpring = positionSpring;

		public QuaternionSpring RotationSpring = rotationSpring;

		public Vector3Spring ScaleSpring = scaleSpring;

		public void GatherOutput(Dictionary<int, BoingBehavior> behaviorMap, BoingManager.UpdateMode updateMode)
		{
			if (behaviorMap.TryGetValue(InstanceID, out var value) && value.isActiveAndEnabled && value.UpdateMode == updateMode)
			{
				value.GatherOutput(ref this);
			}
		}

		public void GatherOutput(Dictionary<int, BoingReactor> reactorMap, BoingManager.UpdateMode updateMode)
		{
			if (reactorMap.TryGetValue(InstanceID, out var value) && value.isActiveAndEnabled && value.UpdateMode == updateMode)
			{
				value.GatherOutput(ref this);
			}
		}

		private void SuppressWarnings()
		{
			m_padding0 = 0;
			m_padding1 = 0;
			m_padding2 = 0;
			m_padding0 = m_padding1;
			m_padding1 = m_padding2;
			m_padding2 = m_padding0;
		}
	}

	internal static Vector3 ComputeTranslationalResults(Transform t, Vector3 src, Vector3 dst, BoingBehavior b)
	{
		if (!b.LockTranslationX && !b.LockTranslationY && !b.LockTranslationZ)
		{
			return dst;
		}
		Vector3 vector = dst - src;
		switch (b.TranslationLockSpace)
		{
		case BoingManager.TranslationLockSpace.Global:
			if (b.LockTranslationX)
			{
				vector.x = 0f;
			}
			if (b.LockTranslationY)
			{
				vector.y = 0f;
			}
			if (b.LockTranslationZ)
			{
				vector.z = 0f;
			}
			break;
		case BoingManager.TranslationLockSpace.Local:
			if (b.LockTranslationX)
			{
				vector -= Vector3.Project(vector, t.right);
			}
			if (b.LockTranslationY)
			{
				vector -= Vector3.Project(vector, t.up);
			}
			if (b.LockTranslationZ)
			{
				vector -= Vector3.Project(vector, t.forward);
			}
			break;
		}
		return src + vector;
	}
}
