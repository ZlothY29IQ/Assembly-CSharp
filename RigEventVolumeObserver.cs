using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RigEventVolumeObserver : MonoBehaviour
{
	[Serializable]
	private class RigEventVolumeObserverGameObject
	{
		public enum Comparison
		{
			EQ,
			LT,
			GT,
			LT_EQ,
			GT_EQ,
			NEQ
		}

		[SerializeField]
		private GameObject gameObject;

		[SerializeField]
		public Comparison comparison;

		[SerializeField]
		public int value;

		public bool Check(RigEventVolume rev)
		{
			return comparison switch
			{
				Comparison.EQ => rev.RigCount == value, 
				Comparison.LT => rev.RigCount < value, 
				Comparison.GT => rev.RigCount > value, 
				Comparison.LT_EQ => rev.RigCount <= value, 
				Comparison.GT_EQ => rev.RigCount >= value, 
				Comparison.NEQ => rev.RigCount != value, 
				_ => false, 
			};
		}

		public void ApplyActiveState(RigEventVolume rev)
		{
			gameObject.SetActive(Check(rev));
		}
	}

	[SerializeField]
	private RigEventVolume observed;

	[SerializeField]
	private RigEventVolumeObserverGameObject[] gameObjects;

	[SerializeField]
	private TMP_Text[] tMP_Texts;

	private List<string> formats = new List<string>();

	private void Awake()
	{
		for (int i = 0; i < tMP_Texts.Length; i++)
		{
			formats.Add(tMP_Texts[i].text);
		}
	}

	private void OnEnable()
	{
		Observed_OnCountChanged();
		observed.OnCountChanged += Observed_OnCountChanged;
	}

	private void OnDisable()
	{
		observed.OnCountChanged -= Observed_OnCountChanged;
	}

	private void Observed_OnCountChanged()
	{
		for (int i = 0; i < gameObjects.Length; i++)
		{
			gameObjects[i].ApplyActiveState(observed);
		}
		for (int j = 0; j < tMP_Texts.Length; j++)
		{
			tMP_Texts[j].text = Format(formats[j]);
		}
	}

	private string Format(string s)
	{
		return s.Replace("\\c", observed.RigCount.ToString());
	}
}
