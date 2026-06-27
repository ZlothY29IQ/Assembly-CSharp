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

	private readonly List<string> typeKeysCache = new List<string>();

	private bool typeKeysDirty;

	private int groupCursor;

	internal static readonly List<string> SharedKeysCache = new List<string>();

	public static CosmeticsProximityReactorManager Instance => _instance;

	public IReadOnlyList<CosmeticsProximityReactor> Cosmetics => cosmetics;

	public static event Action<CosmeticsProximityReactor> OnCosmeticRegistered;

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
			CosmeticsProximityReactorManager.OnCosmeticRegistered?.Invoke(cosmetic);
		}
		IReadOnlyList<string> types = cosmetic.GetTypes();
		for (int i = 0; i < types.Count; i++)
		{
			string text = types[i];
			if (!string.IsNullOrEmpty(text))
			{
				if (!byType.TryGetValue(text, out var value))
				{
					value = new List<CosmeticsProximityReactor>();
					byType[text] = value;
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
				for (int i = 0; i < typeKeysCache.Count; i++)
				{
					string key = typeKeysCache[i];
					if (byType.TryGetValue(key, out var value) && value != null && value.Count > 0)
					{
						ProcessOneGroup(value);
					}
				}
			}
		}
		if (gorillaBodyPart.Count > 0)
		{
			for (int j = 0; j < cosmetics.Count; j++)
			{
				CosmeticsProximityReactor cosmeticsProximityReactor = cosmetics[j];
				if (cosmeticsProximityReactor == null)
				{
					continue;
				}
				if (!cosmeticsProximityReactor.AcceptsAnySource())
				{
					cosmeticsProximityReactor.OnSourceAboveAll();
					continue;
				}
				bool flag = false;
				Vector3 contact = default(Vector3);
				for (int k = 0; k < gorillaBodyPart.Count; k++)
				{
					CosmeticsProximityReactor cosmeticsProximityReactor2 = gorillaBodyPart[k];
					if (!(cosmeticsProximityReactor2 == null) && cosmeticsProximityReactor.AcceptsThisSource(cosmeticsProximityReactor2.gorillaBodyParts))
					{
						bool any;
						float sourceThresholdFor = cosmeticsProximityReactor.GetSourceThresholdFor(cosmeticsProximityReactor2, out any);
						if (any && AreCollidersWithinThreshold(cosmeticsProximityReactor2, cosmeticsProximityReactor, sourceThresholdFor, out var contactPoint))
						{
							cosmeticsProximityReactor.OnSourceBelow(contactPoint, cosmeticsProximityReactor2.gorillaBodyParts, cosmeticsProximityReactor2.GetComponentInParent<VRRig>());
							contact = contactPoint;
							flag = true;
						}
					}
				}
				if (flag)
				{
					cosmeticsProximityReactor.WhileSourceBelow(contact, CosmeticsProximityReactor.GorillaBodyPart.HandLeft | CosmeticsProximityReactor.GorillaBodyPart.HandRight | CosmeticsProximityReactor.GorillaBodyPart.Mouth, (gorillaBodyPart[0] != null) ? gorillaBodyPart[0].GetComponentInParent<VRRig>() : null);
				}
				else
				{
					cosmeticsProximityReactor.OnSourceAboveAll();
				}
			}
		}
		if (typeKeysDirty)
		{
			RebuildTypeKeysCache();
		}
		for (int l = 0; l < typeKeysCache.Count; l++)
		{
			string key2 = typeKeysCache[l];
			if (byType.TryGetValue(key2, out var value2) && value2 != null && value2.Count > 0)
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
				if (!(any || any2))
				{
					continue;
				}
				float num = float.MaxValue;
				if (any && cosmeticPairThresholdWith < num)
				{
					num = cosmeticPairThresholdWith;
				}
				if (any2 && cosmeticPairThresholdWith2 < num)
				{
					num = cosmeticPairThresholdWith2;
				}
				if (AreCollidersWithinThreshold(cosmeticsProximityReactor, cosmeticsProximityReactor2, num, out var contactPoint))
				{
					cosmeticsProximityReactor.OnCosmeticBelowWith(cosmeticsProximityReactor2, contactPoint);
					cosmeticsProximityReactor2.OnCosmeticBelowWith(cosmeticsProximityReactor, contactPoint);
					if (cosmeticsProximityReactor.IsBelow)
					{
						cosmeticsProximityReactor.RefreshAggregateMatched();
						matchedFrame[cosmeticsProximityReactor] = Time.frameCount;
						result = true;
					}
					if (cosmeticsProximityReactor2.IsBelow)
					{
						cosmeticsProximityReactor2.RefreshAggregateMatched();
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
		for (int i = 0; i < group.Count; i++)
		{
			CosmeticsProximityReactor cosmeticsProximityReactor = group[i];
			if (!(cosmeticsProximityReactor == null) && cosmeticsProximityReactor.HasAnyCosmeticMatch() && (!matchedFrame.TryGetValue(cosmeticsProximityReactor, out var value) || value != Time.frameCount))
			{
				if (TryFindAnyCosmeticPartner(cosmeticsProximityReactor, out var partner, out var contact))
				{
					cosmeticsProximityReactor.WhileCosmeticBelowWith(partner, contact);
					partner.WhileCosmeticBelowWith(cosmeticsProximityReactor, contact);
				}
				else
				{
					cosmeticsProximityReactor.OnCosmeticAboveAll();
				}
			}
		}
	}

	private bool TryFindAnyCosmeticPartner(CosmeticsProximityReactor a, out CosmeticsProximityReactor partner, out Vector3 contact)
	{
		partner = null;
		contact = default(Vector3);
		IReadOnlyList<string> types = a.GetTypes();
		for (int i = 0; i < types.Count; i++)
		{
			string text = types[i];
			if (string.IsNullOrEmpty(text) || !byType.TryGetValue(text, out var value) || value == null)
			{
				continue;
			}
			for (int j = 0; j < value.Count; j++)
			{
				CosmeticsProximityReactor cosmeticsProximityReactor = value[j];
				if (cosmeticsProximityReactor == null || cosmeticsProximityReactor == a || ShouldSkipSameIdPair(a, cosmeticsProximityReactor))
				{
					continue;
				}
				bool any;
				float cosmeticPairThresholdWith = a.GetCosmeticPairThresholdWith(cosmeticsProximityReactor, out any);
				if (any)
				{
					float threshold = cosmeticPairThresholdWith;
					if (AreCollidersWithinThreshold(a, cosmeticsProximityReactor, threshold, out var contactPoint))
					{
						partner = cosmeticsProximityReactor;
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
