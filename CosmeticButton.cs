using GorillaExtensions;
using UnityEngine;

public class CosmeticButton : GorillaPressableButton
{
	[SerializeField]
	private Vector3 pressedOffset = new Vector3(0f, 0f, 0.1f);

	[SerializeField]
	private Material disabledMaterial;

	[SerializeField]
	private Vector3 disabledOffset = new Vector3(0f, 0f, 0.1f);

	private Vector3 startingPos;

	protected Vector3 posOffset;

	public bool Initialized { get; private set; }

	public void Awake()
	{
		startingPos = base.transform.localPosition;
		Initialized = true;
	}

	public override void UpdateColor()
	{
		if (!base.enabled)
		{
			buttonRenderer.material = disabledMaterial;
			SetOffText(myText != null);
		}
		else if (isOn)
		{
			buttonRenderer.material = pressedMaterial;
			SetOnText(myText.IsNotNull());
		}
		else
		{
			buttonRenderer.material = unpressedMaterial;
			SetOffText(myText != null);
		}
		UpdatePosition();
	}

	public virtual void UpdatePosition()
	{
		Vector3 localPosition = startingPos;
		if (!base.enabled)
		{
			localPosition += disabledOffset;
		}
		else if (isOn)
		{
			localPosition += pressedOffset;
		}
		posOffset = base.transform.position;
		base.transform.localPosition = localPosition;
		posOffset = base.transform.position - posOffset;
		if (myText != null)
		{
			myText.transform.position += posOffset;
		}
		if (myTmpText != null)
		{
			myTmpText.transform.position += posOffset;
		}
		if (myTmpText2 != null)
		{
			myTmpText2.transform.position += posOffset;
		}
	}
}
