internal class BundleList
{
	private int activeBundleIdx;

	public BundleData[] data;

	public void FromJson(string jsonString)
	{
		data = JSonHelper.FromJson<BundleData>(jsonString);
		if (data.Length == 0)
		{
			return;
		}
		activeBundleIdx = 0;
		int majorVersion = data[0].majorVersion;
		int minorVersion = data[0].minorVersion;
		int minorVersion2 = data[0].minorVersion2;
		int gameMajorVersion = NetworkSystemConfig.GameMajorVersion;
		int gameMinorVersion = NetworkSystemConfig.GameMinorVersion;
		int gameMinorVersion2 = NetworkSystemConfig.GameMinorVersion2;
		for (int i = 1; i < data.Length; i++)
		{
			data[i].isActive = false;
			int num = gameMajorVersion * 1000000 + gameMinorVersion * 1000 + gameMinorVersion2;
			int num2 = data[i].majorVersion * 1000000 + data[i].minorVersion * 1000 + data[i].minorVersion2;
			if (num >= num2 && data[i].majorVersion >= majorVersion && data[i].minorVersion >= minorVersion && data[i].minorVersion2 >= minorVersion2)
			{
				activeBundleIdx = i;
				majorVersion = data[i].majorVersion;
				minorVersion = data[i].minorVersion;
				minorVersion2 = data[i].minorVersion2;
				break;
			}
		}
		data[activeBundleIdx].isActive = true;
	}

	public bool HasSku(string skuName, out int idx)
	{
		if (data == null)
		{
			idx = -1;
			return false;
		}
		for (int i = 0; i < data.Length; i++)
		{
			if (data[i].skuName == skuName)
			{
				idx = i;
				return true;
			}
		}
		idx = -1;
		return false;
	}

	public BundleData ActiveBundle()
	{
		return data[activeBundleIdx];
	}
}
