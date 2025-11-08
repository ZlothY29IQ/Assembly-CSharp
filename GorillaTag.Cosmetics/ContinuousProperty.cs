using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace GorillaTag.Cosmetics;

[Serializable]
public class ContinuousProperty
{
	public enum Type
	{
		Color,
		Scale,
		BlendShape,
		Float,
		ShaderVector2_X,
		ShaderColor,
		BezierInterpolation,
		AxisAngle,
		TransformInterpolation,
		OffsetInterpolation,
		Boolean,
		Speed,
		Rate,
		Volume,
		Pitch,
		PlayStop,
		EnableDisable,
		UnityEvent,
		Trigger
	}

	public enum Cast
	{
		Null = 0,
		Any = 1024,
		Transform = 2048,
		ParticleSystem = 3072,
		SkinnedMeshRenderer = 4096,
		Animator = 5120,
		AudioSource = 6144,
		Renderer = 7168,
		Behaviour = 8192,
		GameObject = 9216,
		Rigidbody = 10240
	}

	[Flags]
	public enum DataFlags
	{
		None = 0,
		HasCurve = 1,
		HasColor = 2,
		HasAxis = 4,
		HasInteger = 8,
		HasInterpolation = 0x10,
		IsShaderProperty = 0x20,
		IsAnimatorParameter = 0x40,
		HasThreshold = 0x80
	}

	private enum ThresholdResult
	{
		Null = 0,
		RisingEdge = 1048576,
		FallingEdge = 2097152,
		Unchanged = 3145728
	}

	private enum ThresholdOption
	{
		Invert,
		Normal
	}

	private enum RotationAxis
	{
		X = 4194304,
		Y = 8388608,
		Z = 12582912
	}

	public enum InterpolationMode
	{
		Position = 4194304,
		Rotation = 8388608,
		PositionAndRotation = 12582912
	}

	[SerializeField]
	private ContinuousPropertyModeSO mode;

	[FormerlySerializedAs("component")]
	[SerializeField]
	protected UnityEngine.Object target;

	private static int linearCurveHash = AnimationCurves.Linear.GetHashCode();

	private static int stepCurveHash = StepCurve.GetHashCode();

	[SerializeField]
	private Gradient color;

	[SerializeField]
	private AnimationCurve curve = AnimationCurves.Linear;

	[FormerlySerializedAs("materialIndex")]
	[SerializeField]
	private int intValue;

	[SerializeField]
	private string stringValue;

	[SerializeField]
	private BezierCurve bezierCurve;

	private const string ENUM_ERROR = "Internal values were changed at some point. Please select a new value.";

	[SerializeField]
	private RotationAxis localAxis = RotationAxis.X;

	[SerializeField]
	private InterpolationMode interpolationMode = InterpolationMode.PositionAndRotation;

	[SerializeField]
	private ParticleSystemStopBehavior stopType = ParticleSystemStopBehavior.StopEmitting;

	[SerializeField]
	private Transform transformA;

	[SerializeField]
	private Transform transformB;

	[SerializeField]
	private XformOffset offsetA;

	[SerializeField]
	private XformOffset offsetB;

	[SerializeField]
	private Vector2 range = new Vector2(0.5f, 1f);

	[SerializeField]
	private ThresholdOption thresholdOption = ThresholdOption.Normal;

	[SerializeField]
	private UnityEvent<float> unityEvent;

	private int internalSwitchValue;

	private ParticleSystem.MainModule particleMain;

	private ParticleSystem.EmissionModule particleEmission;

	private ParticleSystem.MinMaxCurve speedCurveCache;

	private ParticleSystem.MinMaxCurve rateCurveCache;

	private bool previousBoolValue;

	private int stringHash;

	private string ModeTooltip
	{
		get
		{
			if (!mode)
			{
				return "";
			}
			return $"{mode.type}: {mode.GetDescriptionForCast(GetTargetCast(target))}";
		}
	}

	private bool ModeErrorVisible => !IsValid();

	private bool ModeInfoVisible => mode == null;

	private string ModeErrorMessage => $"I can't find a target on '{target?.name}' to apply my '{MyType}' to. Did you drag in the wrong GameObject?\n\n{mode?.ListValidCasts()}";

	public ContinuousPropertyModeSO Mode => mode;

	public Type MyType
	{
		get
		{
			if (!(mode != null))
			{
				return Type.Color;
			}
			return mode.type;
		}
	}

	private bool HasTarget => MyType != Type.UnityEvent;

	private bool TargetInfoVisible
	{
		get
		{
			if (HasTarget)
			{
				return target == null;
			}
			return false;
		}
	}

	private string TargetTooltip
	{
		get
		{
			if (!(mode != null))
			{
				return "";
			}
			return mode.ListValidCasts();
		}
	}

	private bool AssignButtonVisible
	{
		get
		{
			if (mode != null)
			{
				if (!(target == null))
				{
					return !mode.IsCastValid(GetTargetCast(target));
				}
				return true;
			}
			return false;
		}
	}

	private bool ShiftButtonsVisible
	{
		get
		{
			IEnumerable<UnityEngine.Object> allValidObjectsOnMyTarget = GetAllValidObjectsOnMyTarget();
			if (allValidObjectsOnMyTarget == null)
			{
				return false;
			}
			return allValidObjectsOnMyTarget.Count() > 1;
		}
	}

	public UnityEngine.Object Target => target;

	private static AnimationCurve StepCurve => new AnimationCurve(new Keyframe(0f, 0f, float.PositiveInfinity, float.PositiveInfinity), new Keyframe(0.5f, 1f, float.PositiveInfinity, float.PositiveInfinity));

	public bool IsShaderProperty_Cached { get; private set; }

	public bool UsesThreshold_Cached { get; private set; }

	private bool HasGradient => HasAllFlags(DataFlags.HasColor);

	private bool HasCurve => HasAllFlags(DataFlags.HasCurve);

	private bool HasInt => HasAllFlags(DataFlags.HasInteger);

	public int IntValue => intValue;

	private bool HasString => HasAnyFlag(DataFlags.IsShaderProperty | DataFlags.IsAnimatorParameter);

	public string StringValue => stringValue;

	private bool HasBezier => MyType == Type.BezierInterpolation;

	private bool MissingBezier => bezierCurve == null;

	private bool AxisError => !Enum.IsDefined(typeof(RotationAxis), localAxis);

	private bool HasAxis => HasAllFlags(DataFlags.HasAxis);

	private bool InterpolationError => !Enum.IsDefined(typeof(InterpolationMode), interpolationMode);

	private bool HasInterpolation => HasAllFlags(DataFlags.HasInterpolation);

	private bool HasStopAction
	{
		get
		{
			if (MyType == Type.PlayStop)
			{
				return target is ParticleSystem;
			}
			return false;
		}
	}

	private bool HasXforms => MyType == Type.TransformInterpolation;

	private bool MissingXforms
	{
		get
		{
			if (!(transformA == null))
			{
				return transformB == null;
			}
			return true;
		}
	}

	private bool HasOffsets => MyType == Type.OffsetInterpolation;

	private string ThresholdErrorMessage => "The threshold will always be " + (((thresholdOption == ThresholdOption.Normal) ^ (range.x >= range.y)) ? "true." : "false.");

	private string ThresholdTooltip
	{
		get
		{
			if (!ThresholdError)
			{
				return "The threshold will be true" + ((thresholdOption != ThresholdOption.Normal) ? (((range.x > 0f) ? (" below " + range.x) : "") + ((range.x > 0f && range.y < 1f) ? " and" : "") + ((range.y < 1f) ? (" above " + range.y) : "")) : ((range.x > 0f && range.y < 1f) ? $" between {range.x} and {range.y}" : ((range.x > 0f) ? (" above " + range.x) : (" below " + range.y)))) + ", and false otherwise.";
			}
			return ThresholdErrorMessage;
		}
	}

	private bool HasThreshold => HasAllFlags(DataFlags.HasThreshold);

	private bool ThresholdError
	{
		get
		{
			if (!(range.x <= 0f) || !(range.y >= 1f))
			{
				return range.x >= range.y;
			}
			return true;
		}
	}

	private bool HasUnityEvent => MyType == Type.UnityEvent;

	private static Cast GetTargetCast(UnityEngine.Object o)
	{
		if (!(o is ParticleSystem))
		{
			if (!(o is SkinnedMeshRenderer))
			{
				if (!(o is Animator))
				{
					if (!(o is AudioSource))
					{
						if (!(o is Rigidbody))
						{
							if (!(o is Transform))
							{
								if (!(o is Renderer))
								{
									if (!(o is Behaviour))
									{
										if (o is GameObject)
										{
											return Cast.GameObject;
										}
										return Cast.Null;
									}
									return Cast.Behaviour;
								}
								return Cast.Renderer;
							}
							return Cast.Transform;
						}
						return Cast.Rigidbody;
					}
					return Cast.AudioSource;
				}
				return Cast.Animator;
			}
			return Cast.SkinnedMeshRenderer;
		}
		return Cast.ParticleSystem;
	}

	public static bool CastMatches(Cast cast, Cast test)
	{
		return cast switch
		{
			Cast.Null => false, 
			Cast.Any => true, 
			Cast.Renderer => test == Cast.Renderer || test == Cast.SkinnedMeshRenderer, 
			Cast.Behaviour => test != Cast.Transform && test != Cast.GameObject && test != Cast.Rigidbody, 
			_ => test == cast, 
		};
	}

	public static bool HasAllFlags(DataFlags flags, DataFlags test)
	{
		return (flags & test) == test;
	}

	public static bool HasAnyFlag(DataFlags flags, DataFlags test)
	{
		return (flags & test) != 0;
	}

	private static IEnumerable<UnityEngine.Object> GetAllObjects(UnityEngine.Object target)
	{
		if (!(target is Component component))
		{
			if (target is GameObject gameObject)
			{
				return ((IEnumerable<UnityEngine.Object>)(from c in gameObject.GetComponents<Component>()
					where IsValidComponent(c.GetType())
					select c)).Append((UnityEngine.Object)gameObject);
			}
			return null;
		}
		return ((IEnumerable<UnityEngine.Object>)(from c in component.GetComponents<Component>()
			where IsValidComponent(c.GetType())
			select c)).Append((UnityEngine.Object)component.gameObject);
	}

	private static bool IsValidComponent(System.Type t)
	{
		if (t != typeof(Renderer))
		{
			return t != typeof(ParticleSystemRenderer);
		}
		return false;
	}

	public ContinuousProperty()
	{
	}

	public ContinuousProperty(ContinuousPropertyModeSO mode, Transform initialTarget, Vector2 range = default(Vector2))
	{
		this.mode = mode;
		this.range = range;
		FindATarget();
	}

	private void FindATarget()
	{
	}

	private IEnumerable<UnityEngine.Object> GetAllValidObjectsOnMyTarget()
	{
		if (!mode)
		{
			return null;
		}
		return GetAllObjects(target)?.Where((UnityEngine.Object c) => mode.IsCastValid(GetTargetCast(c)));
	}

	private void PreviousTarget()
	{
		ShiftTarget(-1);
	}

	private void NextTarget()
	{
		ShiftTarget(1);
	}

	public bool ShiftTarget(int amount)
	{
		List<UnityEngine.Object> list = GetAllValidObjectsOnMyTarget()?.ToList();
		if (list == null || list.Count == 0)
		{
			return false;
		}
		int num = Mathf.Max(list.IndexOf(target), 0);
		target = list[(num + amount + list.Count) % list.Count];
		return true;
	}

	private void OnValueChanged()
	{
		if (!IsValid())
		{
			ShiftTarget(0);
		}
	}

	public bool IsValid()
	{
		if (!(mode == null) && !(target == null))
		{
			return mode.IsCastValid(GetTargetCast(target));
		}
		return true;
	}

	public int GetTargetInstanceID()
	{
		return target.GetInstanceID();
	}

	private bool HasAllFlags(DataFlags test)
	{
		if (mode != null)
		{
			return HasAllFlags(mode.GetFlagsForClosestCast(GetTargetCast(target)), test);
		}
		return false;
	}

	private bool HasAnyFlag(DataFlags test)
	{
		if (mode != null)
		{
			return HasAnyFlag(mode.GetFlagsForClosestCast(GetTargetCast(target)), test);
		}
		return false;
	}

	private string DynamicIntLabel()
	{
		if (!HasAllFlags(DataFlags.IsShaderProperty))
		{
			Type myType = MyType;
			if (myType != 0 && myType != Type.BlendShape)
			{
				return "Int Value";
			}
		}
		return "Material Index";
	}

	private string DynamicStringLabel()
	{
		if (HasAllFlags(DataFlags.IsShaderProperty))
		{
			return "Property Name";
		}
		if (HasAllFlags(DataFlags.IsAnimatorParameter))
		{
			return "Parameter Name";
		}
		return "String Value";
	}

	public void Init()
	{
		if (mode == null)
		{
			internalSwitchValue = 0;
			return;
		}
		Type type = mode.type;
		Cast cast = mode.GetClosestCast(GetTargetCast(target));
		DataFlags dataFlags = mode.GetFlagsForCast(cast);
		if ((type == Type.BezierInterpolation && MissingBezier) || (type == Type.TransformInterpolation && MissingXforms) || (type == Type.UnityEvent && unityEvent == null))
		{
			internalSwitchValue = 0;
			return;
		}
		if (type == Type.Color && CastMatches(Cast.Renderer, cast))
		{
			type = Type.ShaderColor;
			cast = Cast.Renderer;
			dataFlags |= DataFlags.IsShaderProperty;
			stringValue = "_BaseColor";
		}
		else if (type == Type.PlayStop && cast == Cast.Animator)
		{
			type = Type.EnableDisable;
			cast = Cast.Behaviour;
		}
		internalSwitchValue = (int)((uint)type | (uint)cast | (uint)(HasAllFlags(dataFlags, DataFlags.HasAxis) ? localAxis : ((RotationAxis)0))) | (int)(HasAllFlags(dataFlags, DataFlags.HasInterpolation) ? interpolationMode : ((InterpolationMode)0));
		IsShaderProperty_Cached = HasAllFlags(dataFlags, DataFlags.IsShaderProperty);
		UsesThreshold_Cached = HasAllFlags(dataFlags, DataFlags.HasThreshold);
		if (cast == Cast.ParticleSystem)
		{
			particleMain = ((ParticleSystem)target).main;
			particleEmission = ((ParticleSystem)target).emission;
			speedCurveCache = particleMain.startSpeed;
			rateCurveCache = particleEmission.rateOverTime;
		}
		if (IsShaderProperty_Cached)
		{
			stringHash = Shader.PropertyToID(stringValue);
		}
		else if (HasAllFlags(dataFlags, DataFlags.IsAnimatorParameter))
		{
			stringHash = Animator.StringToHash(stringValue);
		}
		if (!HasAnyFlag(dataFlags, DataFlags.HasCurve))
		{
			curve = AnimationCurves.Linear;
		}
	}

	public void InitThreshold()
	{
		if (UsesThreshold_Cached)
		{
			CheckThreshold(0f);
			if (!IsShaderProperty_Cached)
			{
				previousBoolValue = !previousBoolValue;
				Apply(0f, null);
			}
		}
	}

	public void Apply(float f, MaterialPropertyBlock mpb)
	{
		int num = internalSwitchValue | (int)CheckThreshold(f);
		if (num <= 1056784)
		{
			switch (num)
			{
			default:
				return;
			case 3072:
				particleMain.startColor = color.Evaluate(f);
				return;
			case 2049:
				((Transform)target).localScale = curve.Evaluate(f) * Vector3.one;
				return;
			case 3073:
				particleMain.startSize = curve.Evaluate(f);
				return;
			case 4098:
				((SkinnedMeshRenderer)target).SetBlendShapeWeight(intValue, curve.Evaluate(f) * 100f);
				return;
			case 7171:
				mpb.SetFloat(stringHash, curve.Evaluate(f));
				return;
			case 5123:
				((Animator)target).SetFloat(stringHash, curve.Evaluate(f));
				return;
			case 7172:
				mpb.SetVector(stringHash, new Vector2(curve.Evaluate(f), 0f));
				return;
			case 7173:
				mpb.SetColor(stringHash, color.Evaluate(f));
				return;
			case 1053706:
				break;
			case 5131:
				((Animator)target).speed = curve.Evaluate(f);
				return;
			case 3083:
				particleMain.startSpeed = ScaleCurve(in speedCurveCache, curve.Evaluate(f));
				return;
			case 3084:
				particleEmission.rateOverTime = ScaleCurve(in rateCurveCache, curve.Evaluate(f));
				return;
			case 6157:
				((AudioSource)target).volume = Mathf.Clamp01(curve.Evaluate(f));
				return;
			case 6158:
				((AudioSource)target).pitch = Mathf.Clamp(curve.Evaluate(f), -3f, 3f);
				return;
			case 1051663:
				((ParticleSystem)target).Play();
				return;
			case 1054735:
				((AudioSource)target).Play();
				return;
			case 1055760:
				goto IL_0753;
			case 1056784:
				goto IL_076a;
			case 1041:
			case 1049617:
				unityEvent.Invoke(curve.Evaluate(f));
				return;
			case 1053714:
				((Animator)target).SetTrigger(stringHash);
				return;
			}
			goto IL_0638;
		}
		if (num <= 3146769)
		{
			if (num <= 2102290)
			{
				if (num > 2098193)
				{
					switch (num)
					{
					default:
						_ = 2102290;
						return;
					case 2102282:
						break;
					case 2100239:
						((ParticleSystem)target).Stop(withChildren: true, stopType);
						return;
					}
					goto IL_0638;
				}
				if (num != 1057808)
				{
					_ = 2098193;
					return;
				}
			}
			else
			{
				if (num <= 2104336)
				{
					switch (num)
					{
					default:
						return;
					case 2103311:
						((AudioSource)target).Stop();
						return;
					case 2104336:
						break;
					}
					goto IL_0753;
				}
				if (num == 2105360)
				{
					goto IL_076a;
				}
				if (num != 2106384)
				{
					_ = 3146769;
					return;
				}
			}
			((GameObject)target).SetActive(previousBoolValue);
			return;
		}
		if (num <= 3152912)
		{
			if (num <= 3150858)
			{
				if (num != 3148815)
				{
					_ = 3150858;
				}
			}
			else if (num != 3150866 && num != 3151887)
			{
				_ = 3152912;
			}
			return;
		}
		if (num <= 3154960)
		{
			if (num != 3153936)
			{
				_ = 3154960;
			}
			return;
		}
		switch (num)
		{
		case 12584966:
		{
			float t3 = curve.Evaluate(f);
			((Transform)target).SetPositionAndRotation(bezierCurve.GetPoint(t3), Quaternion.LookRotation(bezierCurve.GetDirection(t3)));
			break;
		}
		case 4196358:
			((Transform)target).position = bezierCurve.GetPoint(curve.Evaluate(f));
			break;
		case 8390662:
			((Transform)target).rotation = Quaternion.LookRotation(bezierCurve.GetDirection(curve.Evaluate(f)));
			break;
		case 4196359:
			((Transform)target).localRotation = Quaternion.Euler(curve.Evaluate(f) * 360f, 0f, 0f);
			break;
		case 8390663:
			((Transform)target).localRotation = Quaternion.Euler(0f, curve.Evaluate(f) * 360f, 0f);
			break;
		case 12584967:
			((Transform)target).localRotation = Quaternion.Euler(0f, 0f, curve.Evaluate(f) * 360f);
			break;
		case 12584968:
		{
			transformA.GetPositionAndRotation(out var position, out var rotation);
			transformB.GetPositionAndRotation(out var position2, out var rotation2);
			float t2 = curve.Evaluate(f);
			((Transform)target).SetPositionAndRotation(Vector3.Lerp(position, position2, t2), Quaternion.Slerp(rotation, rotation2, t2));
			break;
		}
		case 4196360:
			((Transform)target).position = Vector3.Lerp(transformA.position, transformB.position, curve.Evaluate(f));
			break;
		case 8390664:
			((Transform)target).rotation = Quaternion.Slerp(transformA.rotation, transformB.rotation, curve.Evaluate(f));
			break;
		case 12584969:
		{
			float t = curve.Evaluate(f);
			((Transform)target).SetLocalPositionAndRotation(Vector3.Lerp(offsetA.pos, offsetB.pos, t), Quaternion.Slerp(offsetA.rot, offsetB.rot, t));
			break;
		}
		case 4196361:
			((Transform)target).localPosition = Vector3.Lerp(offsetA.pos, offsetB.pos, curve.Evaluate(f));
			break;
		case 8390665:
			((Transform)target).localRotation = Quaternion.Slerp(offsetA.rot, offsetB.rot, curve.Evaluate(f));
			break;
		}
		return;
		IL_0638:
		((Animator)target).SetBool(stringHash, previousBoolValue);
		return;
		IL_0753:
		((Renderer)target).enabled = previousBoolValue;
		return;
		IL_076a:
		((Behaviour)target).enabled = previousBoolValue;
	}

	private ParticleSystem.MinMaxCurve ScaleCurve(in ParticleSystem.MinMaxCurve inCurve, float scale)
	{
		ParticleSystem.MinMaxCurve result = inCurve;
		switch (result.mode)
		{
		case ParticleSystemCurveMode.Constant:
			result.constant *= scale;
			break;
		case ParticleSystemCurveMode.Curve:
		case ParticleSystemCurveMode.TwoCurves:
			result.curveMultiplier *= scale;
			break;
		case ParticleSystemCurveMode.TwoConstants:
			result.constantMin *= scale;
			result.constantMax *= scale;
			break;
		}
		return result;
	}

	private ThresholdResult CheckThreshold(float f)
	{
		if (!UsesThreshold_Cached)
		{
			return ThresholdResult.Null;
		}
		bool flag = f >= range.x && f <= range.y;
		if (!previousBoolValue && ((thresholdOption == ThresholdOption.Normal && flag) || (thresholdOption == ThresholdOption.Invert && !flag)))
		{
			previousBoolValue = true;
			return ThresholdResult.RisingEdge;
		}
		if (previousBoolValue && ((thresholdOption == ThresholdOption.Normal && !flag) || (thresholdOption == ThresholdOption.Invert && flag)))
		{
			previousBoolValue = false;
			return ThresholdResult.FallingEdge;
		}
		return ThresholdResult.Unchanged;
	}
}
