using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace GorillaTag;

[Serializable]
internal class ExpectedUsersDecayTimer : TickSystemTimerAbstract
{
	public float decayTime = 15f;

	private Dictionary<string, float> expectedUsers = new Dictionary<string, float>(20);

	public override void OnTimedEvent()
	{
		if (!NetworkSystem.Instance.InRoom || !NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		int num = 0;
		if (PhotonNetwork.CurrentRoom.ExpectedUsers == null || PhotonNetwork.CurrentRoom.ExpectedUsers.Length == 0)
		{
			return;
		}
		string[] array = PhotonNetwork.CurrentRoom.ExpectedUsers;
		foreach (string key in array)
		{
			if (expectedUsers.TryGetValue(key, out var value))
			{
				if (value + decayTime < Time.time)
				{
					num++;
				}
			}
			else
			{
				expectedUsers.Add(key, Time.time);
			}
		}
		if (num >= PhotonNetwork.CurrentRoom.ExpectedUsers.Length && num != 0)
		{
			PhotonNetwork.CurrentRoom.ClearExpectedUsers();
			expectedUsers.Clear();
		}
	}

	public override void Stop()
	{
		base.Stop();
		expectedUsers.Clear();
	}
}
