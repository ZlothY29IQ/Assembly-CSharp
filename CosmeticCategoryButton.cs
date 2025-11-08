using UnityEngine;

public class CosmeticCategoryButton : CosmeticButton
{
	[SerializeField]
	private SpriteRenderer equippedIcon;

	[SerializeField]
	private SpriteRenderer equippedLeftIcon;

	[SerializeField]
	private SpriteRenderer equippedRightIcon;

	public void SetIcon(Sprite sprite)
	{
		equippedLeftIcon.enabled = false;
		equippedRightIcon.enabled = false;
		equippedIcon.enabled = sprite != null;
		equippedIcon.sprite = sprite;
	}

	public void SetDualIcon(Sprite leftSprite, Sprite rightSprite)
	{
		equippedLeftIcon.enabled = leftSprite != null;
		equippedRightIcon.enabled = rightSprite != null;
		equippedIcon.enabled = false;
		equippedLeftIcon.sprite = leftSprite;
		equippedRightIcon.sprite = rightSprite;
	}

	public override void UpdatePosition()
	{
		base.UpdatePosition();
		if (equippedIcon != null)
		{
			equippedIcon.transform.position += posOffset;
		}
		if (equippedLeftIcon != null)
		{
			equippedLeftIcon.transform.position += posOffset;
		}
		if (equippedRightIcon != null)
		{
			equippedRightIcon.transform.position += posOffset;
		}
	}
}
