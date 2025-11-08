using System.Collections.Generic;
using System.Threading.Tasks;
using GorillaExtensions;
using GorillaTagScripts.VirtualStumpCustomMaps.UI;
using Modio;
using Modio.API;
using Modio.Mods;
using TMPro;
using UnityEngine;

public class CustomMapsSearchScreen : CustomMapsTerminalScreen
{
	[SerializeField]
	private TMP_Text searchPhraseText;

	[SerializeField]
	private TMP_Text searchMessageText;

	[SerializeField]
	private CustomMapsGalleryView customMapsGalleryView;

	[SerializeField]
	private GameObject leftPageButton;

	[SerializeField]
	private GameObject rightPageButton;

	[SerializeField]
	private string defaultSearchString = "SEARCH PHRASE";

	[SerializeField]
	private string noMapsFoundString = "NO RESULTS FOUND";

	[SerializeField]
	private string searchingString = "SEARCHING";

	[SerializeField]
	private int numModsPerRequest = 60;

	[SerializeField]
	private int modsPerPage = 6;

	private string searchPhrase = "";

	private List<Mod> searchedMods = new List<Mod>();

	private List<Mod> filteredSearchedMods = new List<Mod>();

	private List<Mod> displayedMods = new List<Mod>();

	private int currentSearchModsRequestPage;

	private bool loadingSearchMods;

	private bool errorLoadingSearchMods;

	private int totalSearchMods;

	private int currentModPage;

	private string errorMessage = "";

	public override void Show()
	{
		base.Show();
		searchPhraseText.gameObject.SetActive(value: true);
		customMapsGalleryView.gameObject.SetActive(value: false);
		searchMessageText.gameObject.SetActive(value: false);
		leftPageButton.gameObject.SetActive(value: false);
		rightPageButton.gameObject.SetActive(value: false);
		searchedMods.Clear();
		filteredSearchedMods.Clear();
		displayedMods.Clear();
		searchPhraseText.text = defaultSearchString;
		searchPhrase = string.Empty;
		currentSearchModsRequestPage = 0;
		currentModPage = 0;
	}

	public override void Hide()
	{
		base.Hide();
		customMapsGalleryView.ShowTileText(show: false, useMapName: true);
	}

	public override void Initialize()
	{
	}

	public void ReturnFromDetailsScreen()
	{
		base.Show();
		customMapsGalleryView.ShowTileText(show: true, useMapName: true);
	}

	public override void PressButton(CustomMapKeyboardBinding pressedButton)
	{
		if (Time.time < showTime + activationTime || !CustomMapsTerminal.IsDriver)
		{
			return;
		}
		if (CustomMapKeyboardBinding.tile1 <= pressedButton && pressedButton <= CustomMapKeyboardBinding.tile6 && !customMapsGalleryView.IsNull())
		{
			customMapsGalleryView.ShowDetailsForEntry((int)(pressedButton - 62));
		}
		if (pressedButton < CustomMapKeyboardBinding.up)
		{
			string text = searchPhrase;
			int num = (int)pressedButton;
			searchPhrase = text + num;
			RefreshSearchText();
			return;
		}
		if (pressedButton > CustomMapKeyboardBinding.option3 && pressedButton < CustomMapKeyboardBinding.at)
		{
			searchPhrase += pressedButton;
			RefreshSearchText();
			return;
		}
		switch (pressedButton)
		{
		case CustomMapKeyboardBinding.goback:
			if (loadingSearchMods)
			{
				return;
			}
			CustomMapsTerminal.ReturnFromSearchScreen();
			break;
		case CustomMapKeyboardBinding.enter:
			if (loadingSearchMods)
			{
				return;
			}
			searchedMods.Clear();
			filteredSearchedMods.Clear();
			currentSearchModsRequestPage = 0;
			searchMessageText.gameObject.SetActive(value: true);
			searchMessageText.text = searchingString;
			RetrieveMods();
			break;
		case CustomMapKeyboardBinding.delete:
			if (!searchPhrase.IsNullOrEmpty())
			{
				searchPhrase = searchPhrase.Remove(searchPhrase.Length - 1);
			}
			break;
		case CustomMapKeyboardBinding.right:
			currentModPage++;
			RefreshScreenState();
			break;
		case CustomMapKeyboardBinding.left:
			currentModPage--;
			RefreshScreenState();
			break;
		}
		RefreshSearchText();
	}

	private void RefreshSearchText()
	{
		if (searchPhrase.IsNullOrEmpty())
		{
			searchPhraseText.text = defaultSearchString;
		}
		else
		{
			searchPhraseText.text = searchPhrase;
		}
	}

	private async Task RetrieveMods()
	{
		if (!loadingSearchMods)
		{
			loadingSearchMods = true;
			ModSearchFilter modSearchFilter = new ModSearchFilter(currentSearchModsRequestPage++, numModsPerRequest);
			Filtering filtering = Filtering.Like;
			modSearchFilter.ClearSearchPhrases(filtering);
			if (!searchPhrase.IsNullOrEmpty())
			{
				modSearchFilter.AddSearchPhrase(searchPhrase, filtering);
			}
			(Error, ModioPage<Mod>) obj = await ModIOManager.GetMods(modSearchFilter.GetModsFilter());
			Error item = obj.Item1;
			ModioPage<Mod> item2 = obj.Item2;
			loadingSearchMods = false;
			if ((bool)item)
			{
				errorLoadingSearchMods = true;
				errorMessage = item.GetMessage();
				GTDev.LogError("[CustomMapsListScreen::OnAvailableModsRetrieved] Failed to retrieve mods. Error: " + item.GetMessage());
			}
			else
			{
				totalSearchMods = (int)item2.TotalSearchResults;
				searchedMods.AddRange(item2.Data);
				FilterSearchMods();
			}
			RefreshScreenState();
		}
	}

	private void FilterSearchMods()
	{
		if (searchedMods.IsNullOrEmpty())
		{
			return;
		}
		filteredSearchedMods.Clear();
		foreach (Mod searchedMod in searchedMods)
		{
			if (ModIOManager.TryGetNewMapsModId(out var newMapsModId) && searchedMod.Id == newMapsModId)
			{
				totalSearchMods = Mathf.Max(0, totalSearchMods - 1);
			}
			else
			{
				filteredSearchedMods.Add(searchedMod);
			}
		}
	}

	private void RefreshScreenState()
	{
		searchMessageText.gameObject.SetActive(value: false);
		customMapsGalleryView.ResetGallery();
		customMapsGalleryView.gameObject.SetActive(value: false);
		displayedMods.Clear();
		if (errorLoadingSearchMods)
		{
			searchMessageText.gameObject.SetActive(value: true);
			searchMessageText.text = errorMessage;
			leftPageButton.SetActive(value: false);
			rightPageButton.SetActive(value: false);
			return;
		}
		if (filteredSearchedMods.IsNullOrEmpty())
		{
			searchMessageText.gameObject.SetActive(value: true);
			searchMessageText.text = noMapsFoundString;
			leftPageButton.SetActive(value: false);
			rightPageButton.SetActive(value: false);
			return;
		}
		int num = 0;
		int num2 = modsPerPage - 1;
		if (!IsOnFirstPage())
		{
			num = currentModPage * modsPerPage;
			num2 = num + modsPerPage - 1;
			leftPageButton.gameObject.SetActive(value: true);
		}
		else
		{
			leftPageButton.gameObject.SetActive(value: false);
		}
		if (!IsOnLastPage())
		{
			rightPageButton.gameObject.SetActive(value: true);
		}
		else
		{
			rightPageButton.gameObject.SetActive(value: false);
		}
		if (filteredSearchedMods.Count <= num2 && totalSearchMods > searchedMods.Count)
		{
			RetrieveMods();
			return;
		}
		for (int i = num; i <= num2 && filteredSearchedMods.Count > i; i++)
		{
			displayedMods.Add(filteredSearchedMods[i]);
		}
		customMapsGalleryView.gameObject.SetActive(value: true);
		if (!customMapsGalleryView.DisplayGallery(displayedMods, useMapName: true, out var error))
		{
			searchMessageText.gameObject.SetActive(value: true);
			searchMessageText.text = error;
			customMapsGalleryView.gameObject.SetActive(value: false);
			leftPageButton.SetActive(value: false);
			rightPageButton.SetActive(value: false);
		}
	}

	private int GetNumPages()
	{
		int num = totalSearchMods % modsPerPage;
		int num2 = totalSearchMods / modsPerPage;
		if (num > 0)
		{
			num2++;
		}
		return num2;
	}

	private bool IsOnFirstPage()
	{
		return currentModPage == 0;
	}

	private bool IsOnLastPage()
	{
		long num = GetNumPages();
		if (currentModPage + 1 == num)
		{
			return true;
		}
		return false;
	}
}
