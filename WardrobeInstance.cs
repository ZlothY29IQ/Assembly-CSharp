using GorillaNetworking;
using UnityEngine;

public class WardrobeInstance : MonoBehaviour
{
	public WardrobeItemButton[] wardrobeItemButtons;

	public HeadModel selfDoll;

	public void Start()
	{
		CosmeticsController.instance.AddWardrobeInstance(this);
	}

	public void OnDestroy()
	{
		CosmeticsController.instance.RemoveWardrobeInstance(this);
	}
}
