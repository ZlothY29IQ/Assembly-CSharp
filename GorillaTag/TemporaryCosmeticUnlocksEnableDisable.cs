using GorillaExtensions;
using GorillaNetworking;
using UnityEngine;

namespace GorillaTag;

public class TemporaryCosmeticUnlocksEnableDisable : MonoBehaviour
{
	[SerializeField]
	private CosmeticWardrobe m_wardrobe;

	[SerializeField]
	private GameObject m_cosmeticAreaTrigger;

	private TickSystemTimer m_timer;

	private void Awake()
	{
		if (m_wardrobe.IsNull() || m_cosmeticAreaTrigger.IsNull())
		{
			Debug.LogError("TemporaryCosmeticUnlocksEnableDisable: reference is null, disabling self");
			base.enabled = false;
		}
		if (CosmeticsController.instance.IsNull() || !m_wardrobe.WardrobeButtonsInitialized())
		{
			base.enabled = false;
			m_timer = new TickSystemTimer(0.05f, CheckWardrobeRady);
			m_timer.Start();
		}
	}

	private void OnEnable()
	{
		bool tempUnlocksEnabled = PlayerCosmeticsSystem.TempUnlocksEnabled;
		m_wardrobe.UseTemporarySet = tempUnlocksEnabled;
		m_cosmeticAreaTrigger.SetActive(tempUnlocksEnabled);
	}

	private void CheckWardrobeRady()
	{
		if (CosmeticsController.instance.IsNotNull() && m_wardrobe.WardrobeButtonsInitialized())
		{
			m_timer.Stop();
			m_timer = null;
			base.enabled = true;
		}
		else
		{
			m_timer.Start();
		}
	}
}
