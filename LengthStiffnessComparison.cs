using System.Collections.Generic;
using System.Linq;
using BoingKit;
using UnityEngine;

public class LengthStiffnessComparison : MonoBehaviour
{
	public float Run = 11f;

	public float Tilt = 15f;

	public float Period = 3f;

	public float Rest = 3f;

	public Transform BonesA;

	public Transform BonesB;

	private float m_timer;

	private void Start()
	{
		m_timer = 0f;
	}

	private void FixedUpdate()
	{
		BoingBones[] components = BonesA.GetComponents<BoingBones>();
		BoingBones[] components2 = BonesB.GetComponents<BoingBones>();
		Transform[] array = new Transform[2] { BonesA.transform, BonesB.transform };
		IEnumerable<BoingBones> enumerable = components.Concat(components2);
		float fixedDeltaTime = Time.fixedDeltaTime;
		float num = 0.5f * Run;
		m_timer += fixedDeltaTime;
		Transform[] array2;
		if (m_timer > Period + Rest)
		{
			m_timer = Mathf.Repeat(m_timer, Period + Rest);
			array2 = array;
			foreach (Transform obj in array2)
			{
				Vector3 position = obj.position;
				position.z = 0f - num;
				obj.position = position;
			}
			foreach (BoingBones item in enumerable)
			{
				item.Reboot();
			}
		}
		float num2 = Mathf.Min(1f, m_timer * MathUtil.InvSafe(Period));
		float num3 = 1f - Mathf.Pow(1f - num2, 6f);
		array2 = array;
		foreach (Transform obj2 in array2)
		{
			Vector3 position2 = obj2.position;
			position2.z = Mathf.Lerp(0f - num, num, num3);
			obj2.position = position2;
			obj2.rotation = Quaternion.AngleAxis(Tilt * (1f - num3), Vector3.right);
		}
	}
}
