using UnityEngine;

public class ScalerUpper : MonoBehaviour
{
	[SerializeField]
	private Transform[] target;

	[SerializeField]
	private AnimationCurve scaleCurve;

	private float t;

	private void Update()
	{
		for (int i = 0; i < target.Length; i++)
		{
			target[i].transform.localScale = Vector3.one * scaleCurve.Evaluate(t);
		}
		t += Time.deltaTime;
	}

	private void OnEnable()
	{
		t = 0f;
	}

	private void OnDisable()
	{
		for (int i = 0; i < target.Length; i++)
		{
			target[i].transform.localScale = Vector3.one;
		}
	}
}
