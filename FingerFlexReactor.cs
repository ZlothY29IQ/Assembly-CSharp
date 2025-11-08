using System;
using UnityEngine;

public class FingerFlexReactor : MonoBehaviour
{
	[Serializable]
	public class BlendShapeTarget
	{
		public FingerMap sourceFinger;

		public SkinnedMeshRenderer targetRenderer;

		public int blendShapeIndex;

		public Vector2 inputRange = new Vector2(0f, 1f);

		public Vector2 outputRange = new Vector2(0f, 1f);

		[NonSerialized]
		public float currentValue;
	}

	public enum FingerMap
	{
		None = -1,
		LeftThumb,
		LeftIndex,
		LeftMiddle,
		RightThumb,
		RightIndex,
		RightMiddle
	}

	[SerializeField]
	private VRRig _rig;

	[SerializeField]
	private VRMap[] _fingers = new VRMap[0];

	[SerializeField]
	private BlendShapeTarget[] _blendShapeTargets = new BlendShapeTarget[0];

	private void Setup()
	{
		_rig = GetComponentInParent<VRRig>();
		if ((bool)_rig)
		{
			_fingers = new VRMap[6] { _rig.leftThumb, _rig.leftIndex, _rig.leftMiddle, _rig.rightThumb, _rig.rightIndex, _rig.rightMiddle };
		}
	}

	private void Awake()
	{
		Setup();
	}

	private void FixedUpdate()
	{
		UpdateBlendShapes();
	}

	public void UpdateBlendShapes()
	{
		if (!_rig || _blendShapeTargets == null || _fingers == null || _blendShapeTargets.Length == 0 || _fingers.Length == 0)
		{
			return;
		}
		for (int i = 0; i < _blendShapeTargets.Length; i++)
		{
			BlendShapeTarget blendShapeTarget = _blendShapeTargets[i];
			if (blendShapeTarget == null)
			{
				continue;
			}
			int sourceFinger = (int)blendShapeTarget.sourceFinger;
			if (sourceFinger != -1)
			{
				SkinnedMeshRenderer targetRenderer = blendShapeTarget.targetRenderer;
				if ((bool)targetRenderer)
				{
					float lerpValue = GetLerpValue(_fingers[sourceFinger]);
					Vector2 inputRange = blendShapeTarget.inputRange;
					Vector2 outputRange = blendShapeTarget.outputRange;
					targetRenderer.SetBlendShapeWeight(value: blendShapeTarget.currentValue = MathUtils.Linear(lerpValue, inputRange.x, inputRange.y, outputRange.x, outputRange.y), index: blendShapeTarget.blendShapeIndex);
				}
			}
		}
	}

	private static float GetLerpValue(VRMap map)
	{
		if (!(map is VRMapThumb vRMapThumb))
		{
			if (!(map is VRMapIndex { calcT: var calcT }))
			{
				if (!(map is VRMapMiddle { calcT: var calcT2 }))
				{
					return 0f;
				}
				return calcT2;
			}
			return calcT;
		}
		return (vRMapThumb.calcT > 0.1f) ? 1f : 0f;
	}
}
