using System;
using GorillaExtensions;
using UnityEngine;

public class MenagerieDepositBox : MonoBehaviour
{
	public Action<MenagerieCritter> OnCritterInserted;

	public void OnTriggerEnter(Collider other)
	{
		MenagerieCritter component = other.transform.parent.parent.GetComponent<MenagerieCritter>();
		if (component.IsNotNull())
		{
			component.OnReleased = (Action<MenagerieCritter>)Delegate.Combine(component.OnReleased, OnCritterInserted);
		}
	}

	public void OnTriggerExit(Collider other)
	{
		MenagerieCritter component = other.transform.parent.GetComponent<MenagerieCritter>();
		if (component.IsNotNull())
		{
			component.OnReleased = (Action<MenagerieCritter>)Delegate.Remove(component.OnReleased, OnCritterInserted);
		}
	}
}
