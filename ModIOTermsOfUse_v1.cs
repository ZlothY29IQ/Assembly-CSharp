using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class ModIOTermsOfUse_v1 : MonoBehaviour
{
	[SerializeField]
	private Transform uiParent;

	[SerializeField]
	private string title;

	[SerializeField]
	private TMP_Text tmpBody;

	[SerializeField]
	private TMP_Text tmpTitle;

	[SerializeField]
	private TMP_Text tmpPage;

	[SerializeField]
	public GameObject yesNoButtons;

	[SerializeField]
	public GameObject nextButton;

	[SerializeField]
	public GameObject prevButton;

	private bool hasTermsOfUse;

	private Action<bool> termsAcknowledgedCallback;

	private string cachedTermsText;

	private bool waitingForAcknowledge;

	private bool accepted;

	private bool acceptButtonDown;

	[SerializeField]
	private float holdTime = 5f;

	[SerializeField]
	private LineRenderer progressBar;

	private void OnEnable()
	{
		if ((bool)ControllerBehaviour.Instance)
		{
			ControllerBehaviour.Instance.OnAction += PostUpdate;
		}
	}

	private void OnDisable()
	{
		if ((bool)ControllerBehaviour.Instance)
		{
			ControllerBehaviour.Instance.OnAction -= PostUpdate;
		}
	}

	private void PostUpdate()
	{
		if (ControllerBehaviour.Instance.IsLeftStick)
		{
			TurnPage(-1);
		}
		if (ControllerBehaviour.Instance.IsRightStick)
		{
			TurnPage(1);
		}
		if (waitingForAcknowledge)
		{
			acceptButtonDown = ControllerBehaviour.Instance.ButtonDown;
		}
	}

	private async void Start()
	{
		while (!hasTermsOfUse)
		{
			await Task.Yield();
		}
		PrivateUIRoom.AddUI(uiParent);
		if (!(await UpdateTextFromTerms()))
		{
			while (true)
			{
				await Task.Yield();
			}
		}
		await WaitForAcknowledgement();
		termsAcknowledgedCallback?.Invoke(accepted);
		PrivateUIRoom.RemoveUI(uiParent);
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private async Task<bool> UpdateTextFromTerms()
	{
		tmpTitle.text = title;
		tmpBody.text = "Loading...";
		bool num = await UpdateTextWithFullTerms();
		if (!num)
		{
			tmpBody.text = "Failed to retrieve full Terms of Use text from mod.io.\n\nPlease restart the game and try again.";
			tmpBody.pageToDisplay = 1;
			tmpPage.text = string.Empty;
		}
		return num;
	}

	public async Task<bool> UpdateTextWithFullTerms()
	{
		return true;
	}

	private string GetStringForListItemIdx_LowerAlpha(int idx)
	{
		return idx switch
		{
			0 => "  a. <indent=5%>", 
			1 => "  b. <indent=5%>", 
			2 => "  c. <indent=5%>", 
			3 => "  d. <indent=5%>", 
			4 => "  e. <indent=5%>", 
			5 => "  f. <indent=5%>", 
			6 => "  g. <indent=5%>", 
			7 => "  h. <indent=5%>", 
			8 => "  i. <indent=5%>", 
			9 => "  j. <indent=5%>", 
			10 => "  k. <indent=5%>", 
			11 => "  l. <indent=5%>", 
			12 => "  m. <indent=5%>", 
			13 => "  n. <indent=5%>", 
			14 => "  o. <indent=5%>", 
			15 => "  p. <indent=5%>", 
			16 => "  q. <indent=5%>", 
			17 => "  r. <indent=5%>", 
			18 => "  s. <indent=5%>", 
			19 => "  t. <indent=5%>", 
			20 => "  u. <indent=5%>", 
			21 => "  v. <indent=5%>", 
			22 => "  w. <indent=5%>", 
			23 => "  x. <indent=5%>", 
			24 => "  y. <indent=5%>", 
			25 => "  z. <indent=5%>", 
			_ => "", 
		};
	}

	private async Task WaitForAcknowledgement()
	{
		accepted = false;
		float progress = 0f;
		progressBar.transform.localScale = new Vector3(0f, 1f, 1f);
		while (progress < 1f)
		{
			progress = ((!acceptButtonDown) ? 0f : (progress + Time.deltaTime / holdTime));
			progressBar.transform.localScale = new Vector3(Mathf.Clamp01(progress), 1f, 1f);
			progressBar.textureScale = new Vector2(Mathf.Clamp01(progress), -1f);
			await Task.Yield();
		}
		if (progress >= 1f)
		{
			Acknowledge(acceptButtonDown);
		}
	}

	public void TurnPage(int i)
	{
		tmpBody.pageToDisplay = Mathf.Clamp(tmpBody.pageToDisplay + i, 1, tmpBody.textInfo.pageCount);
		tmpPage.text = $"page {tmpBody.pageToDisplay} of {tmpBody.textInfo.pageCount}";
		nextButton.SetActive(tmpBody.pageToDisplay < tmpBody.textInfo.pageCount);
		prevButton.SetActive(tmpBody.pageToDisplay > 1);
		ActivateAcceptButtonGroup();
	}

	private void ActivateAcceptButtonGroup()
	{
		bool active = tmpBody.pageToDisplay == tmpBody.textInfo.pageCount;
		yesNoButtons.SetActive(active);
		waitingForAcknowledge = active;
	}

	public void Acknowledge(bool didAccept)
	{
		accepted = didAccept;
	}
}
