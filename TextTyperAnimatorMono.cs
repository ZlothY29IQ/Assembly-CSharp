using System;
using System.Collections.Generic;
using Cysharp.Text;
using GorillaExtensions;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class TextTyperAnimatorMono : MonoBehaviour, IGorillaSliceableSimple
{
	[FormerlySerializedAs("_textMesh")]
	[Tooltip("Text Mesh Pro component.")]
	[SerializeField]
	private TMP_Text m_textMesh;

	[Tooltip("Delay between characters in seconds")]
	[SerializeField]
	private Vector2 m_typingSpeedMinMax = new Vector2(0.05f, 0.1f);

	[Header("Audio")]
	[Tooltip("AudioClips to play while typing.")]
	[SerializeField]
	private SoundBankPlayer m_typingSoundBank;

	private bool _has_typingSoundBank;

	[Tooltip("AudioClips to play when a ")]
	[SerializeField]
	private SoundBankPlayer m_beginEntrySoundBank;

	private bool _has_beginEntrySoundBank;

	private int _charCount;

	private readonly List<int> _entryIndexes = new List<int>(16);

	private float _waitTime;

	private float _timeOfLastTypedChar = -1f;

	private Unity.Mathematics.Random _random = new Unity.Mathematics.Random(6746u);

	public void EdRestartAnimation()
	{
		m_textMesh.maxVisibleCharacters = 0;
	}

	protected void Awake()
	{
		_has_typingSoundBank = m_typingSoundBank != null;
		_has_beginEntrySoundBank = m_beginEntrySoundBank != null;
		_waitTime = _random.NextFloat(m_typingSpeedMinMax.x, m_typingSpeedMinMax.y);
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void SliceUpdate()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		int maxVisibleCharacters = m_textMesh.maxVisibleCharacters;
		if (maxVisibleCharacters >= 0 && maxVisibleCharacters < _charCount && !(_timeOfLastTypedChar + _waitTime > realtimeSinceStartup))
		{
			maxVisibleCharacters = (m_textMesh.maxVisibleCharacters = maxVisibleCharacters + 1);
			_timeOfLastTypedChar = realtimeSinceStartup;
			if (_has_beginEntrySoundBank && maxVisibleCharacters == 1)
			{
				m_beginEntrySoundBank.Play();
			}
			else if (_has_typingSoundBank)
			{
				m_typingSoundBank.Play();
			}
			_waitTime = _random.NextFloat(m_typingSpeedMinMax.x, m_typingSpeedMinMax.y);
		}
	}

	public void SetText(string text, IList<int> entryIndexes, int nonRichTextTagsCharCount)
	{
		_charCount = nonRichTextTagsCharCount;
		m_textMesh.SetText(text);
		m_textMesh.maxVisibleCharacters = 0;
		_SetEntryIndexes(entryIndexes);
	}

	public void SetText(string text, IList<int> entryIndexes)
	{
		SetText(text, entryIndexes, text.Length);
		m_textMesh.SetText(text);
		m_textMesh.maxVisibleCharacters = 0;
		_SetEntryIndexes(entryIndexes);
	}

	public void SetText(string text)
	{
		SetText(text, Array.Empty<int>());
	}

	public void SetText(Utf16ValueStringBuilder zStringBuilder, IList<int> entryIndexes, int nonRichTextTagsCharCount)
	{
		_charCount = nonRichTextTagsCharCount;
		m_textMesh.SetTextToZString(zStringBuilder);
		m_textMesh.maxVisibleCharacters = 0;
		_SetEntryIndexes(entryIndexes);
	}

	public void SetText(Utf16ValueStringBuilder zStringBuilder)
	{
		SetText(zStringBuilder, Array.Empty<int>(), zStringBuilder.Length);
	}

	private void _SetEntryIndexes(IList<int> entryIndexes)
	{
		_entryIndexes.Clear();
		_entryIndexes.AddRange(entryIndexes);
	}

	public void UpdateText(Utf16ValueStringBuilder zStringBuilder, int nonRichTextTagsCharCount)
	{
		m_textMesh.maxVisibleCharacters = (_charCount = nonRichTextTagsCharCount);
		m_textMesh.SetTextToZString(zStringBuilder);
	}
}
