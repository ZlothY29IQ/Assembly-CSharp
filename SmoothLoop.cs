using System.Collections;
using GorillaExtensions;
using UnityEngine;

public class SmoothLoop : MonoBehaviour, IGorillaSliceableSimple, IBuildValidation
{
	public AudioSource source;

	public float delay;

	public bool randomStart;

	[SerializeField]
	[Range(0f, 1f)]
	private float loopStart = 0.1f;

	[SerializeField]
	[Range(0f, 1f)]
	private float loopEnd = 0.95f;

	public bool BuildValidationCheck()
	{
		if (source == null)
		{
			Debug.LogError("missing audio source, this will fail", base.gameObject);
			return false;
		}
		return true;
	}

	private void Start()
	{
		if (delay != 0f && !randomStart)
		{
			source.GTStop();
			StartCoroutine(DelayedStart());
		}
		else if (randomStart)
		{
			if (source.isActiveAndEnabled)
			{
				source.GTPlay();
			}
			source.time = Random.Range(0f, source.clip.length);
		}
	}

	public void SliceUpdate()
	{
		if (base.enabled && source.time > source.clip.length * loopEnd)
		{
			source.time = loopStart;
		}
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		if (sourceCheck() && randomStart)
		{
			if (source.isActiveAndEnabled)
			{
				source.GTPlay();
			}
			source.time = Random.Range(0f, source.clip.length);
		}
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	private bool sourceCheck()
	{
		if (!source || !source.clip)
		{
			Debug.LogError("SmoothLoop: Disabling because AudioSource is null or has no clip assigned. Path: " + base.transform.GetPathQ(), this);
			base.enabled = false;
			StopAllCoroutines();
			return false;
		}
		return true;
	}

	public IEnumerator DelayedStart()
	{
		if (sourceCheck())
		{
			yield return new WaitForSeconds(delay);
			source.GTPlay();
		}
	}
}
