using System;
using System.Collections.Generic;
using System.Linq;
using BoingKit;
using UnityEngine;

public class PoseStiffnessComparison : MonoBehaviour
{
	public float Run = 11f;

	public float Tilt = 15f;

	public float Period = 3f;

	public float Rest = 3f;

	public Transform BonesA;

	public Transform BonesB;

	private float m_yA;

	private float m_yB;

	private float m_timer;

	private void Start()
	{
		m_timer = 0f;
		m_yA = BonesA.position.y;
		m_yB = BonesB.position.y;
	}

	private void FixedUpdate()
	{
		BoingBones[] components = BonesA.GetComponents<BoingBones>();
		BoingBones[] components2 = BonesB.GetComponents<BoingBones>();
		Transform[] source = new Transform[2] { BonesA.transform, BonesB.transform };
		float[] source2 = new float[2] { m_yA, m_yB };
		IEnumerable<BoingBones> enumerable = components.Concat(components2);
		float fixedDeltaTime = Time.fixedDeltaTime;
		float num = 0.5f * Run;
		m_timer += fixedDeltaTime;
		if (m_timer > Period + Rest)
		{
			m_timer = Mathf.Repeat(m_timer, Period + Rest);
			for (int i = 0; i < 2; i++)
			{
				Transform obj = source.ElementAt(i);
				float y = source2.ElementAt(i);
				Vector3 position = obj.position;
				position.y = y;
				position.z = 0f - num;
				obj.position = position;
			}
			foreach (BoingBones item in enumerable)
			{
				item.Reboot();
			}
		}
		float num2 = Mathf.Min(1f, m_timer * MathUtil.InvSafe(Period));
		float num3 = 1f - Mathf.Pow(1f - num2, 1.5f);
		for (int j = 0; j < 2; j++)
		{
			Transform obj2 = source.ElementAt(j);
			float num4 = source2.ElementAt(j);
			Vector3 position2 = obj2.position;
			position2.y = num4 + 2f * Mathf.Sin(MathF.PI * 4f * num3);
			position2.z = Mathf.Lerp(0f - num, num, num3);
			obj2.position = position2;
		}
	}
}
