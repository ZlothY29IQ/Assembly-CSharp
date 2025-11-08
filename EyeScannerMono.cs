using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Text;
using GorillaLocomotion;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class EyeScannerMono : MonoBehaviour, ISpawnable, IGorillaSliceableSimple
{
	[FormerlySerializedAs("_scanDistance")]
	[Tooltip("Any scannables with transforms beyond this distance will be automatically ignored.")]
	[SerializeField]
	private float m_scanDistanceMax = 10f;

	[SerializeField]
	private float m_scanDistanceMin = 0.5f;

	[FormerlySerializedAs("_textTyper")]
	[Tooltip("The component that handles setting text in the TextMeshPro and animates the text typing.")]
	[SerializeField]
	private TextTyperAnimatorMono m_textTyper;

	[SerializeField]
	private Transform m_reticle;

	[SerializeField]
	private Transform m_overlay;

	[SerializeField]
	private GameObject m_overlayBg;

	[SerializeField]
	private float m_reticleScale = 1f;

	[SerializeField]
	private float m_textScale = 1f;

	[SerializeField]
	private float m_overlayScale = 1f;

	[SerializeField]
	private Vector3 m_pointerOffset;

	[SerializeField]
	private Vector2 m_position;

	[HideInInspector]
	[SerializeField]
	private Color32 m_keyTextColor = new Color32(byte.MaxValue, 34, 0, byte.MaxValue);

	private string _keyRichTextColorTagString = "";

	private static readonly List<IEyeScannable> _registeredScannables = new List<IEyeScannable>(128);

	private static readonly HashSet<int> _registeredScannableIds = new HashSet<int>(128);

	private IEyeScannable _oldClosestScannable;

	private Utf16ValueStringBuilder _sb;

	private readonly int[] _entryIndexes = new int[16];

	[SerializeField]
	private LayerMask _layerMask;

	private Camera _firstPersonCamera;

	private bool _has_firstPersonCamera;

	[SerializeField]
	private float m_LookPrecision = 0.65f;

	[SerializeField]
	private bool m_xrayVision;

	private LineRenderer _line;

	private Color32 KeyTextColor
	{
		get
		{
			return m_keyTextColor;
		}
		set
		{
			m_keyTextColor = value;
			_keyRichTextColorTagString = string.Format(CultureInfo.InvariantCulture.NumberFormat, "<color=#{0:X2}{1:X2}{2:X2}>", value.r, value.g, value.b);
		}
	}

	private List<IEyeScannable> registeredScannables => _registeredScannables;

	bool ISpawnable.IsSpawned { get; set; }

	ECosmeticSelectSide ISpawnable.CosmeticSelectedSide { get; set; }

	public string DebugData { get; private set; }

	public static void Register(IEyeScannable scannable)
	{
		if (_registeredScannableIds.Add(scannable.scannableId))
		{
			_registeredScannables.Add(scannable);
		}
	}

	public static void Unregister(IEyeScannable scannable)
	{
		if (_registeredScannableIds.Remove(scannable.scannableId))
		{
			_registeredScannables.Remove(scannable);
		}
	}

	protected void Awake()
	{
		_sb = ZString.CreateStringBuilder();
		KeyTextColor = KeyTextColor;
		math.sign(m_textTyper.transform.parent.localScale);
		m_textTyper.SetText(string.Empty);
		m_reticle.gameObject.SetActive(value: false);
		m_textTyper.gameObject.SetActive(value: false);
		m_overlayBg.SetActive(value: false);
		_line = GetComponent<LineRenderer>();
		_line.enabled = false;
	}

	public void OnSpawn(VRRig rig)
	{
		if (rig != null && !rig.isOfflineVRRig)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		if (GTPlayer.hasInstance)
		{
			GTPlayer instance = GTPlayer.Instance;
			_firstPersonCamera = instance.GetComponentInChildren<Camera>();
			_has_firstPersonCamera = _firstPersonCamera != null;
		}
	}

	public void OnDespawn()
	{
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	void IGorillaSliceableSimple.SliceUpdate()
	{
		if (GorillaTagger.Instance.offlineVRRig.zoneEntity.currentZone == GTZone.bayou)
		{
			if (_oldClosestScannable != null)
			{
				_OnScannableChanged(null, typeingShow: false);
				_oldClosestScannable = null;
			}
			return;
		}
		IEyeScannable eyeScannable = null;
		Transform obj = base.transform;
		Vector3 position = obj.position;
		Vector3 forward = obj.forward;
		float num = m_LookPrecision;
		for (int i = 0; i < _registeredScannables.Count; i++)
		{
			IEyeScannable eyeScannable2 = _registeredScannables[i];
			Vector3 normalized = (eyeScannable2.Position - position).normalized;
			float num2 = Vector3.Distance(position, eyeScannable2.Position);
			float num3 = Vector3.Dot(forward, normalized);
			if (!(num2 >= m_scanDistanceMin) || !(num2 <= m_scanDistanceMax) || !(num3 > num))
			{
				continue;
			}
			if (!m_xrayVision && Physics.Raycast(position, normalized, out var hitInfo, m_scanDistanceMax, _layerMask.value))
			{
				IEyeScannable componentInParent = hitInfo.collider.GetComponentInParent<IEyeScannable>();
				if (componentInParent == null || componentInParent != eyeScannable2)
				{
					continue;
				}
			}
			num = num3;
			eyeScannable = eyeScannable2;
		}
		if (eyeScannable != _oldClosestScannable)
		{
			if (_oldClosestScannable != null)
			{
				_oldClosestScannable.OnDataChange -= Scannable_OnDataChange;
			}
			_OnScannableChanged(eyeScannable, typeingShow: true);
			_oldClosestScannable = eyeScannable;
			if (_oldClosestScannable != null)
			{
				_oldClosestScannable.OnDataChange += Scannable_OnDataChange;
			}
		}
	}

	private void Scannable_OnDataChange()
	{
		_OnScannableChanged(_oldClosestScannable, typeingShow: false);
	}

	private void LateUpdate()
	{
		if (_oldClosestScannable != null)
		{
			m_reticle.position = _oldClosestScannable.Position;
			float num = math.distance(base.transform.position, m_reticle.position);
			Mathf.Clamp(num * 0.33333f, 0f, 1f);
			float num2 = num * m_reticleScale;
			float num3 = num * m_textScale;
			float num4 = num * m_overlayScale;
			m_reticle.localScale = new Vector3(num2, num2, num2);
			m_overlay.localPosition = new Vector3(m_position.x * num, m_position.y * num, num);
			m_overlay.localScale = new Vector3(num4, num4, 1f);
			_line.SetPosition(0, m_reticle.position);
			_line.SetPosition(1, m_textTyper.transform.position + m_pointerOffset * num3);
			_line.widthMultiplier = num2;
		}
	}

	private void _OnScannableChanged(IEyeScannable scannable, bool typeingShow)
	{
		_sb.Clear();
		if (scannable == null)
		{
			m_textTyper.SetText(_sb);
			m_textTyper.gameObject.SetActive(value: false);
			m_reticle.gameObject.SetActive(value: false);
			m_overlayBg.SetActive(value: false);
			m_reticle.parent = base.transform;
			_line.enabled = false;
			return;
		}
		m_reticle.gameObject.SetActive(value: true);
		m_textTyper.gameObject.SetActive(value: true);
		m_overlayBg.SetActive(value: true);
		m_reticle.position = scannable.Position;
		_line.enabled = true;
		_sb.AppendLine(DebugData);
		_entryIndexes[0] = 0;
		int i = 1;
		int num = 0;
		for (int j = 0; j < scannable.Entries.Count; j++)
		{
			KeyValueStringPair keyValueStringPair = scannable.Entries[j];
			if (!string.IsNullOrEmpty(keyValueStringPair.Key))
			{
				_sb.Append(_keyRichTextColorTagString);
				_sb.Append(keyValueStringPair.Key);
				_sb.Append("</color>: ");
				num += keyValueStringPair.Key.Length + 2;
			}
			if (!string.IsNullOrEmpty(keyValueStringPair.Value))
			{
				_sb.Append(keyValueStringPair.Value);
				num += keyValueStringPair.Value.Length;
			}
			_sb.AppendLine();
			num += Environment.NewLine.Length;
			if (i < _entryIndexes.Length)
			{
				_entryIndexes[i++] = num - 1;
			}
		}
		for (; i < _entryIndexes.Length; i++)
		{
			_entryIndexes[i] = -1;
		}
		if (typeingShow)
		{
			m_textTyper.SetText(_sb, _entryIndexes, num);
		}
		else
		{
			m_textTyper.UpdateText(_sb, num);
		}
	}
}
