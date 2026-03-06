using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GorillaTag;

[Serializable]
public struct XformOffset
{
	[Tooltip("The position of the offset relative to the parent bone.")]
	public Vector3 pos;

	[FormerlySerializedAs("_edRotQuat")]
	[FormerlySerializedAs("rot")]
	[HideInInspector]
	[SerializeField]
	private Quaternion _rotQuat;

	[FormerlySerializedAs("_edRotEulerAngles")]
	[FormerlySerializedAs("_edRotEuler")]
	[HideInInspector]
	[SerializeField]
	private Vector3 _rotEulerAngles;

	[Tooltip("The scale of the offset relative to the parent bone.")]
	public Vector3 scale;

	public static readonly XformOffset Identity;

	[Tooltip("The rotation of the offset relative to the parent bone.")]
	public Quaternion rot
	{
		get
		{
			return _rotQuat;
		}
		set
		{
			_rotQuat = value;
		}
	}

	public XformOffset(Vector3 pos, Quaternion rot, Vector3 scale)
	{
		this.pos = pos;
		_rotQuat = rot;
		_rotEulerAngles = rot.eulerAngles;
		this.scale = scale;
	}

	public XformOffset(Vector3 pos, Vector3 rot, Vector3 scale)
	{
		this.pos = pos;
		_rotQuat = Quaternion.Euler(rot);
		_rotEulerAngles = rot;
		this.scale = scale;
	}

	public XformOffset(Vector3 pos, Quaternion rot)
	{
		this.pos = pos;
		_rotQuat = rot;
		_rotEulerAngles = rot.eulerAngles;
		scale = Vector3.one;
	}

	public XformOffset(Vector3 pos, Vector3 rot)
	{
		this.pos = pos;
		_rotQuat = Quaternion.Euler(rot);
		_rotEulerAngles = rot;
		scale = Vector3.one;
	}

	public XformOffset(Transform parentXform, Transform childXform)
	{
		pos = parentXform.InverseTransformPoint(childXform.position);
		_rotQuat = Quaternion.Inverse(parentXform.rotation) * childXform.rotation;
		_rotEulerAngles = _rotQuat.eulerAngles;
		scale = childXform.lossyScale.SafeDivide(parentXform.lossyScale);
	}

	public XformOffset(Matrix4x4 matrix)
	{
		pos = matrix.GetPosition();
		scale = matrix.lossyScale;
		if (Vector3.Dot(Vector3.Cross(matrix.GetColumn(0), matrix.GetColumn(1)), matrix.GetColumn(2)) < 0f)
		{
			scale = -scale;
		}
		Matrix4x4 matrix4x = matrix;
		matrix4x.SetColumn(0, matrix4x.GetColumn(0) / scale.x);
		matrix4x.SetColumn(1, matrix4x.GetColumn(1) / scale.y);
		matrix4x.SetColumn(2, matrix4x.GetColumn(2) / scale.z);
		_rotQuat = Quaternion.LookRotation(matrix4x.GetColumn(2), matrix4x.GetColumn(1));
		_rotEulerAngles = _rotQuat.eulerAngles;
	}

	public bool Approx(XformOffset other)
	{
		if (pos.Approx(other.pos) && _rotQuat.Approx(other._rotQuat))
		{
			return scale.Approx(other.scale);
		}
		return false;
	}

	static XformOffset()
	{
		Identity = new XformOffset
		{
			_rotQuat = Quaternion.identity,
			scale = Vector3.one
		};
	}
}
