using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.ScavengerHunt;

public class ScavengerListener : MonoBehaviour
{
	[SerializeField]
	private string huntName;

	[SerializeField]
	private UnityEvent OnCompleted;

	[SerializeField]
	private UnityEvent OnCompletedRealtime;

	[SerializeField]
	private UnityEvent<string> OnCollected;

	[SerializeField]
	private UnityEvent<string> OnCollectedRealtime;

	[SerializeField]
	private TMP_Text[] displayHuntStatus;

	[SerializeField]
	private string displayHuntStatusHeading;

	private ScavengerManager.Hunt hunt;

	private async void OnEnable()
	{
		ScavengerManager.OnHuntCompleted = (Action<string, bool>)Delegate.Combine(ScavengerManager.OnHuntCompleted, new Action<string, bool>(OnHuntCompleted));
		ScavengerManager.OnTargetCollected = (Action<string, string, bool>)Delegate.Combine(ScavengerManager.OnTargetCollected, new Action<string, string, bool>(OnTargetCollected));
		while (ScavengerManager.Instance == null)
		{
			await Task.Delay(100);
		}
		hunt = ScavengerManager.Instance.GetHunt(huntName);
		if (hunt == null)
		{
			Debug.LogError("ScavengerManager on " + base.name + " couldn't find a hunt named " + huntName + ". Check your spelling!");
			ScavengerManager.OnHuntCompleted = (Action<string, bool>)Delegate.Remove(ScavengerManager.OnHuntCompleted, new Action<string, bool>(OnHuntCompleted));
			ScavengerManager.OnTargetCollected = (Action<string, string, bool>)Delegate.Remove(ScavengerManager.OnTargetCollected, new Action<string, string, bool>(OnTargetCollected));
		}
		else if (hunt.IsCompleted)
		{
			OnHuntCompleted(huntName, firstCompletetion: false);
		}
		else
		{
			setStatusText();
		}
	}

	private void setStatusText()
	{
		if (displayHuntStatus.Length == 0)
		{
			return;
		}
		string text = displayHuntStatusHeading;
		for (int i = 0; i < hunt.Targets.Count; i++)
		{
			string text2 = hunt.Targets[i].DisplayName;
			if (text2.IsNullOrEmpty())
			{
				text2 = hunt.Targets[i].TargetName;
			}
			text = text + "\n[" + (hunt.IsCollected(hunt.Targets[i]) ? "X" : " ") + "] " + text2;
		}
		for (int j = 0; j < displayHuntStatus.Length; j++)
		{
			displayHuntStatus[j].text = text;
		}
	}

	private void OnTargetCollected(string huntName, string itemName, bool firstCompletetion)
	{
		if (!(huntName != this.huntName))
		{
			OnCollected?.Invoke(itemName);
			if (firstCompletetion)
			{
				OnCollectedRealtime.Invoke(itemName);
			}
			setStatusText();
		}
	}

	private void OnHuntCompleted(string huntName, bool firstCompletetion)
	{
		if (!(huntName != this.huntName))
		{
			OnCompleted?.Invoke();
			if (firstCompletetion)
			{
				OnCompletedRealtime.Invoke();
			}
			ScavengerManager.OnHuntCompleted = (Action<string, bool>)Delegate.Remove(ScavengerManager.OnHuntCompleted, new Action<string, bool>(OnHuntCompleted));
			ScavengerManager.OnTargetCollected = (Action<string, string, bool>)Delegate.Remove(ScavengerManager.OnTargetCollected, new Action<string, string, bool>(OnTargetCollected));
			setStatusText();
		}
	}

	private void OnDisable()
	{
		ScavengerManager.OnHuntCompleted = (Action<string, bool>)Delegate.Remove(ScavengerManager.OnHuntCompleted, new Action<string, bool>(OnHuntCompleted));
		ScavengerManager.OnTargetCollected = (Action<string, string, bool>)Delegate.Remove(ScavengerManager.OnTargetCollected, new Action<string, string, bool>(OnTargetCollected));
	}

	private void OnDestroy()
	{
		ScavengerManager.OnHuntCompleted = (Action<string, bool>)Delegate.Remove(ScavengerManager.OnHuntCompleted, new Action<string, bool>(OnHuntCompleted));
		ScavengerManager.OnTargetCollected = (Action<string, string, bool>)Delegate.Remove(ScavengerManager.OnTargetCollected, new Action<string, string, bool>(OnTargetCollected));
	}
}
