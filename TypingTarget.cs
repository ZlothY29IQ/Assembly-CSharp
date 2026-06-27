using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TypingTarget : GenericObservable
{
	private int index = -1;

	private int pindex = -1;

	private TMP_Text tmp;

	private string value;

	[SerializeField]
	private int charLength = 500;

	[SerializeField]
	private Transform cursor;

	[SerializeField]
	private string backupId;

	public string Text => value;

	private void Awake()
	{
		tmp = GetComponent<TMP_Text>();
		tmp.text = " ";
		cursor.gameObject.SetActive(value: false);
	}

	public void Append(string s)
	{
		if (value.Length != charLength)
		{
			index++;
			List<char> list = new List<char>(value.ToCharArray());
			list.Insert(index, s[0]);
			value = new string(list.ToArray());
		}
	}

	public void Delete()
	{
		if (value.Length != 0)
		{
			List<char> list = new List<char>(value.ToCharArray());
			list.RemoveAt(index);
			value = new string(list.ToArray());
			index--;
		}
	}

	public void Clear()
	{
		index = -1;
		value = string.Empty;
		cursor.gameObject.SetActive(value: false);
	}

	public void MoveCursor(int i)
	{
		index = Mathf.Clamp(index + i, -1, value.Length - 1);
	}

	protected override void OnLostObservable()
	{
		base.OnLostObservable();
		if (!backupId.IsNullOrEmpty())
		{
			PlayerPrefs.SetString("TypingTarget" + backupId, value);
			PlayerPrefs.Save();
			Clear();
		}
	}

	protected override void OnBecameObservable()
	{
		base.OnBecameObservable();
		if (!backupId.IsNullOrEmpty())
		{
			value = PlayerPrefs.GetString("TypingTarget" + backupId, string.Empty);
			index = value.Length - 1;
		}
	}

	protected override void ObservableSliceUpdate()
	{
		if (tmp.text != value + " ")
		{
			tmp.text = value + " ";
		}
		if (index != pindex && index >= -1 && index + 1 < tmp.textInfo.characterCount)
		{
			cursor.gameObject.SetActive(value: true);
			cursor.localPosition = tmp.textInfo.characterInfo[index + 1].bottomLeft;
			pindex = index;
		}
	}
}
