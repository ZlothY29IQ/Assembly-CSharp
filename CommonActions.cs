using GorillaNetworking;
using UnityEngine;

public class CommonActions : MonoBehaviour
{
	public void LoadSavedOutfit(int index)
	{
		if ((bool)CosmeticsController.instance)
		{
			CosmeticsController.instance.LoadSavedOutfit(index);
		}
	}

	public void LoadPrevOutfit()
	{
		if ((bool)CosmeticsController.instance)
		{
			CosmeticsController.instance.PressWardrobeScrollOutfit(forward: false);
		}
	}

	public void LoadNextOutfit()
	{
		if ((bool)CosmeticsController.instance)
		{
			CosmeticsController.instance.PressWardrobeScrollOutfit(forward: true);
		}
	}
}
