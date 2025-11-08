using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BuilderSetSelector : MonoBehaviour
{
	private List<BuilderPieceSet.BuilderDisplayGroup> includedGroups;

	private int numLiveDisplayGroups;

	[SerializeField]
	private Material disabledMaterial;

	[Header("UI")]
	[FormerlySerializedAs("setLabels")]
	[SerializeField]
	private Text[] groupLabels;

	[Header("Buttons")]
	[FormerlySerializedAs("setButtons")]
	[SerializeField]
	private GorillaPressableButton[] groupButtons;

	[SerializeField]
	private GorillaPressableButton previousPageButton;

	[SerializeField]
	private GorillaPressableButton nextPageButton;

	private List<BuilderPieceSet.BuilderPieceCategory> _includedCategories;

	private int includedGroupIndex;

	private BuilderPieceSet.BuilderDisplayGroup currentGroup;

	private int pageIndex;

	private int groupsPerPage = 3;

	private int totalPages = 1;

	private List<Renderer> zoneRenderers = new List<Renderer>(10);

	private bool inBuilderZone;

	[HideInInspector]
	public UnityEvent<int> OnSelectedGroup;

	private void Start()
	{
		zoneRenderers.Clear();
		GorillaPressableButton[] array = groupButtons;
		foreach (GorillaPressableButton gorillaPressableButton in array)
		{
			zoneRenderers.Add(gorillaPressableButton.buttonRenderer);
			Renderer renderer = gorillaPressableButton.myTmpText?.GetComponent<Renderer>();
			if (renderer != null)
			{
				zoneRenderers.Add(renderer);
			}
		}
		zoneRenderers.Add(previousPageButton.buttonRenderer);
		zoneRenderers.Add(nextPageButton.buttonRenderer);
		Renderer renderer2 = previousPageButton.myTmpText?.GetComponent<Renderer>();
		if (renderer2 != null)
		{
			zoneRenderers.Add(renderer2);
		}
		renderer2 = nextPageButton.myTmpText?.GetComponent<Renderer>();
		if (renderer2 != null)
		{
			zoneRenderers.Add(renderer2);
		}
		foreach (Renderer zoneRenderer in zoneRenderers)
		{
			zoneRenderer.enabled = false;
		}
		inBuilderZone = false;
		ZoneManagement instance = ZoneManagement.instance;
		instance.onZoneChanged = (Action)Delegate.Combine(instance.onZoneChanged, new Action(OnZoneChanged));
		OnZoneChanged();
	}

	public void Setup(List<BuilderPieceSet.BuilderPieceCategory> categories)
	{
		List<BuilderPieceSet.BuilderDisplayGroup> liveDisplayGroups = BuilderSetManager.instance.GetLiveDisplayGroups();
		numLiveDisplayGroups = liveDisplayGroups.Count;
		includedGroups = new List<BuilderPieceSet.BuilderDisplayGroup>(liveDisplayGroups.Count);
		_includedCategories = categories;
		foreach (BuilderPieceSet.BuilderDisplayGroup item in liveDisplayGroups)
		{
			if (DoesDisplayGroupHaveIncludedCategories(item))
			{
				includedGroups.Add(item);
			}
		}
		BuilderSetManager.instance.OnOwnedSetsUpdated.AddListener(RefreshUnlockedGroups);
		BuilderSetManager.instance.OnLiveSetsUpdated.AddListener(RefreshUnlockedGroups);
		groupsPerPage = groupButtons.Length;
		totalPages = includedGroups.Count / groupsPerPage;
		if (includedGroups.Count % groupsPerPage > 0)
		{
			totalPages++;
		}
		previousPageButton.gameObject.SetActive(totalPages > 1);
		nextPageButton.gameObject.SetActive(totalPages > 1);
		previousPageButton.myTmpText.enabled = totalPages > 1;
		nextPageButton.myTmpText.enabled = totalPages > 1;
		pageIndex = 0;
		currentGroup = includedGroups[includedGroupIndex];
		previousPageButton.onPressButton.AddListener(OnPreviousPageClicked);
		nextPageButton.onPressButton.AddListener(OnNextPageClicked);
		GorillaPressableButton[] array = groupButtons;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].onPressed += OnSetButtonPressed;
		}
		UpdateLabels();
	}

	private void OnDestroy()
	{
		if (previousPageButton != null)
		{
			previousPageButton.onPressButton.RemoveListener(OnPreviousPageClicked);
		}
		if (nextPageButton != null)
		{
			nextPageButton.onPressButton.RemoveListener(OnNextPageClicked);
		}
		if (BuilderSetManager.instance != null)
		{
			BuilderSetManager.instance.OnOwnedSetsUpdated.RemoveListener(RefreshUnlockedGroups);
			BuilderSetManager.instance.OnLiveSetsUpdated.RemoveListener(RefreshUnlockedGroups);
		}
		GorillaPressableButton[] array = groupButtons;
		foreach (GorillaPressableButton gorillaPressableButton in array)
		{
			if (!(gorillaPressableButton == null))
			{
				gorillaPressableButton.onPressed -= OnSetButtonPressed;
			}
		}
		if (ZoneManagement.instance != null)
		{
			ZoneManagement instance = ZoneManagement.instance;
			instance.onZoneChanged = (Action)Delegate.Remove(instance.onZoneChanged, new Action(OnZoneChanged));
		}
	}

	private void OnZoneChanged()
	{
		bool flag = ZoneManagement.instance.IsZoneActive(GTZone.monkeBlocks);
		if (flag && !inBuilderZone)
		{
			foreach (Renderer zoneRenderer in zoneRenderers)
			{
				zoneRenderer.enabled = true;
			}
		}
		else if (!flag && inBuilderZone)
		{
			foreach (Renderer zoneRenderer2 in zoneRenderers)
			{
				zoneRenderer2.enabled = false;
			}
		}
		inBuilderZone = flag;
	}

	private void OnSetButtonPressed(GorillaPressableButton button, bool isLeft)
	{
		int num = 0;
		for (int i = 0; i < groupButtons.Length; i++)
		{
			if (button.Equals(groupButtons[i]))
			{
				num = i;
				break;
			}
		}
		int num2 = pageIndex * groupsPerPage + num;
		if (num2 < includedGroups.Count)
		{
			BuilderPieceSet.BuilderDisplayGroup builderDisplayGroup = includedGroups[num2];
			if (currentGroup == null || builderDisplayGroup.displayName != currentGroup.displayName)
			{
				OnSelectedGroup?.Invoke(builderDisplayGroup.GetDisplayGroupIdentifier());
			}
		}
	}

	private void RefreshUnlockedGroups()
	{
		List<BuilderPieceSet.BuilderDisplayGroup> liveDisplayGroups = BuilderSetManager.instance.GetLiveDisplayGroups();
		if (liveDisplayGroups.Count != numLiveDisplayGroups)
		{
			string value = ((currentGroup != null) ? currentGroup.displayName : "");
			numLiveDisplayGroups = liveDisplayGroups.Count;
			includedGroups.EnsureCapacity(numLiveDisplayGroups);
			includedGroups.Clear();
			int num = 0;
			foreach (BuilderPieceSet.BuilderDisplayGroup item in liveDisplayGroups)
			{
				if (DoesDisplayGroupHaveIncludedCategories(item))
				{
					if (item.displayName.Equals(value))
					{
						num = includedGroups.Count;
					}
					includedGroups.Add(item);
				}
			}
			if (includedGroups.Count < 1)
			{
				currentGroup = null;
			}
			else
			{
				includedGroupIndex = num;
				currentGroup = includedGroups[includedGroupIndex];
			}
			totalPages = includedGroups.Count / groupsPerPage;
			if (includedGroups.Count % groupsPerPage > 0)
			{
				totalPages++;
			}
			previousPageButton.gameObject.SetActive(totalPages > 1);
			nextPageButton.gameObject.SetActive(totalPages > 1);
			previousPageButton.myTmpText.enabled = totalPages > 1;
			nextPageButton.myTmpText.enabled = totalPages > 1;
		}
		UpdateLabels();
	}

	private void OnPreviousPageClicked()
	{
		RefreshUnlockedGroups();
		int num = Mathf.Clamp(pageIndex - 1, 0, totalPages - 1);
		if (num != pageIndex)
		{
			pageIndex = num;
			UpdateLabels();
		}
	}

	private void OnNextPageClicked()
	{
		RefreshUnlockedGroups();
		int num = Mathf.Clamp(pageIndex + 1, 0, totalPages - 1);
		if (num != pageIndex)
		{
			pageIndex = num;
			UpdateLabels();
		}
	}

	public void SetSelection(int groupID)
	{
		if (BuilderSetManager.instance == null)
		{
			return;
		}
		BuilderPieceSet.BuilderDisplayGroup newGroup = BuilderSetManager.instance.GetDisplayGroupFromIndex(groupID);
		if (newGroup != null)
		{
			currentGroup = newGroup;
			includedGroupIndex = includedGroups.FindIndex((BuilderPieceSet.BuilderDisplayGroup x) => x.displayName == newGroup.displayName);
			UpdateLabels();
		}
	}

	private void UpdateLabels()
	{
		for (int i = 0; i < groupLabels.Length; i++)
		{
			int num = pageIndex * groupsPerPage + i;
			if (num < includedGroups.Count && includedGroups[num] != null)
			{
				if (!groupButtons[i].gameObject.activeSelf)
				{
					groupButtons[i].gameObject.SetActive(value: true);
					groupButtons[i].myTmpText.gameObject.SetActive(value: true);
				}
				if (groupButtons[i].myTmpText.text != includedGroups[num].displayName)
				{
					groupButtons[i].myTmpText.text = includedGroups[num].displayName;
				}
				if (BuilderSetManager.instance.IsPieceSetOwnedLocally(includedGroups[num].setID))
				{
					bool flag = currentGroup != null && includedGroups[num].displayName == currentGroup.displayName;
					if (flag != groupButtons[i].isOn || !groupButtons[i].enabled)
					{
						groupButtons[i].isOn = flag;
						groupButtons[i].buttonRenderer.material = (flag ? groupButtons[i].pressedMaterial : groupButtons[i].unpressedMaterial);
					}
					groupButtons[i].enabled = true;
				}
				else
				{
					if (groupButtons[i].enabled)
					{
						groupButtons[i].buttonRenderer.material = disabledMaterial;
					}
					groupButtons[i].enabled = false;
				}
			}
			else
			{
				if (groupButtons[i].gameObject.activeSelf)
				{
					groupButtons[i].gameObject.SetActive(value: false);
					groupButtons[i].myTmpText.gameObject.SetActive(value: false);
				}
				if (groupButtons[i].isOn || groupButtons[i].enabled)
				{
					groupButtons[i].isOn = false;
					groupButtons[i].enabled = false;
				}
			}
		}
		bool flag2 = pageIndex > 0 && totalPages > 1;
		bool flag3 = pageIndex < totalPages - 1 && totalPages > 1;
		if (previousPageButton.myTmpText.enabled != flag2)
		{
			previousPageButton.myTmpText.enabled = flag2;
		}
		if (nextPageButton.myTmpText.enabled != flag3)
		{
			nextPageButton.myTmpText.enabled = flag3;
		}
	}

	public bool DoesDisplayGroupHaveIncludedCategories(BuilderPieceSet.BuilderDisplayGroup set)
	{
		foreach (BuilderPieceSet.BuilderPieceSubset pieceSubset in set.pieceSubsets)
		{
			if (_includedCategories.Contains(pieceSubset.pieceCategory))
			{
				return true;
			}
		}
		return false;
	}

	public BuilderPieceSet.BuilderDisplayGroup GetSelectedGroup()
	{
		return currentGroup;
	}

	public int GetDefaultGroupID()
	{
		if (includedGroups == null || includedGroups.Count < 1)
		{
			return -1;
		}
		BuilderPieceSet.BuilderDisplayGroup builderDisplayGroup = includedGroups[0];
		if (!BuilderSetManager.instance.IsPieceSetOwnedLocally(builderDisplayGroup.setID))
		{
			foreach (BuilderPieceSet.BuilderDisplayGroup includedGroup in includedGroups)
			{
				if (BuilderSetManager.instance.IsPieceSetOwnedLocally(includedGroup.setID))
				{
					return includedGroup.GetDisplayGroupIdentifier();
				}
			}
			Debug.LogWarning("No default group available for shelf");
			return -1;
		}
		return builderDisplayGroup.GetDisplayGroupIdentifier();
	}
}
