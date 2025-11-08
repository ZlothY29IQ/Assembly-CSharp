using System.Collections;
using GorillaNetworking;
using TMPro;
using UnityEngine;

public class GRBadge : MonoBehaviour, IGameEntityComponent
{
	public enum BadgeState
	{
		AtDispenser,
		WithPlayer
	}

	private const float RESTORE_BADGE_TO_DOCK_WINDOW = 60f;

	[SerializeField]
	private GameEntity gameEntity;

	[SerializeField]
	public TMP_Text playerName;

	[SerializeField]
	public TMP_Text playerTitle;

	[SerializeField]
	public TMP_Text playerLevel;

	[SerializeField]
	private MeshRenderer badgeMesh;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private float retractSpeed = 4f;

	[SerializeField]
	private AudioClip badgeAttachSound;

	[SerializeField]
	private float badgeAttachSoundVolume;

	[SerializeField]
	public int dispenserIndex;

	public int actorNr;

	private Coroutine retractCoroutine;

	private int lastRedeemedPoints = -1;

	public void OnEntityInit()
	{
		gameEntity.manager.ghostReactorManager.reactor.employeeBadges.LinkBadgeToDispenser(this, (int)gameEntity.createData);
	}

	public void OnEntityDestroy()
	{
	}

	public void OnEntityStateChange(long prevState, long nextState)
	{
	}

	private void OnDestroy()
	{
		GhostReactor ghostReactor = GhostReactor.Get(gameEntity);
		if (ghostReactor != null && ghostReactor.employeeBadges != null)
		{
			ghostReactor.employeeBadges.RemoveBadge(this);
		}
	}

	public void Setup(NetPlayer player, int index)
	{
		gameEntity.onlyGrabActorNumber = player.ActorNumber;
		dispenserIndex = index;
		actorNr = player.ActorNumber;
		GRPlayer gRPlayer = GRPlayer.Get(player.ActorNumber);
		bool flag = (int)gameEntity.GetState() == 1;
		if (player.IsLocal)
		{
			flag |= Time.timeAsDouble < gRPlayer.lastLeftWithBadgeAttachedTime + 60.0;
		}
		if (gRPlayer != null && flag)
		{
			base.transform.position = gRPlayer.badgeBodyAnchor.position;
			gRPlayer.AttachBadge(this);
		}
		RefreshText(player);
	}

	public void RefreshText(NetPlayer player)
	{
		playerName.text = player.SanitizedNickName;
		GRPlayer gRPlayer = GRPlayer.Get(player.ActorNumber);
		if (gRPlayer != null && lastRedeemedPoints != gRPlayer.CurrentProgression.redeemedPoints)
		{
			lastRedeemedPoints = gRPlayer.CurrentProgression.redeemedPoints;
			playerTitle.text = GhostReactorProgression.GetTitleName(gRPlayer.CurrentProgression.redeemedPoints);
			playerLevel.text = GhostReactorProgression.GetGrade(gRPlayer.CurrentProgression.redeemedPoints).ToString();
		}
	}

	public void Hide()
	{
		badgeMesh.enabled = false;
		playerName.gameObject.SetActive(value: false);
		playerTitle.gameObject.SetActive(value: false);
		playerLevel.gameObject.SetActive(value: false);
	}

	public void UnHide()
	{
		badgeMesh.enabled = true;
		playerName.gameObject.SetActive(value: true);
		playerTitle.gameObject.SetActive(value: true);
		playerLevel.gameObject.SetActive(value: true);
	}

	public bool IsAttachedToPlayer()
	{
		return (int)gameEntity.GetState() == 1;
	}

	public void StartRetracting()
	{
		gameEntity.RequestState(gameEntity.id, 1L);
		PlayAttachFx();
		if (retractCoroutine != null)
		{
			StopCoroutine(retractCoroutine);
		}
		retractCoroutine = StartCoroutine(RetractCoroutine());
	}

	private IEnumerator RetractCoroutine()
	{
		base.transform.localRotation = Quaternion.identity;
		Vector3 localPosition = base.transform.localPosition;
		for (float sqrMagnitude = localPosition.sqrMagnitude; sqrMagnitude > 1E-05f; sqrMagnitude = localPosition.sqrMagnitude)
		{
			localPosition = Vector3.MoveTowards(localPosition, Vector3.zero, retractSpeed * Time.deltaTime);
			base.transform.localPosition = localPosition;
			yield return null;
			localPosition = base.transform.localPosition;
		}
		base.transform.localPosition = Vector3.zero;
	}

	private void PlayAttachFx()
	{
		if (audioSource != null)
		{
			audioSource.volume = badgeAttachSoundVolume;
			audioSource.clip = badgeAttachSound;
			audioSource.Play();
		}
	}
}
