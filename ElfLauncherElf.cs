using System.Collections;
using UnityEngine;

public class ElfLauncherElf : MonoBehaviour
{
	[SerializeField]
	private Rigidbody rb;

	[SerializeField]
	private SoundBankPlayer bounceAudio;

	[SerializeField]
	private float bounceAudioCooldownDuration;

	[SerializeField]
	private float destroyAfterDuration;

	private float bounceAudioCoolingDownUntilTimestamp;

	private void OnEnable()
	{
		StartCoroutine(ReturnToPoolAfterDelayCo());
	}

	private IEnumerator ReturnToPoolAfterDelayCo()
	{
		yield return new WaitForSeconds(destroyAfterDuration);
		ObjectPools.instance.Destroy(base.gameObject);
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (!(bounceAudioCoolingDownUntilTimestamp > Time.time))
		{
			bounceAudio.Play();
			bounceAudioCoolingDownUntilTimestamp = Time.time + bounceAudioCooldownDuration;
		}
	}

	private void FixedUpdate()
	{
		rb.AddForce(base.transform.lossyScale.x * Physics.gravity * rb.mass, ForceMode.Force);
	}
}
