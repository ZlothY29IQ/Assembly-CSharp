using System.Collections.Generic;
using GorillaExtensions;
using GT_CustomMapSupportRuntime;
using UnityEngine;

namespace GorillaTagScripts.CustomMapSupport;

public class CMSObjectActivationTrigger : CMSTrigger
{
	public List<GameObject> objectsToActivate = new List<GameObject>();

	public List<GameObject> objectsToDeactivate = new List<GameObject>();

	public List<GameObject> triggersToReset = new List<GameObject>();

	public bool onlyResetTriggerCount;

	public override void CopyTriggerSettings(TriggerSettings settings)
	{
		if (settings.GetType() == typeof(ObjectActivationTriggerSettings))
		{
			ObjectActivationTriggerSettings objectActivationTriggerSettings = (ObjectActivationTriggerSettings)settings;
			objectsToActivate = objectActivationTriggerSettings.objectsToActivate;
			objectsToDeactivate = objectActivationTriggerSettings.objectsToDeactivate;
			triggersToReset = objectActivationTriggerSettings.triggersToReset;
			onlyResetTriggerCount = objectActivationTriggerSettings.onlyResetTriggerCount;
		}
		for (int num = objectsToActivate.Count - 1; num >= 0; num--)
		{
			if (objectsToActivate[num] == null)
			{
				objectsToActivate.RemoveAt(num);
			}
		}
		for (int num2 = objectsToDeactivate.Count - 1; num2 >= 0; num2--)
		{
			if (objectsToDeactivate[num2] == null)
			{
				objectsToDeactivate.RemoveAt(num2);
			}
		}
		for (int num3 = triggersToReset.Count - 1; num3 >= 0; num3--)
		{
			if (triggersToReset[num3] == null)
			{
				triggersToReset.RemoveAt(num3);
			}
		}
		base.CopyTriggerSettings(settings);
	}

	public override void Trigger(double triggerTime = -1.0, bool originatedLocally = false, bool ignoreTriggerCount = false)
	{
		base.Trigger(triggerTime, originatedLocally, ignoreTriggerCount);
		foreach (GameObject item in objectsToDeactivate)
		{
			if (item.IsNotNull())
			{
				item.SetActive(value: false);
			}
		}
		foreach (GameObject item2 in objectsToActivate)
		{
			if (item2.IsNotNull())
			{
				item2.SetActive(value: true);
			}
		}
		foreach (GameObject item3 in triggersToReset)
		{
			if (item3.IsNull())
			{
				continue;
			}
			CMSTrigger[] components = item3.GetComponents<CMSTrigger>();
			if (components != null)
			{
				CMSTrigger[] array = components;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].ResetTrigger(onlyResetTriggerCount);
				}
			}
		}
	}
}
