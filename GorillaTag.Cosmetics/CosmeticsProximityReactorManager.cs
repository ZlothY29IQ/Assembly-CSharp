using System;
using System.Collections.Generic;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class CosmeticsProximityReactorManager : MonoBehaviour, IGorillaSliceableSimple
{
	private static CosmeticsProximityReactorManager _instance;

	private readonly List<CosmeticsProximityReactor> cosmetics = new List<CosmeticsProximityReactor>();

	private readonly List<CosmeticsProximityReactor> gorillaBodyPart = new List<CosmeticsProximityReactor>();

	private readonly Dictionary<string, List<CosmeticsProximityReactor>> byType = new Dictionary<string, List<CosmeticsProximityReactor>>(StringComparer.Ordinal);

	private readonly Dictionary<CosmeticsProximityReactor, int> matchedFrame = new Dictionary<CosmeticsProximityReactor, int>();

	[Tooltip("Perf - How many cosmetic groups should we fully process per frame (slice)")]
	[SerializeField]
	private int groupsPerSlice = 1;

	private readonly List<string> typeKeysCache = new List<string>();

	private bool typeKeysDirty;

	private int groupCursor;

	internal static readonly List<string> SharedKeysCache = new List<string>();

	public static CosmeticsProximityReactorManager Instance => _instance;

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		else
		{
			_instance = this;
		}
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		if (_instance == this)
		{
			_instance = null;
		}
	}

	public void Register(CosmeticsProximityReactor cosmetic)
	{
		if (cosmetic == null)
		{
			return;
		}
		if (cosmetic.IsGorillaBody())
		{
			if (!gorillaBodyPart.Contains(cosmetic))
			{
				gorillaBodyPart.Add(cosmetic);
			}
			return;
		}
		if (!cosmetics.Contains(cosmetic))
		{
			cosmetics.Add(cosmetic);
		}
		foreach (string type in cosmetic.GetTypes())
		{
			if (!string.IsNullOrEmpty(type))
			{
				if (!byType.TryGetValue(type, out var value))
				{
					value = new List<CosmeticsProximityReactor>();
					byType[type] = value;
				}
				if (!value.Contains(cosmetic))
				{
					value.Add(cosmetic);
					typeKeysDirty = true;
				}
			}
		}
	}

	public void Unregister(CosmeticsProximityReactor cosmetic)
	{
		if (cosmetic == null)
		{
			return;
		}
		cosmetics.Remove(cosmetic);
		gorillaBodyPart.Remove(cosmetic);
		matchedFrame.Remove(cosmetic);
		foreach (KeyValuePair<string, List<CosmeticsProximityReactor>> item in byType)
		{
			if (item.Value.Remove(cosmetic))
			{
				typeKeysDirty = true;
			}
		}
	}

	public void SliceUpdate()
	{
		if (cosmetics.Count == 0)
		{
			return;
		}
		if (AnyGroupHasTwo())
		{
			if (typeKeysDirty)
			{
				RebuildTypeKeysCache();
			}
			if (typeKeysCache.Count > 0)
			{
				for (int i = 0; i < groupsPerSlice; i++)
				{
					if (typeKeysCache.Count <= 0)
					{
						break;
					}
					if (groupCursor >= typeKeysCache.Count)
					{
						groupCursor = 0;
					}
					string key = typeKeysCache[groupCursor];
					if (byType.TryGetValue(key, out var value) && value != null && value.Count > 0)
					{
						ProcessOneGroup(value);
					}
					groupCursor++;
				}
			}
		}
		if (gorillaBodyPart.Count > 0)
		{
			foreach (CosmeticsProximityReactor cosmetic in cosmetics)
			{
				if (cosmetic == null)
				{
					continue;
				}
				if (!cosmetic.AcceptsAnySource())
				{
					cosmetic.OnSourceAboveAll();
					continue;
				}
				bool flag = false;
				Vector3 contact = default(Vector3);
				foreach (CosmeticsProximityReactor item in gorillaBodyPart)
				{
					if (!(item == null) && cosmetic.AcceptsThisSource(item.gorillaBodyParts))
					{
						bool any;
						float sourceThresholdFor = cosmetic.GetSourceThresholdFor(item, out any);
						if (any && AreCollidersWithinThreshold(item, cosmetic, sourceThresholdFor, out var contactPoint))
						{
							cosmetic.OnSourceBelow(contactPoint, item.gorillaBodyParts);
							contact = contactPoint;
							flag = true;
						}
					}
				}
				if (flag)
				{
					cosmetic.WhileSourceBelow(contact, CosmeticsProximityReactor.GorillaBodyPart.HandLeft | CosmeticsProximityReactor.GorillaBodyPart.HandRight | CosmeticsProximityReactor.GorillaBodyPart.Mouth);
				}
				else
				{
					cosmetic.OnSourceAboveAll();
				}
			}
		}
		if (typeKeysDirty)
		{
			RebuildTypeKeysCache();
		}
		foreach (string item2 in typeKeysCache)
		{
			if (byType.TryGetValue(item2, out var value2) && value2 != null && value2.Count > 0)
			{
				BreakTheBoundForGroup(value2);
			}
		}
	}

	private void ProcessOneGroup(List<CosmeticsProximityReactor> group)
	{
		if (!CheckProximity(group))
		{
			BreakTheBoundForGroup(group);
		}
	}

	private bool CheckProximity(List<CosmeticsProximityReactor> group)
	{
		bool result = false;
		for (int i = 0; i < group.Count; i++)
		{
			CosmeticsProximityReactor cosmeticsProximityReactor = group[i];
			if (cosmeticsProximityReactor == null)
			{
				continue;
			}
			for (int j = i + 1; j < group.Count; j++)
			{
				CosmeticsProximityReactor cosmeticsProximityReactor2 = group[j];
				if (cosmeticsProximityReactor2 == null || ShouldSkipSameIdPair(cosmeticsProximityReactor, cosmeticsProximityReactor2))
				{
					continue;
				}
				bool any;
				float cosmeticPairThresholdWith = cosmeticsProximityReactor.GetCosmeticPairThresholdWith(cosmeticsProximityReactor2, out any);
				bool any2;
				float cosmeticPairThresholdWith2 = cosmeticsProximityReactor2.GetCosmeticPairThresholdWith(cosmeticsProximityReactor, out any2);
				if (!(any && any2))
				{
					continue;
				}
				float threshold = Mathf.Min(cosmeticPairThresholdWith, cosmeticPairThresholdWith2);
				if (AreCollidersWithinThreshold(cosmeticsProximityReactor, cosmeticsProximityReactor2, threshold, out var contactPoint))
				{
					cosmeticsProximityReactor.OnCosmeticBelowWith(cosmeticsProximityReactor2, contactPoint);
					cosmeticsProximityReactor2.OnCosmeticBelowWith(cosmeticsProximityReactor, contactPoint);
					if (cosmeticsProximityReactor.IsBelow && cosmeticsProximityReactor2.IsBelow)
					{
						cosmeticsProximityReactor.RefreshAggregateMatched();
						cosmeticsProximityReactor2.RefreshAggregateMatched();
						matchedFrame[cosmeticsProximityReactor] = Time.frameCount;
						matchedFrame[cosmeticsProximityReactor2] = Time.frameCount;
						result = true;
					}
				}
			}
		}
		return result;
	}

	private void BreakTheBoundForGroup(List<CosmeticsProximityReactor> group)
	{
		foreach (CosmeticsProximityReactor item in group)
		{
			if (!(item == null) && item.HasAnyCosmeticMatch() && (!matchedFrame.TryGetValue(item, out var value) || value != Time.frameCount))
			{
				if (TryFindAnyCosmeticPartner(item, out var partner, out var contact))
				{
					item.WhileCosmeticBelowWith(partner, contact);
					partner.WhileCosmeticBelowWith(item, contact);
				}
				else
				{
					item.OnCosmeticAboveAll();
				}
			}
		}
	}

	private bool TryFindAnyCosmeticPartner(CosmeticsProximityReactor a, out CosmeticsProximityReactor partner, out Vector3 contact)
	{
		partner = null;
		contact = default(Vector3);
		foreach (string type in a.GetTypes())
		{
			if (string.IsNullOrEmpty(type) || !byType.TryGetValue(type, out var value) || value == null)
			{
				continue;
			}
			foreach (CosmeticsProximityReactor item in value)
			{
				if (item == null || item == a || ShouldSkipSameIdPair(a, item))
				{
					continue;
				}
				bool any;
				float cosmeticPairThresholdWith = a.GetCosmeticPairThresholdWith(item, out any);
				bool any2;
				float cosmeticPairThresholdWith2 = item.GetCosmeticPairThresholdWith(a, out any2);
				if (any && any2)
				{
					float threshold = Mathf.Min(cosmeticPairThresholdWith, cosmeticPairThresholdWith2);
					if (AreCollidersWithinThreshold(a, item, threshold, out var contactPoint))
					{
						partner = item;
						contact = contactPoint;
						return true;
					}
				}
			}
		}
		return false;
	}

	private static bool ShouldSkipSameIdPair(CosmeticsProximityReactor a, CosmeticsProximityReactor b)
	{
		if (!a.ignoreSameCosmeticInstances && !b.ignoreSameCosmeticInstances)
		{
			return false;
		}
		if (string.IsNullOrEmpty(a.PlayFabID) || string.IsNullOrEmpty(b.PlayFabID))
		{
			return false;
		}
		return string.Equals(a.PlayFabID, b.PlayFabID, StringComparison.Ordinal);
	}

	private static bool AreCollidersWithinThreshold(CosmeticsProximityReactor a, CosmeticsProximityReactor b, float threshold, out Vector3 contactPoint)
	{
		Vector3 vector = ((b.collider == null) ? b.transform.position : b.collider.ClosestPoint(a.transform.position));
		Vector3 vector2 = ((a.collider == null) ? a.transform.position : a.collider.ClosestPoint(vector));
		contactPoint = (vector2 + vector) * 0.5f;
		return Vector3.Distance(vector2, vector) <= threshold;
	}

	private bool AnyGroupHasTwo()
	{
		foreach (KeyValuePair<string, List<CosmeticsProximityReactor>> item in byType)
		{
			List<CosmeticsProximityReactor> value = item.Value;
			if (value != null && value.Count >= 2)
			{
				return true;
			}
		}
		return false;
	}

	private void RebuildTypeKeysCache()
	{
		typeKeysCache.Clear();
		foreach (KeyValuePair<string, List<CosmeticsProximityReactor>> item in byType)
		{
			List<CosmeticsProximityReactor> value = item.Value;
			if (value != null && value.Count > 0)
			{
				typeKeysCache.Add(item.Key);
			}
		}
		typeKeysDirty = false;
		if (groupCursor >= typeKeysCache.Count)
		{
			groupCursor = 0;
		}
	}
}
