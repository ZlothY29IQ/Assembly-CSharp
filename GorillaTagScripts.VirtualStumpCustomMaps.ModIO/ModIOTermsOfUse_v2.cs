using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Modio;
using Modio.Customizations;
using UnityEngine;

namespace GorillaTagScripts.VirtualStumpCustomMaps.ModIO;

public class ModIOTermsOfUse_v2 : LegalAgreements
{
	[SerializeField]
	private string confirmString = "Press and Hold to Confirm";

	private static ModIOTermsOfUse_v2 modioTermsInstance;

	private TermsOfUse termsOfUse;

	private Agreement fullTermsOfUse;

	private Agreement fullPrivacyPolicy;

	private string cachedTermsText;

	protected override void Awake()
	{
		if (modioTermsInstance != null)
		{
			Debug.LogError("Trying to set [LegalAgreements] instance but it is not null", this);
			base.gameObject.SetActive(value: false);
			return;
		}
		modioTermsInstance = this;
		stickHeldDuration = 0f;
		scrollSpeed = _minScrollSpeed;
		base.enabled = false;
	}

	public async Task<Error> ShowTerms()
	{
		_ = Error.None;
		Error error;
		(error, termsOfUse) = await TermsOfUse.Get();
		if ((bool)error)
		{
			GTDev.LogError($"[ModIOTermsOfUse::ShowTerms] Failed to get TermsOfUse from Mod.io: {error}");
			return error;
		}
		(error, fullTermsOfUse) = await Agreement.GetAgreement(AgreementType.TermsOfUse);
		if ((bool)error)
		{
			GTDev.LogError($"[ModIOTermsOfUse::ShowTerms] Failed to get full Terms of Use Agreement from Mod.io: {error}");
			return error;
		}
		(error, fullPrivacyPolicy) = await Agreement.GetAgreement(AgreementType.PrivacyPolicy);
		if ((bool)error)
		{
			GTDev.LogError($"[ModIOTermsOfUse::ShowTerms] Failed to get full Privacy Policy Agreement from Mod.io: {error}");
			return error;
		}
		base.enabled = true;
		await StartLegalAgreements();
		return Error.None;
	}

	public override async Task StartLegalAgreements()
	{
		if (!legalAgreementsStarted)
		{
			legalAgreementsStarted = true;
			PrivateUIRoom.ForceStartOverlay();
			PrivateUIRoom.AddUI(uiParent);
			_pressAndHoldToConfirmButton.SetText(confirmString);
			_pressAndHoldToConfirmButton.gameObject.SetActive(value: false);
			HandRayController.Instance.EnableHandRays();
			UpdateTextFromTerms();
			await WaitForAcknowledgement();
			scrollBar.value = 1f;
			PrivateUIRoom.StopForcedOverlay();
			PrivateUIRoom.RemoveUI(uiParent);
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void UpdateTextFromTerms()
	{
		tmpTitle.text = "Mod.io Terms of Use";
		tmpBody.text = "Loading...";
		cachedTermsText = termsOfUse.TermsText + "\n\n";
		cachedTermsText = cachedTermsText + FormatAgreementText(fullTermsOfUse) + "\n\n\n";
		cachedTermsText += FormatAgreementText(fullPrivacyPolicy);
		tmpBody.text = cachedTermsText;
	}

	private string FormatAgreementText(Agreement agreement)
	{
		string input = agreement.Name + "\n\nEffective Date: " + agreement.DateLive.ToLongDateString() + "\n\n" + agreement.Content;
		input = Regex.Replace(input, "<!--[^>]*(-->)", "");
		input = input.Replace("<h1>", "<b>");
		input = input.Replace("</h1>", "</b>");
		input = input.Replace("<h2>", "<b>");
		input = input.Replace("</h2>", "</b>");
		input = input.Replace("<h3>", "<b>");
		input = input.Replace("</h3>", "</b>");
		input = input.Replace("<hr>", "");
		input = input.Replace("<br>", "\n");
		input = input.Replace("</li>", "</indent>\n");
		input = input.Replace("<strong>", "<b>");
		input = input.Replace("</strong>", "</b>");
		input = input.Replace("<em>", "<i>");
		input = input.Replace("</em>", "</i>");
		input = Regex.Replace(input, "<a[^>]*>{1}", "");
		input = input.Replace("</a>", "");
		Match match = Regex.Match(input, "<p[^>]*align:center[^>]*>{1}");
		while (match.Success)
		{
			input = input.Remove(match.Index, match.Length);
			input = input.Insert(match.Index, "\n<align=\"center\">");
			int startIndex = input.IndexOf("</p>", match.Index, StringComparison.Ordinal);
			input = input.Remove(startIndex, 4);
			input = input.Insert(startIndex, "</align>");
			match = Regex.Match(input, "<p[^>]*align:center[^>]*>{1}");
		}
		input = input.Replace("<p>", "\n");
		input = input.Replace("</p>", "");
		input = Regex.Replace(input, "<ol[^>]*>{1}", "<ol>");
		int num = input.IndexOf("<ol>", StringComparison.OrdinalIgnoreCase);
		bool flag = num != -1;
		while (flag)
		{
			int num2 = input.IndexOf("</ol>", num, StringComparison.OrdinalIgnoreCase);
			input = input.Remove(num, "<ol>".Length);
			int num3 = input.IndexOf("<li>", num, StringComparison.OrdinalIgnoreCase);
			bool flag2 = num3 != -1;
			int num4 = 0;
			while (flag2)
			{
				input = input.Remove(num3, "<li>".Length);
				input = input.Insert(num3, GetStringForListItemIdx_LowerAlpha(num4++));
				num2 = input.IndexOf("</ol>", num, StringComparison.OrdinalIgnoreCase);
				num3 = input.IndexOf("<li>", num, StringComparison.OrdinalIgnoreCase);
				flag2 = num3 != -1 && num3 < num2;
			}
			input = input.Remove(num2, "</ol>".Length);
			num = input.IndexOf("<ol>", StringComparison.OrdinalIgnoreCase);
			flag = num != -1;
		}
		input = Regex.Replace(input, "<ul[^>]*>{1}", "<ul>");
		int num5 = input.IndexOf("<ul>", StringComparison.OrdinalIgnoreCase);
		bool flag3 = num5 != -1;
		while (flag3)
		{
			int num6 = input.IndexOf("</ul>", num5, StringComparison.OrdinalIgnoreCase);
			input = input.Remove(num5, "<ul>".Length);
			int num7 = input.IndexOf("<li>", num5, StringComparison.OrdinalIgnoreCase);
			bool flag4 = num7 != -1;
			while (flag4)
			{
				input = input.Remove(num7, "<li>".Length);
				input = input.Insert(num7, "  - <indent=5%>");
				num6 = input.IndexOf("</ul>", num5, StringComparison.OrdinalIgnoreCase);
				num7 = input.IndexOf("<li>", num5, StringComparison.OrdinalIgnoreCase);
				flag4 = num7 != -1 && num7 < num6;
			}
			input = input.Remove(num6, "</ul>".Length);
			num5 = input.IndexOf("<ul>", StringComparison.OrdinalIgnoreCase);
			flag3 = num5 != -1;
		}
		input = Regex.Replace(input, "<table[^>]*>{1}", "");
		input = input.Replace("<tbody>", "");
		input = input.Replace("<tr>", "");
		input = input.Replace("<td>", "");
		input = input.Replace("<center>", "");
		input = input.Replace("</table>", "");
		input = input.Replace("</tbody>", "");
		input = input.Replace("</tr>", "\n");
		input = input.Replace("</td>", "");
		return input.Replace("</center>", "");
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
}
