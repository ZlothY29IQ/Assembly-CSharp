using System.Collections.Generic;
using GorillaExtensions;
using GT_CustomMapSupportRuntime;
using UnityEngine;

namespace GorillaTagScripts.CustomMapSupport;

public class CMSPlayAnimationTrigger : CMSTrigger
{
	public List<GameObject> animatedObjects = new List<GameObject>();

	public string animationName = "";

	public override void CopyTriggerSettings(TriggerSettings settings)
	{
		if (settings.GetType() == typeof(PlayAnimationTriggerSettings))
		{
			PlayAnimationTriggerSettings playAnimationTriggerSettings = (PlayAnimationTriggerSettings)settings;
			animatedObjects = playAnimationTriggerSettings.animatedObjects;
			animationName = playAnimationTriggerSettings.animationName;
		}
		for (int num = animatedObjects.Count - 1; num >= 0; num--)
		{
			if (animatedObjects[num].IsNull())
			{
				animatedObjects.RemoveAt(num);
			}
		}
		base.CopyTriggerSettings(settings);
	}

	public override void Trigger(double triggerTime = -1.0, bool originatedLocally = false, bool ignoreTriggerCount = false)
	{
		base.Trigger(triggerTime, originatedLocally, ignoreTriggerCount);
		foreach (GameObject animatedObject in animatedObjects)
		{
			Animator component = animatedObject.GetComponent<Animator>();
			if (component.IsNotNull())
			{
				component.Play(animationName);
			}
		}
	}
}
