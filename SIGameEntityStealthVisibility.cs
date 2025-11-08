using GorillaLocomotion;
using UnityEngine;

public class SIGameEntityStealthVisibility : MonoBehaviour
{
	[SerializeField]
	private Renderer[] stealthedComponents;

	[SerializeField]
	private float revealRange = 5f;

	[SerializeField]
	private float hideRange = 8f;

	private bool isStealthed;

	private void OnEnable()
	{
		revealRange = Mathf.Min(revealRange, hideRange);
	}

	private void OnDisable()
	{
		SetVisibility(visible: true);
	}

	private void LateUpdate()
	{
		Vector3 position = GTPlayer.Instance.transform.position;
		float num = Vector3.SqrMagnitude(base.transform.position - position);
		if (isStealthed && num < revealRange * revealRange)
		{
			SetVisibility(visible: true);
		}
		else if (!isStealthed && num > hideRange * hideRange)
		{
			SetVisibility(visible: false);
		}
	}

	private void SetVisibility(bool visible)
	{
		isStealthed = !visible;
		for (int i = 0; i < stealthedComponents.Length; i++)
		{
			stealthedComponents[i].enabled = visible;
		}
	}
}
