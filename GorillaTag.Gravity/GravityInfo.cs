using System;
using UnityEngine;

namespace GorillaTag.Gravity;

[Serializable]
public struct GravityInfo
{
	public Vector3 gravityUpDirection;

	public Vector3 rotationDirection;

	public float rotationSpeed;

	public float gravityStrength;

	public bool rotate;
}
