using System.Collections;
using Photon.Pun;
using UnityEngine;

public class GRDropZone : MonoBehaviour
{
	[SerializeField]
	private GameObject vfxRoot;

	[SerializeField]
	private GameObject sfxPrefab;

	public float effectDuration = 1f;

	private bool playingEffect;

	[SerializeField]
	private Vector3 repelDirectionLocal = Vector3.up;

	private Vector3 repelDirectionWorld = Vector3.up;

	private void Awake()
	{
		repelDirectionWorld = base.transform.TransformDirection(repelDirectionLocal.normalized);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (PhotonNetwork.IsMasterClient)
		{
			GameEntity component = other.attachedRigidbody.GetComponent<GameEntity>();
			if (component != null && component.manager.ghostReactorManager != null)
			{
				GhostReactorManager.Get(component).EntityEnteredDropZone(component);
			}
		}
	}

	public Vector3 GetRepelDirectionWorld()
	{
		return repelDirectionWorld;
	}

	public void PlayEffect()
	{
		if (vfxRoot != null && !playingEffect)
		{
			vfxRoot.SetActive(value: true);
			playingEffect = true;
			if (sfxPrefab != null)
			{
				ObjectPools.instance.Instantiate(sfxPrefab, base.transform.position, base.transform.rotation);
			}
			StartCoroutine(DelayedStopEffect());
		}
	}

	private IEnumerator DelayedStopEffect()
	{
		yield return new WaitForSeconds(effectDuration);
		vfxRoot.SetActive(value: false);
		playingEffect = false;
	}
}
