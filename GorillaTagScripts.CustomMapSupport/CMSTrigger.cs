using System;
using GT_CustomMapSupportRuntime;
using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts.CustomMapSupport;

public class CMSTrigger : MonoBehaviour
{
	public const byte INVALID_TRIGGER_ID = byte.MaxValue;

	public const double MAX_PHOTON_SERVER_TIME = 4294967.295;

	public const float MINIMUM_VALIDATION_DISTANCE = 2f;

	public bool syncedToAllPlayers;

	public float validationDistanceSquared;

	public TriggerSource triggeredBy = TriggerSource.HeadOrBody;

	public double onEnableTriggerDelay;

	public double generalRetriggerDelay;

	public bool retriggerAfterDuration;

	public double retriggerStayDuration = 2.0;

	public byte numAllowedTriggers;

	private byte numTimesTriggered;

	private double lastTriggerTime = -1.0;

	private double enabledTime = -1.0;

	public byte id = byte.MaxValue;

	public void OnEnable()
	{
		if (onEnableTriggerDelay > 0.0)
		{
			enabledTime = Time.time;
		}
	}

	public byte GetID()
	{
		return id;
	}

	public virtual void CopyTriggerSettings(TriggerSettings settings)
	{
		id = settings.triggerId;
		triggeredBy = settings.triggeredBy;
		float num = Math.Max(settings.validationDistance, 2f);
		validationDistanceSquared = num * num;
		if (triggeredBy == TriggerSource.None)
		{
			if (settings.triggeredByHead && !settings.triggeredByBody)
			{
				triggeredBy = TriggerSource.Head;
			}
			else if (settings.triggeredByBody && !settings.triggeredByHead)
			{
				triggeredBy = TriggerSource.Body;
			}
			else if (settings.triggeredByHands && !settings.triggeredByHead && !settings.triggeredByBody)
			{
				triggeredBy = TriggerSource.Hands;
			}
			else
			{
				triggeredBy = TriggerSource.HeadOrBody;
			}
		}
		switch (triggeredBy)
		{
		case TriggerSource.Head:
		case TriggerSource.Body:
		case TriggerSource.HeadOrBody:
			base.gameObject.layer = UnityLayer.GorillaTrigger.ToLayerIndex();
			break;
		case TriggerSource.Hands:
			base.gameObject.layer = UnityLayer.GorillaInteractable.ToLayerIndex();
			break;
		}
		onEnableTriggerDelay = settings.onEnableTriggerDelay;
		generalRetriggerDelay = settings.generalRetriggerDelay;
		retriggerAfterDuration = settings.retriggerAfterDuration;
		if (Math.Abs(settings.retriggerDelay - 2f) > 0.001f && Math.Abs(settings.retriggerStayDuration - 2.0) < 0.001)
		{
			settings.retriggerStayDuration = settings.retriggerDelay;
		}
		retriggerStayDuration = Math.Max(generalRetriggerDelay, settings.retriggerStayDuration);
		if (retriggerStayDuration <= 0.0)
		{
			retriggerAfterDuration = false;
		}
		numAllowedTriggers = settings.numAllowedTriggers;
		syncedToAllPlayers = settings.syncedToAllPlayers_private;
		if (syncedToAllPlayers)
		{
			CMSSerializer.RegisterTrigger(base.gameObject.scene.name, this);
		}
		Collider[] components = base.gameObject.GetComponents<Collider>();
		for (int i = 0; i < components.Length; i++)
		{
			components[i].isTrigger = true;
		}
	}

	public void OnTriggerEnter(Collider triggeringCollider)
	{
		if (ValidateCollider(triggeringCollider) && CanTrigger())
		{
			OnTriggerActivation(triggeringCollider);
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (retriggerAfterDuration && ValidateCollider(other) && CanTrigger())
		{
			double num = Time.time;
			if (NetworkSystem.Instance.InRoom)
			{
				num = PhotonNetwork.Time;
			}
			if (lastTriggerTime + retriggerStayDuration <= num)
			{
				OnTriggerActivation(other);
			}
		}
	}

	private bool ValidateCollider(Collider other)
	{
		GameObject gameObject = other.gameObject;
		bool num = gameObject == GorillaTagger.Instance.headCollider.gameObject && (triggeredBy == TriggerSource.Head || triggeredBy == TriggerSource.HeadOrBody);
		bool flag = false;
		flag = ((!GorillaTagger.Instance.bodyCollider.enabled) ? (gameObject == VRRig.LocalRig.gameObject && (triggeredBy == TriggerSource.Body || triggeredBy == TriggerSource.HeadOrBody)) : (gameObject == GorillaTagger.Instance.bodyCollider.gameObject && (triggeredBy == TriggerSource.Body || triggeredBy == TriggerSource.HeadOrBody)));
		bool flag2 = (gameObject == GorillaTagger.Instance.leftHandTriggerCollider.gameObject || gameObject == GorillaTagger.Instance.rightHandTriggerCollider.gameObject) && triggeredBy == TriggerSource.Hands;
		return num || flag || flag2;
	}

	private void OnTriggerActivation(Collider activatingCollider)
	{
		if (syncedToAllPlayers)
		{
			CMSSerializer.RequestTrigger(id);
		}
		else
		{
			Trigger(-1.0, originatedLocally: true);
		}
	}

	public bool CanTrigger()
	{
		if (numAllowedTriggers > 0 && numTimesTriggered >= numAllowedTriggers)
		{
			return false;
		}
		if (onEnableTriggerDelay > 0.0 && (double)Time.time - enabledTime < onEnableTriggerDelay)
		{
			return false;
		}
		if (generalRetriggerDelay <= 0.0)
		{
			return true;
		}
		if (NetworkSystem.Instance.InRoom)
		{
			if (PhotonNetwork.Time - lastTriggerTime < -1.0)
			{
				lastTriggerTime = 0.0 - (4294967.295 - lastTriggerTime);
			}
			if (lastTriggerTime + generalRetriggerDelay <= PhotonNetwork.Time)
			{
				return true;
			}
		}
		else if (lastTriggerTime + generalRetriggerDelay <= (double)Time.time)
		{
			return true;
		}
		return false;
	}

	public virtual void Trigger(double triggerTime = -1.0, bool originatedLocally = false, bool ignoreTriggerCount = false)
	{
		if (!ignoreTriggerCount)
		{
			if (numAllowedTriggers > 0 && numTimesTriggered >= numAllowedTriggers)
			{
				return;
			}
			numTimesTriggered++;
		}
		if (NetworkSystem.Instance.InRoom)
		{
			if (triggerTime < 0.0)
			{
				triggerTime = PhotonNetwork.Time;
			}
		}
		else if (originatedLocally)
		{
			triggerTime = Time.time;
		}
		lastTriggerTime = triggerTime;
		if (numAllowedTriggers > 0 && numTimesTriggered >= numAllowedTriggers)
		{
			Collider[] components = base.gameObject.GetComponents<Collider>();
			for (int i = 0; i < components.Length; i++)
			{
				components[i].enabled = false;
			}
		}
	}

	public void ResetTrigger(bool onlyResetTriggerCount = false)
	{
		if (!onlyResetTriggerCount)
		{
			lastTriggerTime = -1.0;
		}
		numTimesTriggered = 0;
		Collider[] components = base.gameObject.GetComponents<Collider>();
		for (int i = 0; i < components.Length; i++)
		{
			components[i].enabled = true;
		}
		CMSSerializer.ResetTrigger(id);
	}

	public void SetTriggerCount(byte value)
	{
		numTimesTriggered = Math.Min(value, numAllowedTriggers);
		if (numTimesTriggered >= numAllowedTriggers)
		{
			Collider[] components = base.gameObject.GetComponents<Collider>();
			for (int i = 0; i < components.Length; i++)
			{
				components[i].enabled = false;
			}
		}
	}

	public void SetLastTriggerTime(double value)
	{
		lastTriggerTime = value;
	}
}
