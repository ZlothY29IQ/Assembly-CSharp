using TMPro;
using UnityEngine;

namespace GorillaTagScripts.UI.ModIO;

public class VirtualStumpTeleportingHUD : MonoBehaviour
{
	private const string VIRT_STUMP_HUD_ENTERING_KEY = "VIRT_STUMP_HUD_ENTERING";

	private const string VIRT_STUMP_HUD_LEAVING_KEY = "VIRT_STUMP_HUD_LEAVING";

	[SerializeField]
	private string enteringVirtualStumpString = "Now Entering the Virtual Stump";

	[SerializeField]
	private string leavingVirtualStumpString = "Now Leaving the Virtual Stump";

	[SerializeField]
	private TMP_Text teleportingStatusText;

	[SerializeField]
	private int maxNumProgressDots = 3;

	[SerializeField]
	private float textUpdateInterval = 0.5f;

	private float lastTextUpdateTime;

	private int numProgressDots;

	private bool isEnteringVirtualStump;

	public void Initialize(bool isEntering)
	{
		isEnteringVirtualStump = isEntering;
		if (isEntering)
		{
			if (!LocalisationManager.TryGetKeyForCurrentLocale("VIRT_STUMP_HUD_ENTERING", out var result, enteringVirtualStumpString))
			{
				Debug.LogError("[LOCALIZATION::VIRT_STUMP_TELEPORT_HUD] Failed to retrieve key [VIRT_STUMP_HUD_ENTERING] for locale [" + LocalisationManager.CurrentLanguage.LocaleName + "]");
			}
			teleportingStatusText.text = result;
			teleportingStatusText.gameObject.SetActive(value: true);
		}
		else
		{
			if (!LocalisationManager.TryGetKeyForCurrentLocale("VIRT_STUMP_HUD_LEAVING", out var result2, leavingVirtualStumpString))
			{
				Debug.LogError("[LOCALIZATION::VIRT_STUMP_TELEPORT_HUD] Failed to retrieve key [VIRT_STUMP_HUD_LEAVING] for locale [" + LocalisationManager.CurrentLanguage.LocaleName + "]");
			}
			teleportingStatusText.text = result2;
			teleportingStatusText.gameObject.SetActive(value: true);
		}
	}

	private void Update()
	{
		if (Time.time - lastTextUpdateTime > textUpdateInterval)
		{
			lastTextUpdateTime = Time.time;
			IncrementProgressDots();
			teleportingStatusText.text = (isEnteringVirtualStump ? enteringVirtualStumpString : leavingVirtualStumpString);
			for (int i = 0; i < numProgressDots; i++)
			{
				teleportingStatusText.text += ".";
			}
		}
	}

	private void IncrementProgressDots()
	{
		numProgressDots++;
		if (numProgressDots > maxNumProgressDots)
		{
			numProgressDots = 0;
		}
	}
}
