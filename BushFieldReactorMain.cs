using System;
using System.Collections.Generic;
using BoingKit;
using UnityEngine;

public class BushFieldReactorMain : MonoBehaviour
{
	public GameObject Bush;

	public GameObject Blossom;

	public GameObject Sphere;

	public int NumBushes;

	public Vector2 BushScaleRange;

	public int NumBlossoms;

	public Vector2 BlossomScaleRange;

	public Vector2 FieldBounds;

	public int NumSpheresPerCircle;

	public int NumCircles;

	public float MaxCircleRadius;

	public float CircleSpeed;

	private List<GameObject> m_aSphere;

	private float m_basePhase;

	public void Start()
	{
		UnityEngine.Random.InitState(0);
		for (int i = 0; i < NumBushes; i++)
		{
			GameObject obj = UnityEngine.Object.Instantiate(Bush);
			float num = UnityEngine.Random.Range(BushScaleRange.x, BushScaleRange.y);
			obj.transform.position = new Vector3(UnityEngine.Random.Range(-0.5f * FieldBounds.x, 0.5f * FieldBounds.x), 0.2f * num, UnityEngine.Random.Range(-0.5f * FieldBounds.y, 0.5f * FieldBounds.y));
			obj.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
			obj.transform.localScale = num * Vector3.one;
			BoingBehavior component = obj.GetComponent<BoingBehavior>();
			if (component != null)
			{
				component.Reboot();
			}
		}
		for (int j = 0; j < NumBlossoms; j++)
		{
			GameObject obj2 = UnityEngine.Object.Instantiate(Blossom);
			float num2 = UnityEngine.Random.Range(BlossomScaleRange.x, BlossomScaleRange.y);
			obj2.transform.position = new Vector3(UnityEngine.Random.Range(-0.5f * FieldBounds.x, 0.5f * FieldBounds.y), 0.2f * num2, UnityEngine.Random.Range(-0.5f * FieldBounds.y, 0.5f * FieldBounds.y));
			obj2.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
			obj2.transform.localScale = num2 * Vector3.one;
			BoingBehavior component2 = obj2.GetComponent<BoingBehavior>();
			if (component2 != null)
			{
				component2.Reboot();
			}
		}
		m_aSphere = new List<GameObject>(NumSpheresPerCircle * NumCircles);
		for (int k = 0; k < NumCircles; k++)
		{
			for (int l = 0; l < NumSpheresPerCircle; l++)
			{
				m_aSphere.Add(UnityEngine.Object.Instantiate(Sphere));
			}
		}
		m_basePhase = 0f;
	}

	public void Update()
	{
		int num = 0;
		for (int i = 0; i < NumCircles; i++)
		{
			float num2 = MaxCircleRadius / (float)(i + 1);
			for (int j = 0; j < NumSpheresPerCircle; j++)
			{
				float num3 = m_basePhase + (float)j / (float)NumSpheresPerCircle * 2f * MathF.PI;
				num3 *= ((i % 2 == 0) ? 1f : (-1f));
				m_aSphere[num].transform.position = new Vector3(num2 * Mathf.Cos(num3), 0.2f, num2 * Mathf.Sin(num3));
				num++;
			}
		}
		m_basePhase -= CircleSpeed / MaxCircleRadius * Time.deltaTime;
	}
}
