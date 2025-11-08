using System.Collections.Generic;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

public class CrittersLoudNoise : CrittersActor
{
	public float soundVolume;

	public float volumeFearAttractionMultiplier;

	public float soundDuration;

	public double timeSoundEnabled;

	public bool soundEnabled;

	private bool wasSoundEnabled;

	public bool disableWhenSoundDisabled;

	public override void OnEnable()
	{
		base.OnEnable();
		SetTimeEnabled();
	}

	public void SpawnData(float _soundVolume, float _soundDuration, float _soundMultiplier, bool _soundEnabled)
	{
		soundVolume = _soundVolume;
		volumeFearAttractionMultiplier = _soundMultiplier;
		soundDuration = _soundDuration;
		soundEnabled = _soundEnabled;
		Initialize();
	}

	public override bool ProcessLocal()
	{
		bool flag = base.ProcessLocal();
		if (!isEnabled)
		{
			return flag;
		}
		wasEnabled = base.gameObject.activeSelf;
		wasSoundEnabled = soundEnabled;
		if (PhotonNetwork.InRoom)
		{
			if (PhotonNetwork.Time > timeSoundEnabled + (double)soundDuration || timeSoundEnabled > PhotonNetwork.Time)
			{
				soundEnabled = false;
			}
		}
		else if ((double)Time.time > timeSoundEnabled + (double)soundDuration || timeSoundEnabled > (double)Time.time)
		{
			soundEnabled = false;
		}
		if (disableWhenSoundDisabled && !soundEnabled)
		{
			isEnabled = false;
			if (base.gameObject.activeSelf != isEnabled)
			{
				base.gameObject.SetActive(isEnabled);
			}
		}
		updatedSinceLastFrame = flag || wasSoundEnabled != soundEnabled || wasEnabled != isEnabled;
		return updatedSinceLastFrame;
	}

	public override void ProcessRemote()
	{
		if (!wasEnabled && isEnabled)
		{
			SetTimeEnabled();
		}
	}

	public void SetTimeEnabled()
	{
		if (PhotonNetwork.InRoom)
		{
			timeSoundEnabled = PhotonNetwork.Time;
		}
		else
		{
			timeSoundEnabled = Time.time;
		}
	}

	public override void CalculateFear(CrittersPawn critter, float multiplier)
	{
		if (soundEnabled)
		{
			if (soundDuration == 0f)
			{
				critter.IncreaseFear(soundVolume * volumeFearAttractionMultiplier * multiplier, this);
			}
			else if ((PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time)) - timeSoundEnabled < (double)soundDuration)
			{
				critter.IncreaseFear(soundVolume * volumeFearAttractionMultiplier * Time.deltaTime * multiplier, this);
			}
		}
	}

	public override void CalculateAttraction(CrittersPawn critter, float multiplier)
	{
		if (soundEnabled)
		{
			if (soundDuration == 0f)
			{
				critter.IncreaseAttraction(soundVolume * volumeFearAttractionMultiplier * multiplier, this);
			}
			else if ((PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time)) - timeSoundEnabled < (double)soundDuration)
			{
				critter.IncreaseAttraction(soundVolume * volumeFearAttractionMultiplier * Time.deltaTime * multiplier, this);
			}
		}
	}

	public override bool UpdateSpecificActor(PhotonStream stream)
	{
		if (!(base.UpdateSpecificActor(stream) & CrittersManager.ValidateDataType<float>(stream.ReceiveNext(), out var dataAsType) & CrittersManager.ValidateDataType<float>(stream.ReceiveNext(), out var dataAsType2) & CrittersManager.ValidateDataType<bool>(stream.ReceiveNext(), out var dataAsType3) & CrittersManager.ValidateDataType<float>(stream.ReceiveNext(), out var dataAsType4)))
		{
			return false;
		}
		soundVolume = dataAsType.GetFinite();
		soundDuration = dataAsType2.GetFinite();
		soundEnabled = dataAsType3;
		volumeFearAttractionMultiplier = dataAsType4.GetFinite();
		return true;
	}

	public override void SendDataByCrittersActorType(PhotonStream stream)
	{
		base.SendDataByCrittersActorType(stream);
		stream.SendNext(soundVolume);
		stream.SendNext(soundDuration);
		stream.SendNext(soundEnabled);
		stream.SendNext(volumeFearAttractionMultiplier);
	}

	public override int AddActorDataToList(ref List<object> objList)
	{
		base.AddActorDataToList(ref objList);
		objList.Add(soundVolume);
		objList.Add(soundDuration);
		objList.Add(soundEnabled);
		objList.Add(volumeFearAttractionMultiplier);
		return TotalActorDataLength();
	}

	public override int TotalActorDataLength()
	{
		return BaseActorDataLength() + 4;
	}

	public override int UpdateFromRPC(object[] data, int startingIndex)
	{
		startingIndex += base.UpdateFromRPC(data, startingIndex);
		if (!CrittersManager.ValidateDataType<float>(data[startingIndex], out var dataAsType))
		{
			return TotalActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<float>(data[startingIndex + 1], out var dataAsType2))
		{
			return TotalActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<bool>(data[startingIndex + 2], out var dataAsType3))
		{
			return TotalActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<float>(data[startingIndex + 3], out var dataAsType4))
		{
			return TotalActorDataLength();
		}
		soundVolume = dataAsType.GetFinite();
		soundDuration = dataAsType2.GetFinite();
		soundEnabled = dataAsType3;
		volumeFearAttractionMultiplier = dataAsType4.GetFinite();
		return TotalActorDataLength();
	}

	public void PlayHandTapLocal(bool isLeft)
	{
		timeSoundEnabled = (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time));
		soundEnabled = true;
	}

	public void PlayHandTapRemote(double serverTime, bool isLeft)
	{
		timeSoundEnabled = serverTime;
		soundEnabled = true;
	}

	public void PlayVoiceSpeechLocal(double serverTime, float duration, float volume)
	{
		soundDuration = duration;
		timeSoundEnabled = serverTime;
		soundVolume = volume;
		soundEnabled = true;
	}
}
