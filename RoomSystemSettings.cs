using System.Collections.Generic;
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
	private GameObject playerImpactEffect;

	[SerializeField]
	private List<RoomSystem.PlayerEffectConfig> playerEffects = new List<RoomSystem.PlayerEffectConfig>();

	[SerializeField]
	private int pausedDCTimer;

	public ExpectedUsersDecayTimer ExpectedUsersTimer => expectedUsersTimer;

	public TickSystemTimer ResyncNetworkTimeTimer => resyncNetworkTimeTimer;

	public CallLimiterWithCooldown StatusEffectLimiter => statusEffectLimiter;

	public CallLimiterWithCooldown SoundEffectLimiter => soundEffectLimiter;

	public CallLimiterWithCooldown SoundEffectOtherLimiter => soundEffectOtherLimiter;

	public CallLimiterWithCooldown PlayerEffectLimiter => playerEffectLimiter;

	public GameObject PlayerImpactEffect => playerImpactEffect;

	public List<RoomSystem.PlayerEffectConfig> PlayerEffects => playerEffects;

	public int PausedDCTimer => pausedDCTimer;
}
