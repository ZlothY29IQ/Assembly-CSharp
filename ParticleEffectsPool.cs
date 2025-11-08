using System.Collections;
using System.Collections.Generic;
using Cysharp.Text;
using UnityEngine;

public class ParticleEffectsPool : MonoBehaviour
{
	public ParticleEffect[] effects = new ParticleEffect[0];

	[Space]
	public int poolSize = 10;

	[Space]
	private RingBuffer<ParticleEffect>[] _pools = new RingBuffer<ParticleEffect>[0];

	private Dictionary<long, int> _effectToPool = new Dictionary<long, int>();

	public void Awake()
	{
		OnPoolAwake();
		Setup();
	}

	protected virtual void OnPoolAwake()
	{
	}

	private void Setup()
	{
		MoveToSceneWorldRoot();
		_pools = new RingBuffer<ParticleEffect>[effects.Length];
		_effectToPool = new Dictionary<long, int>(effects.Length);
		for (int i = 0; i < effects.Length; i++)
		{
			ParticleEffect particleEffect = effects[i];
			_pools[i] = InitPoolForPrefab(i, effects[i]);
			_effectToPool.TryAdd(particleEffect.effectID, i);
		}
	}

	private void MoveToSceneWorldRoot()
	{
		Transform obj = base.transform;
		obj.parent = null;
		obj.position = Vector3.zero;
		obj.rotation = Quaternion.identity;
		obj.localScale = Vector3.one;
	}

	private RingBuffer<ParticleEffect> InitPoolForPrefab(int index, ParticleEffect prefab)
	{
		RingBuffer<ParticleEffect> ringBuffer = new RingBuffer<ParticleEffect>(poolSize);
		string arg = prefab.name.Trim();
		for (int i = 0; i < poolSize; i++)
		{
			ParticleEffect particleEffect = Object.Instantiate(prefab, base.transform);
			particleEffect.gameObject.SetActive(value: false);
			particleEffect.pool = this;
			particleEffect.poolIndex = index;
			particleEffect.name = ZString.Concat(arg, "*", i);
			ringBuffer.Push(particleEffect);
		}
		return ringBuffer;
	}

	public void PlayEffect(ParticleEffect effect, Vector3 worldPos)
	{
		PlayEffect(effect.effectID, worldPos);
	}

	public void PlayEffect(ParticleEffect effect, Vector3 worldPos, float delay)
	{
		PlayEffect(effect.effectID, worldPos, delay);
	}

	public void PlayEffect(long effectID, Vector3 worldPos)
	{
		PlayEffect(GetPoolIndex(effectID), worldPos);
	}

	public void PlayEffect(long effectID, Vector3 worldPos, float delay)
	{
		PlayEffect(GetPoolIndex(effectID), worldPos, delay);
	}

	public void PlayEffect(int index, Vector3 worldPos)
	{
		if (index != -1 && _pools[index].TryPop(out var item))
		{
			item.transform.localPosition = worldPos;
			item.Play();
		}
	}

	public void PlayEffect(int index, Vector3 worldPos, float delay)
	{
		if (delay.Approx(0f))
		{
			PlayEffect(index, worldPos);
		}
		else
		{
			StartCoroutine(PlayDelayed(index, worldPos, delay));
		}
	}

	private IEnumerator PlayDelayed(int index, Vector3 worldPos, float delay)
	{
		yield return new WaitForSeconds(delay);
		PlayEffect(index, worldPos);
	}

	public void Return(ParticleEffect effect)
	{
		_pools[effect.poolIndex].Push(effect);
	}

	public int GetPoolIndex(long effectID)
	{
		if (_effectToPool.TryGetValue(effectID, out var value))
		{
			return value;
		}
		return -1;
	}
}
