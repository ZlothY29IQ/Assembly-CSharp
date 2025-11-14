using System;
using UnityEngine;

[Obsolete]
public class GorillaPawn : MonoBehaviour
{
	[SerializeField]
	private Transform _transform;

	[SerializeField]
	private Transform _handLeft;

	[SerializeField]
	private Transform _handRight;

	[SerializeField]
	private Transform _head;

	[Space]
	[SerializeField]
	private VRRig _rig;

	[SerializeField]
	private ZoneEntityBSP _zoneEntity;

	[Space]
	[SerializeField]
	private XformNode _handLeftXform;

	[SerializeField]
	private XformNode _handRightXform;

	[SerializeField]
	private XformNode _bodyXform;

	[SerializeField]
	private XformNode _headXform;

	[Space]
	private int _id;

	private int _index;

	private bool _invalid;

	public const int MAX_PAWNS = 10;

	private static GorillaPawn[] _gPawns = new GorillaPawn[10];

	private static int _gPawnActiveCount = 0;

	private static Matrix4x4[] _gShaderData = new Matrix4x4[10];

	public VRRig rig => _rig;

	public ZoneEntityBSP zoneEntity => _zoneEntity;

	public new Transform transform => _transform;

	public XformNode handLeft => _handLeftXform;

	public XformNode handRight => _handRightXform;

	public XformNode body => _bodyXform;

	public XformNode head => _headXform;

	public static GorillaPawn[] AllPawns => _gPawns;

	public static int ActiveCount => _gPawnActiveCount;

	public static Matrix4x4[] ShaderData => _gShaderData;

	private void Awake()
	{
		Setup(force: false);
	}

	private void Setup(bool force)
	{
		_transform = base.transform;
		_rig = GetComponentInChildren<VRRig>();
		if (!_rig)
		{
			return;
		}
		_zoneEntity = _rig.zoneEntity;
		bool flag = force || _handLeft.AsNull() == null;
		bool flag2 = force || _handRight.AsNull() == null;
		bool flag3 = force || _head.AsNull() == null;
		if (!flag && !flag2 && !flag3)
		{
			return;
		}
		Transform[] bones = _rig.mainSkin.bones;
		foreach (Transform transform in bones)
		{
			string text = transform.name;
			if (flag3 && text.StartsWith("head", StringComparison.OrdinalIgnoreCase))
			{
				_head = transform;
				_headXform = new XformNode();
				_headXform.localPosition = new Vector3(0f, 0.13f, 0.015f);
				_headXform.radius = 0.12f;
				_headXform.parent = transform;
			}
			else if (flag && text.StartsWith("hand.L", StringComparison.OrdinalIgnoreCase))
			{
				_handLeft = transform;
				_handLeftXform = new XformNode();
				_handLeftXform.localPosition = new Vector3(-0.014f, 0.034f, 0f);
				_handLeftXform.radius = 0.044f;
				_handLeftXform.parent = transform;
			}
			else if (flag2 && text.StartsWith("hand.R", StringComparison.OrdinalIgnoreCase))
			{
				_handRight = transform;
				_handRightXform = new XformNode();
				_handRightXform.localPosition = new Vector3(0.014f, 0.034f, 0f);
				_handRightXform.radius = 0.044f;
				_handRightXform.parent = transform;
			}
		}
	}

	private bool CanRun()
	{
		if (_gPawnActiveCount > 10)
		{
			Debug.LogError($"Cannot register more than {10} pawns.");
			return false;
		}
		return true;
	}

	private void OnEnable()
	{
		if (CanRun())
		{
			_id = -1;
			if ((bool)_rig && _rig.OwningNetPlayer != null)
			{
				_id = _rig.OwningNetPlayer.ActorNumber;
			}
			_index = _gPawnActiveCount++;
			_gPawns[_index] = this;
		}
	}

	private void OnDisable()
	{
		_id = -1;
		if (CanRun() && _index >= 0 && _index < _gPawnActiveCount - 1)
		{
			int num = --_gPawnActiveCount;
			_gPawns.Swap(_index, num);
			_index = num;
		}
	}

	private void OnDestroy()
	{
		int num = _gPawns.IndexOfRef(this);
		_gPawns[num] = null;
		Array.Sort(_gPawns, ComparePawns);
		int i;
		for (i = 0; i < _gPawns.Length && (bool)_gPawns[i]; i++)
		{
		}
		_gPawnActiveCount = i;
	}

	private static int ComparePawns(GorillaPawn x, GorillaPawn y)
	{
		bool flag = x.AsNull() == null;
		bool flag2 = y.AsNull() == null;
		if (flag && flag2)
		{
			return 0;
		}
		if (flag)
		{
			return 1;
		}
		if (flag2)
		{
			return -1;
		}
		return x._index.CompareTo(y._index);
	}

	public static void SyncPawnData()
	{
		Matrix4x4[] gShaderData = _gShaderData;
		m4x4 m4x5 = default(m4x4);
		for (int i = 0; i < _gPawnActiveCount; i++)
		{
			GorillaPawn obj = _gPawns[i];
			Vector4 v = obj._headXform.worldPosition;
			Vector4 v2 = obj._bodyXform.worldPosition;
			Vector4 v3 = obj._handLeftXform.worldPosition;
			Vector4 v4 = obj._handRightXform.worldPosition;
			m4x5.SetRow0(ref v);
			m4x5.SetRow1(ref v2);
			m4x5.SetRow2(ref v3);
			m4x5.SetRow3(ref v4);
			m4x5.Push(ref gShaderData[i]);
		}
		for (int j = _gPawnActiveCount; j < 10; j++)
		{
			MatrixUtils.Clear(ref gShaderData[j]);
		}
	}
}
