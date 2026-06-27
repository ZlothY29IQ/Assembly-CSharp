using System.Collections.Generic;
using GorillaGameModes;
using GorillaTag;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/RoomSystemSettings", order = 2)]
internal class RoomSystemSettings : ScriptableObject
{
	[SerializeField]
	private ExpectedUsersDecayTimer expectedUsersTimer;

	[SerializeField]
	private TickSystemTimer resyncNetworkTimeTimer;

	[SerializeField]
	private CallLimiterWithCooldown statusEffectLimiter;

	[SerializeField]
	private CallLimiterWithCooldown soundEffectLimiter;

	[SerializeField]
	private CallLimiterWithCooldown soundEffectOtherLimiter;

	[SerializeField]
	private CallLimiterWithCooldown playerEffectLimiter;

	[SerializeField]
	private CallLimiterWithCooldown lavaSyncLimiter;

	[SerializeField]
	private GameObject playerImpactEffect;

	[SerializeField]
	private List<RoomSystem.PlayerEffectConfig> playerEffects = new List<RoomSystem.PlayerEffectConfig>();

	[SerializeField]
	private int pausedDCTimer;

	[SerializeField]
	private RoomCount publicRoomCountZoneModeMapping;

	[SerializeField]
	private PrivateRoomCount privateRoomCountZoneModeMapping;

	[SerializeField]
	private RoomCount subsPublicRoomCountZoneModeMapping;

	[SerializeField]
	private PrivateRoomCount subsPrivateRoomCountZoneModeMapping;

	public ExpectedUsersDecayTimer ExpectedUsersTimer => expectedUsersTimer;

	public TickSystemTimer ResyncNetworkTimeTimer => resyncNetworkTimeTimer;

	public CallLimiterWithCooldown StatusEffectLimiter => statusEffectLimiter;

	public CallLimiterWithCooldown SoundEffectLimiter => soundEffectLimiter;

	public CallLimiterWithCooldown SoundEffectOtherLimiter => soundEffectOtherLimiter;

	public CallLimiterWithCooldown PlayerEffectLimiter => playerEffectLimiter;

	public CallLimiterWithCooldown LavaSyncLimiter => lavaSyncLimiter;

	public GameObject PlayerImpactEffect => playerImpactEffect;

	public List<RoomSystem.PlayerEffectConfig> PlayerEffects => playerEffects;

	public int PausedDCTimer => pausedDCTimer;

	public int GetRoomCount(bool privateRoom, bool sub)
	{
		if (privateRoom)
		{
			if (!sub)
			{
				return privateRoomCountZoneModeMapping.GetRoomCount();
			}
			return subsPrivateRoomCountZoneModeMapping.GetRoomCount();
		}
		if (!sub)
		{
			return publicRoomCountZoneModeMapping.GetRoomCount();
		}
		return subsPublicRoomCountZoneModeMapping.GetRoomCount();
	}

	public int GetRoomCount(GTZone zone, GameModeType mode, bool privateRoom, bool sub)
	{
		if (privateRoom)
		{
			if (!sub)
			{
				return privateRoomCountZoneModeMapping.GetRoomCount(zone, mode);
			}
			return subsPrivateRoomCountZoneModeMapping.GetRoomCount(zone, mode);
		}
		if (!sub)
		{
			return publicRoomCountZoneModeMapping.GetRoomCount(zone, mode);
		}
		return subsPublicRoomCountZoneModeMapping.GetRoomCount(zone, mode);
	}
}
