using Photon.Pun;

internal class GorillaTagCompetitiveRPCs : RPCNetworkBase
{
	private GameModeSerializer serializer;

	private GorillaTagCompetitiveManager tagCompManager;

	public override void SetClassTarget(IWrappedSerializable target, GorillaWrappedSerializer netHandler)
	{
		tagCompManager = (GorillaTagCompetitiveManager)target;
		serializer = (GameModeSerializer)netHandler;
	}

	[PunRPC]
	public void SendScoresToLateJoinerRPC(int[] playerId, int[] numTags, float[] pointsOnDefense, float[] joinTime, bool[] infected, float[] taggedTime, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "SendScoresToLateJoinerRPC");
		if (info.Sender == null || !info.Sender.IsMasterClient)
		{
			return;
		}
		PhotonMessageInfoWrapped photonMessageInfoWrapped = new PhotonMessageInfoWrapped(info);
		if (photonMessageInfoWrapped.Sender.CheckSingleCallRPC(NetPlayer.SingleCallRPC.RankedSendScoreToLateJoiner))
		{
			return;
		}
		photonMessageInfoWrapped.Sender.ReceivedSingleCallRPC(NetPlayer.SingleCallRPC.RankedSendScoreToLateJoiner);
		if (playerId == null || numTags == null || pointsOnDefense == null || joinTime == null || infected == null || taggedTime == null)
		{
			return;
		}
		int num = playerId.Length;
		if (num > 10)
		{
			return;
		}
		for (int i = 0; i < num; i++)
		{
			for (int j = i + 1; j < num; j++)
			{
				if (playerId[i] == playerId[j])
				{
					return;
				}
			}
		}
		if (numTags.Length != num || pointsOnDefense.Length != num || joinTime.Length != num || infected.Length != num || taggedTime.Length != num)
		{
			return;
		}
		for (int k = 0; k < num; k++)
		{
			if (NetworkSystem.Instance.GetNetPlayerByID(playerId[k]) == null || numTags[k] < 0 || numTags[k] >= 15 || pointsOnDefense[k] < 0f)
			{
				return;
			}
			float num2 = joinTime[k];
			if (float.IsNaN(num2) || float.IsInfinity(num2) || num2 < 0f || num2 > tagCompManager.GetRoundDuration() + 15f)
			{
				return;
			}
			float num3 = taggedTime[k];
			if (float.IsNaN(num3) || float.IsInfinity(num3) || num3 < 0f || num3 > tagCompManager.GetRoundDuration() + 15f)
			{
				return;
			}
		}
		tagCompManager.GetScoring().ReceivedScoresForLateJoiner(playerId, numTags, pointsOnDefense, joinTime, infected, taggedTime);
	}
}
