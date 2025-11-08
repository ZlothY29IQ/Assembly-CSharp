using System.Globalization;
using Backtrace.Unity;
using Backtrace.Unity.Model;
using GorillaNetworking;
using PlayFab;
using Unity.Mathematics;
using UnityEngine;

public class BacktraceManager : MonoBehaviour
{
	public double backtraceSampleRate = 0.01;

	public virtual void Awake()
	{
		GetComponent<BacktraceClient>().BeforeSend = (BacktraceData data) => (!(new Unity.Mathematics.Random((uint)(Time.realtimeSinceStartupAsDouble * 1000.0)).NextDouble() <= backtraceSampleRate)) ? null : data;
	}

	private void Start()
	{
		PlayFabTitleDataCache.Instance.GetTitleData("BacktraceSampleRate", delegate(string data)
		{
			if (data != null)
			{
				double.TryParse(data.Trim('"'), NumberStyles.Any, CultureInfo.InvariantCulture, out backtraceSampleRate);
				Debug.Log($"Set backtrace sample rate to: {backtraceSampleRate}");
			}
		}, delegate(PlayFabError e)
		{
			Debug.LogError($"Error getting Backtrace sample rate: {e}");
		});
	}
}
