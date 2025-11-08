using System.Collections.Generic;
using UnityEngine;

public class GRArmorEnemy : MonoBehaviour
{
	[SerializeField]
	private List<Renderer> renderers;

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
	private List<Material> armorStateMaterials;

	[SerializeField]
	private int[] armorStateThresholds;

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
		Hide(renderers, !flag);
		if (!flag || armorStateMaterials.Count <= 0 || armorStateMaterials.Count != armorStateThresholds.Length)
		{
			return;
		}
		Material material = armorStateMaterials[0];
		for (int i = 0; i < armorStateMaterials.Count && hp <= armorStateThresholds[i]; i++)
		{
			material = armorStateMaterials[i];
			if (hp == armorStateThresholds[i])
			{
				break;
			}
		}
		if (material != renderers[0].material)
		{
			renderers[0].material = material;
			SetArmorColor(GetArmorColor());
		}
	}

	public void SetArmorColor(Color newColor)
	{
		if (renderers != null && renderers.Count > 0)
		{
			renderers[0].material.SetColor("_BaseColor", newColor);
		}
	}

	public Color GetArmorColor()
	{
		Color result = Color.white;
		if (renderers.Count > 0)
		{
			result = renderers[0].material.GetColor("_BaseColor");
		}
		return result;
	}

	public static void Hide(List<Renderer> renderers, bool hide)
	{
		if (renderers == null)
		{
			return;
		}
		for (int i = 0; i < renderers.Count; i++)
		{
			if (renderers[i] != null)
			{
				renderers[i].enabled = !hide;
			}
		}
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
