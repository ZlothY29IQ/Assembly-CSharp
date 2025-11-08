using System.Collections.Generic;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

public class CrittersFood : CrittersActor
{
	public float maxFood;

	public float currentFood;

	private int lastFood;

	public float startingSize;

	public float currentSize;

	public Transform food;

	public bool disableWhenEmpty = true;

	public override void Initialize()
	{
		base.Initialize();
		currentFood = maxFood;
	}

	public void SpawnData(float _maxFood, float _currentFood, float _startingSize)
	{
		maxFood = _maxFood;
		currentFood = _currentFood;
		startingSize = _startingSize;
		currentSize = currentFood / maxFood * startingSize;
		food.localScale = new Vector3(currentSize, currentSize, currentSize);
	}

	public override bool ProcessLocal()
	{
		bool flag = base.ProcessLocal();
		if (!isEnabled)
		{
			return flag;
		}
		wasEnabled = base.gameObject.activeSelf;
		ProcessFood();
		bool flag2 = Mathf.FloorToInt(currentFood) != lastFood;
		lastFood = Mathf.FloorToInt(currentFood);
		if (currentFood == 0f && disableWhenEmpty)
		{
			isEnabled = false;
		}
		if (base.gameObject.activeSelf != isEnabled)
		{
			base.gameObject.SetActive(isEnabled);
		}
		updatedSinceLastFrame = flag || flag2 || wasEnabled != isEnabled;
		return updatedSinceLastFrame;
	}

	public override void ProcessRemote()
	{
		base.ProcessRemote();
		if (isEnabled)
		{
			ProcessFood();
		}
	}

	public void ProcessFood()
	{
		if (currentSize != currentFood / maxFood * startingSize)
		{
			currentSize = currentFood / maxFood * startingSize;
			food.localScale = new Vector3(currentSize, currentSize, currentSize);
			if (storeCollider != null)
			{
				storeCollider.radius = currentSize / 2f;
			}
		}
	}

	public void Feed(float amountEaten)
	{
		currentFood = Mathf.Max(0f, currentFood - amountEaten);
	}

	public override bool UpdateSpecificActor(PhotonStream stream)
	{
		if (!(base.UpdateSpecificActor(stream) & CrittersManager.ValidateDataType<int>(stream.ReceiveNext(), out var dataAsType) & CrittersManager.ValidateDataType<float>(stream.ReceiveNext(), out var dataAsType2) & CrittersManager.ValidateDataType<float>(stream.ReceiveNext(), out var dataAsType3)))
		{
			return false;
		}
		currentFood = dataAsType;
		maxFood = dataAsType2.GetFinite();
		startingSize = dataAsType3.GetFinite();
		return true;
	}

	public override void SendDataByCrittersActorType(PhotonStream stream)
	{
		base.SendDataByCrittersActorType(stream);
		stream.SendNext(Mathf.FloorToInt(currentFood));
		stream.SendNext(maxFood);
		stream.SendNext(startingSize);
	}

	public override int AddActorDataToList(ref List<object> objList)
	{
		base.AddActorDataToList(ref objList);
		objList.Add(Mathf.FloorToInt(currentFood));
		objList.Add(maxFood);
		objList.Add(startingSize);
		return TotalActorDataLength();
	}

	public override int TotalActorDataLength()
	{
		return BaseActorDataLength() + 3;
	}

	public override int UpdateFromRPC(object[] data, int startingIndex)
	{
		startingIndex += base.UpdateFromRPC(data, startingIndex);
		if (!CrittersManager.ValidateDataType<int>(data[startingIndex], out var dataAsType))
		{
			return TotalActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<float>(data[startingIndex + 1], out var dataAsType2))
		{
			return TotalActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<float>(data[startingIndex + 2], out var dataAsType3))
		{
			return TotalActorDataLength();
		}
		currentFood = dataAsType;
		maxFood = dataAsType2.GetFinite();
		startingSize = dataAsType3.GetFinite();
		return TotalActorDataLength();
	}
}
