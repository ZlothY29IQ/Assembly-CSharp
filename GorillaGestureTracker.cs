using System;
using UnityEngine;

public class GorillaGestureTracker : MonoBehaviour
{
	[SerializeField]
	private VRRig _rig;

	[SerializeField]
	private Transform _rigTransform;

	public const int N_FACE = 0;

	public const int R_HAND = 1;

	public const int R_PALM = 2;

	public const int R_WRIST = 3;

	public const int R_DIGITS = 4;

	public const int R_THUMB = 5;

	public const int R_INDEX = 6;

	public const int R_MIDDLE = 7;

	public const int L_HAND = 8;

	public const int L_PALM = 9;

	public const int L_WRIST = 10;

	public const int L_DIGITS = 11;

	public const int L_THUMB = 12;

	public const int L_INDEX = 13;

	public const int L_MIDDLE = 14;

	public const int N_SIZE = 15;

	[Space]
	[SerializeField]
	private Vector3 _handBasisAngles = new Vector3(0f, 2f, 341f);

	[Space]
	[SerializeField]
	private Vector3 _faceBasisOffset = new Vector3(0f, 0.1f, 0.136f);

	[SerializeField]
	private Quaternion _faceBasisAngles = Quaternion.Euler(-8f, 0f, 0f);

	[Space]
	[SerializeField]
	private bool _debug;

	[NonSerialized]
	private bool _setupDone;

	public static uint TickRate = 24u;

	[Space]
	[SerializeField]
	private Transform[] _bones = new Transform[15];

	[NonSerialized]
	private VRMap[] _vrNodes = new VRMap[15];

	[NonSerialized]
	private float[] _inputs = new float[15];

	[NonSerialized]
	private int[] _flexes = new int[15];

	[NonSerialized]
	private Vector3[] _normals = new Vector3[15];

	[NonSerialized]
	private Vector3[] _positions = new Vector3[15];

	[Space]
	[SerializeField]
	private GorillaHandGesture[] _gestures = new GorillaHandGesture[0];

	[NonSerialized]
	private bool[] _matchesR = new bool[0];

	[NonSerialized]
	private bool[] _matchesL = new bool[0];

	private const int H_BENT = 0;

	private const int H_OPEN = 3;

	private const int H_CLOSED = 6;

	private const int N_HAND = 0;

	private const int A_PALM = 1;

	private const int A_WRIST = 2;

	private const int A_DIGITS = 3;

	private const int D_THUMB = 4;

	private const int D_INDEX = 5;

	private const int D_MIDDLE = 6;

	private void Awake()
	{
		Setup();
	}

	private void Setup()
	{
		if (_rig.AsNull() == null)
		{
			_rig = GetComponentInChildren<VRRig>();
		}
		if (_rig.AsNull() == null)
		{
			return;
		}
		_rigTransform = _rig.transform;
		_vrNodes[1] = _rig.rightHand;
		_vrNodes[5] = _rig.rightThumb;
		_vrNodes[6] = _rig.rightIndex;
		_vrNodes[7] = _rig.rightMiddle;
		_vrNodes[8] = _rig.leftHand;
		_vrNodes[12] = _rig.leftThumb;
		_vrNodes[13] = _rig.leftIndex;
		_vrNodes[14] = _rig.leftMiddle;
		Transform[] bones = _rig.mainSkin.bones;
		foreach (Transform transform in bones)
		{
			string text = transform.name;
			if (text.Contains("head", StringComparison.OrdinalIgnoreCase))
			{
				_bones[0] = transform;
			}
			else if (text.Contains("hand.R", StringComparison.OrdinalIgnoreCase))
			{
				_bones[1] = transform;
			}
			else if (text.Contains("thumb.03.R", StringComparison.OrdinalIgnoreCase))
			{
				_bones[5] = transform;
			}
			else if (text.Contains("f_index.02.R", StringComparison.OrdinalIgnoreCase))
			{
				_bones[6] = transform;
			}
			else if (text.Contains("f_middle.02.R", StringComparison.OrdinalIgnoreCase))
			{
				_bones[7] = transform;
			}
			else if (text.Contains("hand.L", StringComparison.OrdinalIgnoreCase))
			{
				_bones[8] = transform;
			}
			else if (text.Contains("thumb.03.L", StringComparison.OrdinalIgnoreCase))
			{
				_bones[12] = transform;
			}
			else if (text.Contains("f_index.02.L", StringComparison.OrdinalIgnoreCase))
			{
				_bones[13] = transform;
			}
			else if (text.Contains("f_middle.02.L", StringComparison.OrdinalIgnoreCase))
			{
				_bones[14] = transform;
			}
		}
		_matchesR = new bool[_gestures.Length];
		_matchesL = new bool[_gestures.Length];
		_setupDone = true;
	}

	private void FixedUpdate()
	{
		PollNodes();
		PollGestures();
	}

	private void PollGestures()
	{
		if (_gestures != null)
		{
			int num = _gestures.Length;
			float deltaTime = Time.deltaTime;
			for (int i = 0; i < num; i++)
			{
				PollGesture(1, i, deltaTime, ref _matchesR);
				PollGesture(8, i, deltaTime, ref _matchesL);
			}
		}
	}

	private void PollNodes()
	{
		PollFace(0);
		PollHandAxes(1);
		PollThumb(5, out var flex);
		PollIndex(6, out var flex2);
		PollMiddle(7, out var flex3);
		PollHandAxes(8);
		PollThumb(12, out var flex4);
		PollIndex(13, out var flex5);
		PollMiddle(14, out var flex6);
		_flexes[1] = flex + 1 + (flex2 + 1) + (flex3 + 1);
		_flexes[8] = flex4 + 1 + (flex5 + 1) + (flex6 + 1);
	}

	private void PollThumb(int i, out int flex)
	{
		VRMapThumb vRMapThumb = (VRMapThumb)_vrNodes[i];
		Transform obj = _bones[i];
		float num = 0f;
		bool flag = vRMapThumb.primaryButtonTouch || vRMapThumb.secondaryButtonTouch;
		bool flag2 = vRMapThumb.primaryButtonPress || vRMapThumb.secondaryButtonPress;
		if (flag)
		{
			num = 0.1f;
		}
		if (flag2)
		{
			num = 1f;
		}
		flex = -1;
		if (flag2)
		{
			flex = 1;
		}
		else if (!flag)
		{
			flex = 0;
		}
		Vector3 position = obj.position;
		Vector3 up = obj.up;
		_positions[i] = position;
		_normals[i] = up;
		_inputs[i] = num;
		_flexes[i] = flex;
	}

	private void PollIndex(int i, out int flex)
	{
		VRMapIndex obj = (VRMapIndex)_vrNodes[i];
		Transform transform = _bones[i];
		float num = Mathf.Clamp01(obj.triggerValue / 0.88f);
		flex = -1;
		if (num.Approx(0f))
		{
			flex = 0;
		}
		if (num.Approx(1f))
		{
			flex = 1;
		}
		Vector3 position = transform.position;
		Vector3 up = transform.up;
		_positions[i] = position;
		_normals[i] = up;
		_inputs[i] = num;
		_flexes[i] = flex;
	}

	private void PollMiddle(int i, out int flex)
	{
		VRMapMiddle obj = (VRMapMiddle)_vrNodes[i];
		Transform transform = _bones[i];
		float gripValue = obj.gripValue;
		flex = -1;
		if (gripValue.Approx(0f))
		{
			flex = 0;
		}
		if (gripValue.Approx(1f))
		{
			flex = 1;
		}
		Vector3 position = transform.position;
		Vector3 up = transform.up;
		_positions[i] = position;
		_normals[i] = up;
		_inputs[i] = gripValue;
		_flexes[i] = flex;
	}

	private void PollGesture(int hand, int i, float dt, ref bool[] results)
	{
		results[i] = false;
		GorillaHandGesture gorillaHandGesture = _gestures[i];
		if (gorillaHandGesture.track)
		{
			GestureNode[] nodes = gorillaHandGesture.nodes;
			int tracked = 0;
			int matches = 0;
			TrackHand(hand, (GestureHandNode)nodes[0], ref tracked, ref matches);
			TrackHandAxis(hand + 1, nodes[1], ref tracked, ref matches);
			TrackHandAxis(hand + 2, nodes[2], ref tracked, ref matches);
			TrackHandAxis(hand + 3, nodes[3], ref tracked, ref matches);
			TrackDigit(hand + 4, (GestureDigitNode)nodes[4], ref tracked, ref matches);
			TrackDigit(hand + 5, (GestureDigitNode)nodes[5], ref tracked, ref matches);
			TrackDigit(hand + 6, (GestureDigitNode)nodes[6], ref tracked, ref matches);
			results[i] = tracked == matches;
		}
	}

	private void TrackHand(int hand, GestureHandNode node, ref int tracked, ref int matches)
	{
		if (!node.track)
		{
			return;
		}
		GestureHandState state = node.state;
		if ((state & GestureHandState.IsLeft) == GestureHandState.IsLeft)
		{
			tracked++;
			if (hand == 8)
			{
				matches++;
			}
		}
		if ((state & GestureHandState.IsRight) == GestureHandState.IsRight)
		{
			tracked++;
			if (hand == 1)
			{
				matches++;
			}
		}
		if ((state & GestureHandState.Open) == GestureHandState.Open)
		{
			tracked++;
			if (_flexes[hand] == 3)
			{
				matches++;
			}
		}
		if ((state & GestureHandState.Closed) == GestureHandState.Closed)
		{
			tracked++;
			if (_flexes[hand] == 6)
			{
				matches++;
			}
		}
	}

	private void TrackHandAxis(int axis, GestureNode node, ref int tracked, ref int matches)
	{
		if (!node.track)
		{
			return;
		}
		GestureAlignment alignment = node.alignment;
		Vector3 lhs = _normals[axis];
		Vector3 rhs = _normals[0];
		float num = Vector3.Dot(lhs, Vector3.up);
		float num2 = 0f - num;
		float num3 = Vector3.Dot(lhs, rhs);
		float num4 = 0f - num3;
		if ((alignment & GestureAlignment.WorldUp) == GestureAlignment.WorldUp)
		{
			tracked++;
			if (num > 1E-05f)
			{
				matches++;
			}
		}
		if ((alignment & GestureAlignment.WorldDown) == GestureAlignment.WorldDown)
		{
			tracked++;
			if (num2 > 1E-05f)
			{
				matches++;
			}
		}
		if ((alignment & GestureAlignment.TowardFace) == GestureAlignment.TowardFace)
		{
			tracked++;
			if (num3 > 1E-05f)
			{
				matches++;
			}
		}
		if ((alignment & GestureAlignment.AwayFromFace) == GestureAlignment.AwayFromFace)
		{
			tracked++;
			if (num4 > 1E-05f)
			{
				matches++;
			}
		}
	}

	private void TrackDigit(int digit, GestureDigitNode node, ref int tracked, ref int matches)
	{
		if (!node.track)
		{
			return;
		}
		GestureAlignment alignment = node.alignment;
		GestureDigitFlexion flexion = node.flexion;
		Vector3 lhs = _normals[digit];
		Vector3 rhs = _normals[0];
		int num = _flexes[digit];
		bool flag = num == 0;
		bool flag2 = num == 1;
		bool flag3 = num == -1;
		float num2 = Vector3.Dot(lhs, Vector3.up);
		float num3 = 0f - num2;
		float num4 = Vector3.Dot(lhs, rhs);
		float num5 = 0f - num4;
		if ((alignment & GestureAlignment.WorldUp) == GestureAlignment.WorldUp)
		{
			tracked++;
			if (num2 > 1E-05f)
			{
				matches++;
			}
		}
		if ((alignment & GestureAlignment.WorldDown) == GestureAlignment.WorldDown)
		{
			tracked++;
			if (num3 > 1E-05f)
			{
				matches++;
			}
		}
		if ((alignment & GestureAlignment.TowardFace) == GestureAlignment.TowardFace)
		{
			tracked++;
			if (num4 > 1E-05f)
			{
				matches++;
			}
		}
		if ((alignment & GestureAlignment.AwayFromFace) == GestureAlignment.AwayFromFace)
		{
			tracked++;
			if (num5 > 1E-05f)
			{
				matches++;
			}
		}
		if ((flexion & GestureDigitFlexion.Bent) == GestureDigitFlexion.Bent)
		{
			tracked++;
			if (flag3)
			{
				matches++;
			}
		}
		if ((flexion & GestureDigitFlexion.Open) == GestureDigitFlexion.Open)
		{
			tracked++;
			if (flag)
			{
				matches++;
			}
		}
		if ((flexion & GestureDigitFlexion.Closed) == GestureDigitFlexion.Closed)
		{
			tracked++;
			if (flag2)
			{
				matches++;
			}
		}
	}

	private void PollFace(int index)
	{
		Transform transform = _bones[index];
		_positions[index] = transform.TransformPoint(_faceBasisOffset);
		_normals[index] = _faceBasisAngles * transform.forward;
	}

	private void PollHandAxes(int hand)
	{
		bool flag = hand == 1;
		bool num = hand == 8;
		int num2 = hand + 1;
		int num3 = hand + 2;
		int num4 = hand + 3;
		Transform transform = _bones[hand];
		Vector3 handBasisAngles = _handBasisAngles;
		if (num)
		{
			handBasisAngles.z *= -1f;
		}
		Quaternion quaternion = transform.rotation * Quaternion.Euler(handBasisAngles);
		_positions[hand] = transform.position;
		_normals[num2] = quaternion * Vector3.right * (flag ? 1f : (-1f));
		_normals[num3] = quaternion * Vector3.forward;
		_normals[num4] = quaternion * Vector3.up;
	}
}
