using System;
using System.Collections.Generic;
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
		Rigidbody = 10240,
		VoicePitchShiftCosmetic = 11264
	}

	[Flags]
	public enum DataFlags
	{
		None = 0,
		[Tooltip("Expose the AnimationCurve for single values")]
		HasCurve = 1,
		[Tooltip("Expose the Gradient for colors")]
		HasColor = 2,
		[Tooltip("Select which axis it should rotate on")]
		HasAxis = 4,
		[Tooltip("Expose the integer, usually for material index")]
		HasInteger = 8,
		[Tooltip("Select whether to use position, rotation, or both when interpolating")]
		HasInterpolation = 0x10,
		[Tooltip("Expose the string and hash it into a shader property ID")]
		IsShaderProperty = 0x20,
		[Tooltip("Expose the string and hash it into an animator parameter ID")]
		IsAnimatorParameter = 0x40,
		[Tooltip("Expose the threshold range as a dual slider")]
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

	public enum EventMode
	{
		Passthrough = 4194304,
		Frequency = 8388608,
		AveragePerSecond = 12582912
	}

	[SerializeField]
	private ContinuousPropertyModeSO mode;

	[FormerlySerializedAs("component")]
	[SerializeField]
	protected UnityEngine.Object target;

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
	private EventMode eventMode = EventMode.Passthrough;

	[SerializeField]
	private UnityEvent<float> unityEvent;

	[Tooltip("Check this box if only the owner/local player is supposed to run this property.")]
	[SerializeField]
	private bool runOnlyLocally;

	private bool rigLocal;

	private int internalSwitchValue;

	private ParticleSystem.MainModule particleMain;

	private ParticleSystem.EmissionModule particleEmission;

	private ParticleSystem.MinMaxCurve speedCurveCache;

	private ParticleSystem.MinMaxCurve rateCurveCache;

	private float frequencyTimer;

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

	private bool ModeInfoVisible => mode == null;

	private bool ModeErrorVisible => !IsValid();

	private string ModeErrorMessage
	{
		get
		{
			if (!(mode != null))
			{
				return "How did we get here?";
			}
			return "I couldn't find any valid target to apply my '" + mode.name + "' to in the whole prefab.\n\n" + mode.ListValidCasts();
		}
	}

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

	private bool ShiftButtonsVisible => mode != null;

	public UnityEngine.Object Target => target;

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

	private bool HasAxisMode => HasAllFlags(DataFlags.HasAxis);

	private bool InterpolationError => !Enum.IsDefined(typeof(InterpolationMode), interpolationMode);

	private bool HasInterpolationMode => HasAllFlags(DataFlags.HasInterpolation);

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

	private bool HasEventMode
	{
		get
		{
			if (MyType == Type.UnityEvent)
			{
				return !HasAnyFlag(DataFlags.HasThreshold);
			}
			return false;
		}
	}

	private bool HasUnityEvent => MyType == Type.UnityEvent;

	public bool RunOnlyLocally => runOnlyLocally;

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
						if (!(o is VoiceShiftCosmetic))
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
						return Cast.VoicePitchShiftCosmetic;
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

	private static void GetAllValidObjectsNonAlloc(Transform t, List<UnityEngine.Object> objects)
	{
		objects.Clear();
		objects.Add(t.gameObject);
		Component[] components = t.GetComponents<Component>();
		foreach (UnityEngine.Object obj in components)
		{
			if (IsValidObject(obj.GetType()))
			{
				objects.Add(obj);
			}
		}
	}

	private static bool IsValidObject(System.Type t)
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
		target = initialTarget;
		this.range = range;
		ShiftTarget(0);
	}

	private void PreviousTarget()
	{
		ShiftTarget(-1);
	}

	private void NextTarget()
	{
		ShiftTarget(1);
	}

	public bool ShiftTarget(int shiftAmount)
	{
		if (mode == null)
		{
			return false;
		}
		int num = -1;
		Transform transform = ((!(target != null)) ? null : ((target as GameObject)?.transform ?? ((Component)target).transform));
		Transform transform2 = transform;
		if (transform2 == null)
		{
			return false;
		}
		Stack<Transform> stack = new Stack<Transform>();
		stack.Push(transform2);
		List<UnityEngine.Object> list = new List<UnityEngine.Object>();
		List<UnityEngine.Object> list2 = new List<UnityEngine.Object>();
		Transform result;
		while (stack.TryPop(out result))
		{
			if (num < 0 && result == transform)
			{
				num = list.Count;
			}
			GetAllValidObjectsNonAlloc(result, list2);
			foreach (UnityEngine.Object item in list2)
			{
				if (mode.IsCastValid(GetTargetCast(item)))
				{
					if (item == target)
					{
						num = list.Count;
					}
					list.Add(item);
				}
			}
			for (int num2 = result.childCount - 1; num2 >= 0; num2--)
			{
				stack.Push(result.GetChild(num2));
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		target = list[(num >= 0) ? ((num + shiftAmount + list.Count) % list.Count) : 0];
		return true;
	}

	private void OnModeOrTargetChanged()
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
			if (myType != Type.Color && myType != Type.BlendShape)
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

	public void SetRigIsLocal(bool v)
	{
		rigLocal = v;
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
		if (cast == Cast.Null || (type == Type.BezierInterpolation && MissingBezier) || (type == Type.TransformInterpolation && MissingXforms) || (type == Type.UnityEvent && unityEvent == null))
		{
			internalSwitchValue = 0;
			IsShaderProperty_Cached = false;
			UsesThreshold_Cached = false;
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
		internalSwitchValue = (int)((uint)type | (uint)cast | (uint)(HasAxisMode ? localAxis : ((RotationAxis)0)) | (uint)(HasInterpolationMode ? interpolationMode : ((InterpolationMode)0))) | (int)(HasEventMode ? eventMode : ((EventMode)0));
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
				Apply(0f, 0f, null);
			}
		}
	}

	public void Apply(float f, float deltaTime, MaterialPropertyBlock mpb)
	{
		if (runOnlyLocally && !rigLocal)
		{
			return;
		}
		int num = internalSwitchValue | (int)CheckThreshold(f);
		if (num <= 1057808)
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
			case 11278:
				((VoiceShiftCosmetic)target).Pitch = curve.Evaluate(f);
				return;
			case 1051663:
				((ParticleSystem)target).Play();
				return;
			case 1054735:
				((AudioSource)target).Play();
				return;
			case 1055760:
				goto IL_07ab;
			case 1056784:
				goto IL_07c2;
			case 1057808:
				goto IL_07d9;
			case 1049617:
				unityEvent.Invoke(curve.Evaluate(f));
				return;
			case 1053714:
				((Animator)target).SetTrigger(stringHash);
				return;
			}
		}
		else
		{
			if (num > 3150858)
			{
				if (num <= 3154960)
				{
					if (num <= 3151887)
					{
						if (num != 3150866)
						{
							_ = 3151887;
						}
					}
					else if (num != 3152912 && num != 3153936)
					{
						_ = 3154960;
					}
					return;
				}
				switch (num)
				{
				case 12584966:
				{
					float t = curve.Evaluate(f);
					((Transform)target).SetPositionAndRotation(bezierCurve.GetPoint(t), Quaternion.LookRotation(bezierCurve.GetDirection(t)));
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
					float t3 = curve.Evaluate(f);
					((Transform)target).SetPositionAndRotation(Vector3.Lerp(position, position2, t3), Quaternion.Slerp(rotation, rotation2, t3));
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
					float t2 = curve.Evaluate(f);
					((Transform)target).SetLocalPositionAndRotation(Vector3.Lerp(offsetA.pos, offsetB.pos, t2), Quaternion.Slerp(offsetA.rot, offsetB.rot, t2));
					break;
				}
				case 4196361:
					((Transform)target).localPosition = Vector3.Lerp(offsetA.pos, offsetB.pos, curve.Evaluate(f));
					break;
				case 8390665:
					((Transform)target).localRotation = Quaternion.Slerp(offsetA.rot, offsetB.rot, curve.Evaluate(f));
					break;
				case 4195345:
					unityEvent.Invoke(curve.Evaluate(f));
					break;
				case 8389649:
				{
					float num4 = curve.Evaluate(f);
					float num5 = 1f / num4;
					frequencyTimer += deltaTime;
					if (frequencyTimer >= num5)
					{
						frequencyTimer = Mathf.Repeat(frequencyTimer - num5, num5);
						unityEvent.Invoke(num4);
					}
					break;
				}
				case 12583953:
				{
					float num2 = curve.Evaluate(f);
					float num3 = 1f - Mathf.Exp((0f - num2) * deltaTime);
					if (UnityEngine.Random.value < num3)
					{
						unityEvent.Invoke(num2);
					}
					break;
				}
				}
				return;
			}
			if (num > 2103311)
			{
				if (num <= 2106384)
				{
					if (num == 2104336)
					{
						goto IL_07ab;
					}
					if (num == 2105360)
					{
						goto IL_07c2;
					}
					if (num != 2106384)
					{
						return;
					}
					goto IL_07d9;
				}
				if (num != 3146769 && num != 3148815)
				{
					_ = 3150858;
				}
				return;
			}
			switch (num)
			{
			default:
				return;
			case 2102282:
				break;
			case 2100239:
				((ParticleSystem)target).Stop(withChildren: true, stopType);
				return;
			case 2103311:
				((AudioSource)target).Stop();
				return;
			}
		}
		((Animator)target).SetBool(stringHash, previousBoolValue);
		return;
		IL_07ab:
		((Renderer)target).enabled = previousBoolValue;
		return;
		IL_07d9:
		((GameObject)target).SetActive(previousBoolValue);
		return;
		IL_07c2:
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

	private bool CheckContinuousEvent(float f, float deltaTime)
	{
		switch (eventMode)
		{
		case EventMode.Passthrough:
			return true;
		case EventMode.Frequency:
			frequencyTimer += deltaTime;
			if (frequencyTimer < f)
			{
				return false;
			}
			frequencyTimer = Mathf.Repeat(frequencyTimer - f, f);
			return true;
		case EventMode.AveragePerSecond:
		{
			float num = 1f - Mathf.Exp((0f - f) * deltaTime);
			return UnityEngine.Random.value < num;
		}
		default:
			return false;
		}
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
