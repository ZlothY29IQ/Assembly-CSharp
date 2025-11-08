using System.Collections;
using Photon.Pun;
using UnityEngine;

public class ChestHeartbeat : MonoBehaviour
{
	public int millisToWait;

	public int millisMin = 300;

	public int lastShot;

	public AudioSource audioSource;

	public Transform scaleTransform;

	private float deltaTime;

	private float heartMinSize = 0.9f;

	private float heartMaxSize = 1.2f;

	private float minTime = 0.05f;

	private float maxTime = 0.1f;

	private float endtime = 0.25f;

	private float currentTime;

	public void Update()
	{
		if (PhotonNetwork.InRoom)
		{
			if ((PhotonNetwork.ServerTimestamp > lastShot + millisMin || Mathf.Abs(PhotonNetwork.ServerTimestamp - lastShot) > 10000) && PhotonNetwork.ServerTimestamp % 1500 <= 10)
			{
				lastShot = PhotonNetwork.ServerTimestamp;
				audioSource.GTPlayOneShot(audioSource.clip);
				StartCoroutine(HeartBeat());
			}
		}
		else if ((Time.time * 1000f > (float)(lastShot + millisMin) || Mathf.Abs(Time.time * 1000f - (float)lastShot) > 10000f) && Time.time * 1000f % 1500f <= 10f)
		{
			lastShot = PhotonNetwork.ServerTimestamp;
			audioSource.GTPlayOneShot(audioSource.clip);
			StartCoroutine(HeartBeat());
		}
	}

	private IEnumerator HeartBeat()
	{
		float startTime = Time.time;
		while (Time.time < startTime + endtime)
		{
			if (Time.time < startTime + minTime)
			{
				deltaTime = Time.time - startTime;
				scaleTransform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * heartMinSize, deltaTime / minTime);
			}
			else if (Time.time < startTime + maxTime)
			{
				deltaTime = Time.time - startTime - minTime;
				scaleTransform.localScale = Vector3.Lerp(Vector3.one * heartMinSize, Vector3.one * heartMaxSize, deltaTime / (maxTime - minTime));
			}
			else if (Time.time < startTime + endtime)
			{
				deltaTime = Time.time - startTime - maxTime;
				scaleTransform.localScale = Vector3.Lerp(Vector3.one * heartMaxSize, Vector3.one, deltaTime / (endtime - maxTime));
			}
			yield return new WaitForFixedUpdate();
		}
	}
}
