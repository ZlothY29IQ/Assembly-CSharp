using System;
using System.Collections;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

public class CrittersCageDeposit : CrittersActorDeposit
{
	private bool isHandlingDeposit;

	public Vector3 depositStartLocation;

	public Vector3 depositEndLocation;

	public float submitDuration = 0.5f;

	public float returnDuration = 1f;

	public AudioSource depositAudio;

	public AudioClip depositStartSound;

	public AudioClip depositEmptySound;

	public AudioClip depositCritterSound;

	private CrittersActor currentCage;

	public event Action<Menagerie.CritterData, int> OnDepositCritter;

	private void Awake()
	{
		attachPoint.OnGrabbedChild += StartProcessCage;
	}

	protected override bool CanDeposit(CrittersActor depositActor)
	{
		if (base.CanDeposit(depositActor))
		{
			return !isHandlingDeposit;
		}
		return false;
	}

	private void StartProcessCage(CrittersActor depositedActor)
	{
		currentCage = depositedActor;
		StartCoroutine(ProcessCage());
	}

	private IEnumerator ProcessCage()
	{
		isHandlingDeposit = true;
		bool isLocalDeposit = currentCage.lastGrabbedPlayer == PhotonNetwork.LocalPlayer.ActorNumber;
		depositAudio.GTPlayOneShot(depositStartSound, isLocalDeposit ? 1f : 0.5f);
		float transition = 0f;
		CrittersPawn crittersPawn = currentCage.GetComponentInChildren<CrittersPawn>();
		int lastGrabbedPlayer = currentCage.lastGrabbedPlayer;
		Menagerie.CritterData critterData = ((!crittersPawn.IsNotNull()) ? new Menagerie.CritterData() : new Menagerie.CritterData(crittersPawn.visuals));
		while (transition < submitDuration)
		{
			transition += Time.deltaTime;
			attachPoint.transform.localPosition = Vector3.Lerp(depositStartLocation, depositEndLocation, Mathf.Min(transition / submitDuration, 1f));
			yield return null;
		}
		if (crittersPawn.IsNotNull())
		{
			this.OnDepositCritter?.Invoke(critterData, lastGrabbedPlayer);
			Vector3 zero = Vector3.zero;
			crittersPawn.Released(keepWorldPosition: false, default(Quaternion), zero);
			crittersPawn.gameObject.SetActive(value: false);
			depositAudio.GTPlayOneShot(depositCritterSound, isLocalDeposit ? 1f : 0.5f);
		}
		else
		{
			depositAudio.GTPlayOneShot(depositEmptySound, isLocalDeposit ? 1f : 0.5f);
		}
		currentCage.transform.position = Vector3.zero;
		currentCage.gameObject.SetActive(value: false);
		currentCage = null;
		transition = 0f;
		while (transition < returnDuration)
		{
			transition += Time.deltaTime;
			attachPoint.transform.localPosition = Vector3.Lerp(depositEndLocation, depositStartLocation, Mathf.Min(transition / returnDuration, 1f));
			yield return null;
		}
		isHandlingDeposit = false;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(base.transform.TransformPoint(depositStartLocation), 0.1f);
		Gizmos.DrawLine(base.transform.TransformPoint(depositStartLocation), base.transform.TransformPoint(depositEndLocation));
		Gizmos.DrawWireSphere(base.transform.TransformPoint(depositEndLocation), 0.1f);
	}
}
