using UnityEngine;

namespace GorillaTag.Cosmetics;

public class ParticleModifierCosmetic : MonoBehaviour
{
	[SerializeField]
	private ParticleSystem ps;

	[Tooltip("For calling gradual functions only")]
	[SerializeField]
	private float transitionSpeed = 5f;

	public ParticleSettingsSO[] particleSettings = new ParticleSettingsSO[0];

	private float originalStartSize;

	private Color originalStartColor;

	private float? targetSize;

	private Color? targetColor;

	private int currentIndex;

	private void Awake()
	{
		StoreOriginalValues();
		currentIndex = -1;
	}

	private void OnValidate()
	{
		StoreOriginalValues();
	}

	private void OnEnable()
	{
		StoreOriginalValues();
	}

	private void OnDisable()
	{
		ResetToOriginal();
	}

	private void StoreOriginalValues()
	{
		if (!(ps == null))
		{
			ParticleSystem.MainModule main = ps.main;
			originalStartSize = main.startSize.constant;
			originalStartColor = main.startColor.color;
		}
	}

	public void ApplySetting(ParticleSettingsSO setting)
	{
		SetStartSize(setting.startSize);
		SetStartColor(setting.startColor);
	}

	public void ApplySettingLerp(ParticleSettingsSO setting)
	{
		LerpStartSize(setting.startSize);
		LerpStartColor(setting.startColor);
	}

	public void MoveToNextSetting()
	{
		currentIndex++;
		if (currentIndex > -1 && currentIndex < particleSettings.Length)
		{
			ParticleSettingsSO setting = particleSettings[currentIndex];
			ApplySetting(setting);
		}
	}

	public void MoveToNextSettingLerp()
	{
		currentIndex++;
		if (currentIndex > -1 && currentIndex < particleSettings.Length)
		{
			ParticleSettingsSO setting = particleSettings[currentIndex];
			ApplySettingLerp(setting);
		}
	}

	public void ResetSettings()
	{
		currentIndex = -1;
		ResetToOriginal();
	}

	public void MoveToSettingIndex(int index)
	{
		if (index > -1 && index < particleSettings.Length)
		{
			ParticleSettingsSO setting = particleSettings[index];
			ApplySetting(setting);
		}
	}

	public void MoveToSettingIndexLerp(int index)
	{
		if (index > -1 && index < particleSettings.Length)
		{
			ParticleSettingsSO setting = particleSettings[index];
			ApplySettingLerp(setting);
		}
	}

	public void SetStartSize(float size)
	{
		if (!(ps == null))
		{
			ParticleSystem.MainModule main = ps.main;
			main.startSize = size;
			targetSize = null;
		}
	}

	public void IncreaseStartSize(float delta)
	{
		if (!(ps == null))
		{
			ParticleSystem.MainModule main = ps.main;
			float constant = main.startSize.constant;
			main.startSize = constant + delta;
			targetSize = null;
		}
	}

	public void LerpStartSize(float size)
	{
		if (!(ps == null) && !(Mathf.Abs(ps.main.startSize.constant - size) < 0.01f))
		{
			targetSize = size;
		}
	}

	public void SetStartColor(Color color)
	{
		if (!(ps == null))
		{
			ParticleSystem.MainModule main = ps.main;
			main.startColor = color;
			targetColor = null;
		}
	}

	public void LerpStartColor(Color color)
	{
		if (!(ps == null))
		{
			Color color2 = ps.main.startColor.color;
			if (!IsColorApproximatelyEqual(color2, color))
			{
				targetColor = color;
			}
		}
	}

	public void SetStartValues(float size, Color color)
	{
		SetStartSize(size);
		SetStartColor(color);
	}

	public void LerpStartValues(float size, Color color)
	{
		LerpStartSize(size);
		LerpStartColor(color);
	}

	private void Update()
	{
		if (ps == null)
		{
			return;
		}
		ParticleSystem.MainModule main = ps.main;
		if (targetSize.HasValue)
		{
			float num = Mathf.Lerp(main.startSize.constant, targetSize.Value, Time.deltaTime * transitionSpeed);
			main.startSize = num;
			if (Mathf.Abs(num - targetSize.Value) < 0.01f)
			{
				main.startSize = targetSize.Value;
				targetSize = null;
			}
		}
		if (targetColor.HasValue)
		{
			Color color = Color.Lerp(main.startColor.color, targetColor.Value, Time.deltaTime * transitionSpeed);
			main.startColor = color;
			if (IsColorApproximatelyEqual(color, targetColor.Value))
			{
				main.startColor = targetColor.Value;
				targetColor = null;
			}
		}
	}

	[ContextMenu("Reset To Original")]
	public void ResetToOriginal()
	{
		if (!(ps == null))
		{
			targetSize = null;
			targetColor = null;
			ParticleSystem.MainModule main = ps.main;
			main.startSize = originalStartSize;
			main.startColor = originalStartColor;
		}
	}

	private bool IsColorApproximatelyEqual(Color a, Color b, float threshold = 0.0001f)
	{
		float num = a.r - b.r;
		float num2 = a.g - b.g;
		float num3 = a.b - b.b;
		float num4 = a.a - b.a;
		return num * num + num2 * num2 + num3 * num3 + num4 * num4 < threshold;
	}
}
