using System;
using GorillaTagScripts;
using UnityEngine;

public class GorillaCaveCrystalVisuals : MonoBehaviour
{
	public CrystalVisualsPreset crysalPreset;

	[SerializeField]
	[Range(0f, 1f)]
	private float _lerp;

	[Space]
	public MeshRenderer _renderer;

	public Material _sharedMaterial;

	[SerializeField]
	public Texture2D instanceAlbedo;

	[SerializeField]
	private bool _initialized;

	[SerializeField]
	private int _lastState;

	[SerializeField]
	public GorillaCaveCrystalSetup _setup;

	private MaterialPropertyBlock _block;

	[NonSerialized]
	private bool _ranSetupOnce;

	private static readonly ShaderHashId _Color = "_Color";

	private static readonly ShaderHashId _EmissionColor = "_EmissionColor";

	private static readonly ShaderHashId _MainTex = "_MainTex";

	public float lerp
	{
		get
		{
			return _lerp;
		}
		set
		{
			_lerp = value;
		}
	}

	public void Setup()
	{
		TryGetComponent<MeshRenderer>(out _renderer);
		if (!(_renderer == null))
		{
			_setup = GorillaCaveCrystalSetup.Instance;
			_sharedMaterial = _renderer.sharedMaterial;
			_initialized = crysalPreset != null && _renderer != null && _sharedMaterial != null;
			Update();
		}
	}

	private void Start()
	{
		UpdateAlbedo();
		ForceUpdate();
	}

	public void UpdateAlbedo()
	{
		if (_initialized && !(instanceAlbedo == null))
		{
			if (_block == null)
			{
				_block = new MaterialPropertyBlock();
			}
			_renderer.GetPropertyBlock(_block);
			_block.SetTexture(_MainTex, instanceAlbedo);
			_renderer.SetPropertyBlock(_block);
		}
	}

	private void Awake()
	{
		UpdateAlbedo();
		Update();
	}

	private void Update()
	{
		if (!_initialized)
		{
			return;
		}
		if (Application.isPlaying)
		{
			int hashCode = (crysalPreset, _lerp).GetHashCode();
			if (_lastState == hashCode)
			{
				return;
			}
			_lastState = hashCode;
		}
		if (_block == null)
		{
			_block = new MaterialPropertyBlock();
		}
		CrystalVisualsPreset.VisualState stateA = crysalPreset.stateA;
		CrystalVisualsPreset.VisualState stateB = crysalPreset.stateB;
		Color value = Color.Lerp(stateA.albedo, stateB.albedo, _lerp);
		Color value2 = Color.Lerp(stateA.emission, stateB.emission, _lerp);
		_renderer.GetPropertyBlock(_block);
		_block.SetColor(_Color, value);
		_block.SetColor(_EmissionColor, value2);
		_renderer.SetPropertyBlock(_block);
	}

	public void ForceUpdate()
	{
		_lastState = 0;
		Update();
	}

	private static void InitializeCrystals()
	{
		GorillaCaveCrystalVisuals[] array = UnityEngine.Object.FindObjectsByType<GorillaCaveCrystalVisuals>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
		foreach (GorillaCaveCrystalVisuals obj in array)
		{
			obj.UpdateAlbedo();
			obj.ForceUpdate();
			obj._lastState = -1;
		}
	}
}
