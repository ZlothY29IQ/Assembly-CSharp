using GorillaExtensions;
using Photon.Pun;

public struct CritterAppearance(string hatName, float size = 1f)
{
	public float size = size;

	public string hatName = hatName;

	public object[] WriteToRPCData()
	{
		object[] array = new object[2] { hatName, size };
		if (hatName == null)
		{
			array[0] = string.Empty;
		}
		if (size != 0f)
		{
			array[1] = size;
		}
		return array;
	}

	public static int DataLength()
	{
		return 2;
	}

	public static bool ValidateData(object[] data)
	{
		if (data == null || data.Length != DataLength())
		{
			return false;
		}
		if (!CrittersManager.ValidateDataType<float>(data[1], out var dataAsType))
		{
			return false;
		}
		if (dataAsType < 0f || float.IsNaN(dataAsType) || float.IsInfinity(dataAsType))
		{
			return false;
		}
		return true;
	}

	public static CritterAppearance ReadFromRPCData(object[] data)
	{
		if (!CrittersManager.ValidateDataType<string>(data[0], out var _))
		{
			return new CritterAppearance(string.Empty);
		}
		if (!CrittersManager.ValidateDataType<float>(data[1], out var dataAsType2))
		{
			return new CritterAppearance(string.Empty);
		}
		return new CritterAppearance((string)data[0], dataAsType2.GetFinite());
	}

	public static CritterAppearance ReadFromPhotonStream(PhotonStream data)
	{
		string obj = (string)data.ReceiveNext();
		float num = (float)data.ReceiveNext();
		return new CritterAppearance(obj, num);
	}

	public override string ToString()
	{
		return $"Size: {size} Hat: {hatName}";
	}
}
