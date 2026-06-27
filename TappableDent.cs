using UnityEngine;

public class TappableDent : Tappable
{
	[SerializeField]
	private int numTapsToDestroy = 3;

	[SerializeField]
	private Vector3 finalLocalOffset;

	[SerializeField]
	private Vector3 finalLocalScale;

	[SerializeField]
	private GameObject parent;

	private int numTapsSoFar;

	private Vector3 offsetPerTap;

	private Vector3 scaleOffsetPerTap;

	private void Start()
	{
		if (parent == null)
		{
			parent = base.gameObject;
		}
		offsetPerTap = base.transform.parent.InverseTransformVector(base.transform.TransformVector(finalLocalOffset / numTapsToDestroy));
		scaleOffsetPerTap = (finalLocalScale - base.transform.localScale) / numTapsToDestroy;
	}

	public override void OnTapLocal(float tapStrength, float tapTime, PhotonMessageInfoWrapped info)
	{
		numTapsSoFar++;
		if (numTapsSoFar >= numTapsToDestroy)
		{
			parent.SetActive(value: false);
			return;
		}
		base.transform.localPosition += offsetPerTap;
		base.transform.localScale += scaleOffsetPerTap;
	}
}
