using UnityEngine;

public class GorillaMouthFlap : MonoBehaviour, IGorillaSliceableSimple
{
	public GameObject targetFace;

	public MouthFlapLevel[] mouthFlapLevels;

	public MouthFlapLevel noMicFace;

	public MouthFlapLevel leafBlowerFace;

	private bool useMicEnabled;

	private float leafBlowerActiveUntilTimestamp;

	private int activeFlipbookIndex;

	private float activeFlipbookPlayTime;

	private GorillaSpeakerLoudness speaker;

	private float lastTimeUpdated;

	private float deltaTime;

	private Renderer targetFaceRenderer;

	private MaterialPropertyBlock facePropBlock;

	private Texture defaultMouthAtlas;

	private Material defaultFaceMaterial;

	private bool hasDefaultMouthAtlas;

	private bool hasDefaultFaceMaterial;

	private ShaderHashId _MouthMap = "_MouthMap";

	private ShaderHashId _BaseMap = "_BaseMap";

	private void Start()
	{
		speaker = GetComponent<GorillaSpeakerLoudness>();
		targetFaceRenderer = targetFace.GetComponent<Renderer>();
		facePropBlock = new MaterialPropertyBlock();
		hasDefaultMouthAtlas = false;
		if (targetFaceRenderer != null)
		{
			SetDefaultMouthAtlas(targetFaceRenderer.material);
		}
	}

	public void EnableLeafBlower()
	{
		leafBlowerActiveUntilTimestamp = Time.time + 0.1f;
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		lastTimeUpdated = Time.time;
		deltaTime = Time.deltaTime;
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void SliceUpdate()
	{
		deltaTime = Time.time - lastTimeUpdated;
		lastTimeUpdated = Time.time;
		if (speaker == null)
		{
			speaker = GetComponent<GorillaSpeakerLoudness>();
			return;
		}
		float currentLoudness = 0f;
		if (speaker.IsSpeaking)
		{
			currentLoudness = speaker.Loudness;
		}
		CheckMouthflapChange(speaker.IsMicEnabled, currentLoudness);
		MouthFlapLevel mouthFlap = noMicFace;
		if (leafBlowerActiveUntilTimestamp > Time.time)
		{
			mouthFlap = leafBlowerFace;
		}
		else if (useMicEnabled)
		{
			mouthFlap = mouthFlapLevels[activeFlipbookIndex];
		}
		UpdateMouthFlapFlipbook(mouthFlap);
	}

	private void CheckMouthflapChange(bool isMicEnabled, float currentLoudness)
	{
		if (isMicEnabled)
		{
			useMicEnabled = true;
			int num = mouthFlapLevels.Length - 1;
			while (num >= 0 && currentLoudness < mouthFlapLevels[num].maxRequiredVolume)
			{
				if (currentLoudness > mouthFlapLevels[num].minRequiredVolume)
				{
					if (activeFlipbookIndex != num)
					{
						activeFlipbookIndex = num;
						activeFlipbookPlayTime = 0f;
					}
					break;
				}
				num--;
			}
		}
		else if (useMicEnabled)
		{
			useMicEnabled = false;
			activeFlipbookPlayTime = 0f;
		}
	}

	private void UpdateMouthFlapFlipbook(MouthFlapLevel mouthFlap)
	{
		Material material = targetFaceRenderer.material;
		activeFlipbookPlayTime += deltaTime;
		activeFlipbookPlayTime %= mouthFlap.cycleDuration;
		int num = Mathf.FloorToInt(activeFlipbookPlayTime * (float)mouthFlap.faces.Length / mouthFlap.cycleDuration);
		material.SetTextureOffset(_MouthMap, mouthFlap.faces[num]);
	}

	public void SetMouthTextureReplacement(Texture2D replacementMouthAtlas)
	{
		Material material = targetFaceRenderer.material;
		SetDefaultMouthAtlas(material);
		material.SetTexture(_MouthMap, replacementMouthAtlas);
	}

	public void ClearMouthTextureReplacement()
	{
		targetFaceRenderer.material.SetTexture(_MouthMap, defaultMouthAtlas);
	}

	public Material SetFaceMaterialReplacement(Material replacementFaceMaterial)
	{
		if (!hasDefaultFaceMaterial)
		{
			defaultFaceMaterial = targetFaceRenderer.material;
			hasDefaultFaceMaterial = true;
		}
		targetFaceRenderer.material = replacementFaceMaterial;
		if (hasDefaultMouthAtlas && defaultMouthAtlas != null)
		{
			targetFaceRenderer.material.SetTexture(_MouthMap, defaultMouthAtlas);
		}
		return targetFaceRenderer.material;
	}

	public void ClearFaceMaterialReplacement()
	{
		if (hasDefaultFaceMaterial)
		{
			targetFaceRenderer.material = defaultFaceMaterial;
		}
	}

	private void SetDefaultMouthAtlas(Material face)
	{
		if (!hasDefaultMouthAtlas)
		{
			defaultMouthAtlas = face.GetTexture(_MouthMap);
			hasDefaultMouthAtlas = true;
		}
	}
}
