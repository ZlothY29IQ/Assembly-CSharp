using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GorillaNetworking;
using PlayFab;
using UnityEngine;
using UnityEngine.UI;

public class LegalAgreementBodyText : MonoBehaviour
{
	private enum State
	{
		Ready,
		Loading,
		Error
	}

	[SerializeField]
	private Text textBox;

	[SerializeField]
	private TextAsset textAsset;

	[SerializeField]
	private RectTransform rectTransform;

	private List<Text> textCollection = new List<Text>();

	private string cachedText;

	private State state;

	public float Height => rectTransform.rect.height;

	private void Awake()
	{
		textCollection.Add(textBox);
	}

	public void SetText(string text)
	{
		text = Regex.Unescape(text);
		string[] array = text.Split(new string[3]
		{
			Environment.NewLine,
			"\\r\\n",
			"\n"
		}, StringSplitOptions.None);
		for (int i = 0; i < array.Length; i++)
		{
			Text text2 = null;
			if (i >= textCollection.Count)
			{
				text2 = UnityEngine.Object.Instantiate(textBox, base.transform);
				textCollection.Add(text2);
			}
			else
			{
				text2 = textCollection[i];
			}
			text2.text = array[i];
		}
	}

	public void ClearText()
	{
		foreach (Text item in textCollection)
		{
			item.text = string.Empty;
		}
		state = State.Ready;
	}

	public async Task<bool> UpdateTextFromPlayFabTitleData(string key, string version)
	{
		string text = key + "_" + version;
		state = State.Loading;
		PlayFabTitleDataCache.Instance.GetTitleData(text, OnTitleDataReceived, OnPlayFabError);
		while (state == State.Loading)
		{
			await Task.Yield();
		}
		if (cachedText != null)
		{
			SetText(cachedText.Substring(1, cachedText.Length - 2));
			return true;
		}
		return false;
	}

	private void OnPlayFabError(PlayFabError obj)
	{
		Debug.LogError("ERROR: " + obj.ErrorMessage);
		state = State.Error;
	}

	private void OnTitleDataReceived(string text)
	{
		cachedText = text;
		state = State.Ready;
	}
}
