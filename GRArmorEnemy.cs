using System;
using System.Collections.Generic;
using UnityEngine;

public class GRArmorEnemy : MonoBehaviour
{
	[Serializable]
	public struct GREnemyArmorLevel
	{
		public int healthThreshold;

		public Material mainRendererMaterial;

		public List<GameObject> visibleObjects;

		public List<GameObject> hiddenObjects;
	}

	[SerializeField]
	private List<Renderer> renderers;

	[SerializeField]
	private List<GameObject> visibleObjects;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private GameObject fxHit;

	[SerializeField]
	private AudioClip hitSound;

	[SerializeField]
	private float hitSoundVolume;

	[SerializeField]
	private GameObject fxBlock;

	[SerializeField]
	private AudioClip blockSound;

	[SerializeField]
	private float blockSoundVolume;

	[SerializeField]
	private GameObject fxDestroy;

	[SerializeField]
	private AudioClip destroySound;

	[SerializeField]
	private float destroySoundVolume;

	[SerializeField]
	public List<GREnemyArmorLevel> armorStateData;

	[SerializeField]
	public Renderer materialSwapRenderer;

	private GameEntity entity;

	public GameObject armorFragmentPrefab;

	public Vector3 fragmentSpawnOffset = new Vector3(0f, 0.5f, 0.5f);

	public int numFragmentsWhenShattered = 3;

	public float fragmentLaunchPitch = 30f;

	private int hp;

	private void Awake()
	{
		SetHp(0);
		entity = GetComponent<GameEntity>();
	}

	public void SetHp(int hp)
	{
		this.hp = hp;
		RefreshArmor();
	}

	private void RefreshArmor()
	{
		bool flag = hp > 0;
		GREnemy.HideRenderers(renderers, !flag);
		GREnemy.HideObjects(visibleObjects, !flag);
		if (armorStateData.Count <= 0)
		{
			return;
		}
		int num = -1;
		Material mainRendererMaterial = armorStateData[0].mainRendererMaterial;
		for (int i = 0; i < armorStateData.Count; i++)
		{
			num = i;
			mainRendererMaterial = armorStateData[i].mainRendererMaterial;
			if (hp >= armorStateData[i].healthThreshold)
			{
				break;
			}
		}
		if (flag && materialSwapRenderer != null && mainRendererMaterial != materialSwapRenderer.material)
		{
			materialSwapRenderer.material = mainRendererMaterial;
			SetArmorColor(GetArmorColor());
		}
		if (num == -1)
		{
			return;
		}
		GREnemy.HideObjects(armorStateData[num].visibleObjects, !flag);
		for (int j = 0; j < armorStateData[num].hiddenObjects.Count; j++)
		{
			GameObject gameObject = armorStateData[num].hiddenObjects[j];
			if (gameObject.activeInHierarchy)
			{
				PlayDestroyFx(gameObject.transform.position);
			}
		}
		GREnemy.HideObjects(armorStateData[num].hiddenObjects, hide: true);
	}

	public void SetArmorColor(Color newColor)
	{
		if (renderers != null && renderers.Count > 0)
		{
			materialSwapRenderer.material.SetColor("_BaseColor", newColor);
		}
	}

	public Color GetArmorColor()
	{
		Color result = Color.white;
		if (materialSwapRenderer != null)
		{
			result = materialSwapRenderer.material.GetColor("_BaseColor");
		}
		return result;
	}

	public void PlayHitFx(Vector3 position)
	{
		PlayFx(fxHit, position);
		PlaySound(hitSound, hitSoundVolume, position);
	}

	public void PlayBlockFx(Vector3 position)
	{
		PlayFx(fxBlock, position);
		PlaySound(blockSound, blockSoundVolume, position);
	}

	public void PlayDestroyFx(Vector3 position)
	{
		PlayFx(fxDestroy, position);
		PlaySound(destroySound, destroySoundVolume, position);
	}

	private void PlayFx(GameObject fx, Vector3 position)
	{
		if (!(fx == null))
		{
			fx.SetActive(value: false);
			fx.SetActive(value: true);
		}
	}

	private void PlaySound(AudioClip clip, float volume, Vector3 position)
	{
		audioSource.clip = clip;
		audioSource.volume = volume;
		audioSource.Play();
	}

	public void FragmentArmor()
	{
		if (entity.IsAuthority())
		{
			float num = 0f;
			for (int i = 0; i < numFragmentsWhenShattered; i++)
			{
				num += 360f / (float)numFragmentsWhenShattered;
				Quaternion quaternion = Quaternion.Euler(0f, num, fragmentLaunchPitch);
				Vector3 vector = quaternion * fragmentSpawnOffset;
				entity.manager.RequestCreateItem(armorFragmentPrefab.name.GetStaticHash(), base.transform.position + vector, quaternion, entity.GetNetId());
			}
		}
	}
}
