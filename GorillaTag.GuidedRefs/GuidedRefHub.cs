using System;
using System.Collections.Generic;
using GorillaExtensions;
using GorillaTag.GuidedRefs.Internal;
using UnityEngine;

namespace GorillaTag.GuidedRefs;

[DefaultExecutionOrder(int.MinValue)]
public class GuidedRefHub : MonoBehaviour, IGuidedRefMonoBehaviour, IGuidedRefObject
{
	[SerializeField]
	private bool isRootInstance;

	public GuidedRefHubIdSO hubId;

	[NonSerialized]
	[OnEnterPlay_SetNull]
	public static GuidedRefHub rootInstance;

	[NonSerialized]
	[OnEnterPlay_Set(false)]
	public static bool hasRootInstance;

	[DebugReadout]
	private readonly Dictionary<GuidedRefTargetIdSO, RelayInfo> lookupRelayInfoByTargetId = new Dictionary<GuidedRefTargetIdSO, RelayInfo>(256);

	private static readonly Dictionary<RelayInfo, GuidedRefTargetIdSO> static_relayInfo_to_targetId = new Dictionary<RelayInfo, GuidedRefTargetIdSO>(256);

	[OnEnterPlay_Clear]
	private static readonly Dictionary<int, List<GuidedRefHub>> globalLookupHubsThatHaveRegisteredInstId = new Dictionary<int, List<GuidedRefHub>>(256);

	[OnEnterPlay_Clear]
	private static readonly Dictionary<GuidedRefHub, List<int>> globalLookupRefInstIDsByHub = new Dictionary<GuidedRefHub, List<int>>(256);

	[OnEnterPlay_Clear]
	private static readonly List<GuidedRefHub> globalHubsTransientList = new List<GuidedRefHub>(32);

	private const string kUnsuppliedCallerName = "UNSUPPLIED_CALLER_NAME";

	[DebugReadout]
	[OnEnterPlay_Clear]
	internal static readonly HashSet<IGuidedRefReceiverMono> kReceiversWaitingToFullyResolve = new HashSet<IGuidedRefReceiverMono>(256);

	[DebugReadout]
	[OnEnterPlay_Clear]
	internal static readonly HashSet<IGuidedRefReceiverMono> kReceiversFullyRegistered = new HashSet<IGuidedRefReceiverMono>(256);

	protected void Awake()
	{
		GuidedRefInitialize();
	}

	protected void OnDestroy()
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		if (isRootInstance)
		{
			hasRootInstance = false;
			rootInstance = null;
		}
		if (!globalLookupRefInstIDsByHub.TryGetValue(this, out var value))
		{
			return;
		}
		foreach (int item in value)
		{
			globalLookupHubsThatHaveRegisteredInstId[item].Remove(this);
		}
		globalLookupRefInstIDsByHub.Remove(this);
	}

	public void GuidedRefInitialize()
	{
		if (isRootInstance)
		{
			if (hasRootInstance)
			{
				Debug.LogError("GuidedRefHub: Attempted to assign global instance when one was already assigned:\n- This path: " + base.transform.GetPath() + "\n- Global instance: " + rootInstance.transform.GetPath() + "\n", this);
				UnityEngine.Object.Destroy(this);
				return;
			}
			hasRootInstance = true;
			rootInstance = this;
		}
		globalLookupRefInstIDsByHub[this] = new List<int>(2);
	}

	public static bool IsInstanceIDRegisteredWithAnyHub(int instanceID)
	{
		return globalLookupHubsThatHaveRegisteredInstId.ContainsKey(instanceID);
	}

	private void RegisterTarget_Internal<TIGuidedRefTargetMono>(TIGuidedRefTargetMono targetMono) where TIGuidedRefTargetMono : IGuidedRefTargetMono
	{
		RelayInfo orAddRelayInfoByTargetId = GetOrAddRelayInfoByTargetId(targetMono.GRefTargetInfo.targetId);
		if (orAddRelayInfoByTargetId == null)
		{
			return;
		}
		IGuidedRefTargetMono guidedRefTargetMono = targetMono;
		if (orAddRelayInfoByTargetId.targetMono != null && orAddRelayInfoByTargetId.targetMono != guidedRefTargetMono)
		{
			if (!targetMono.GRefTargetInfo.hackIgnoreDuplicateRegistration)
			{
				Debug.LogError("GuidedRefHub: Multiple targets registering with the same Hub. Maybe look at the HubID you are using:- hub=\"" + base.transform.GetPath() + "\"\n- target1=\"" + orAddRelayInfoByTargetId.targetMono.transform.GetPath() + "\",\n- target2=\"" + targetMono.transform.GetPath() + "\"", this);
			}
			return;
		}
		int instanceID = targetMono.GetInstanceID();
		GetHubsThatHaveRegisteredInstId(instanceID).Add(this);
		if (!globalLookupRefInstIDsByHub.TryGetValue(this, out var value))
		{
			Debug.LogError("GuidedRefHub: It appears hub was not registered before `RegisterTarget` was called on it: - hub: \"" + base.transform.GetPath() + "\"\n- target: \"" + targetMono.transform.GetPath() + "\"", this);
		}
		else
		{
			value.Add(instanceID);
			orAddRelayInfoByTargetId.targetMono = targetMono;
			ResolveReferences(orAddRelayInfoByTargetId);
		}
	}

	public static void RegisterTarget<TIGuidedRefTargetMono>(TIGuidedRefTargetMono targetMono, GuidedRefHubIdSO[] hubIds = null, Component debugCaller = null) where TIGuidedRefTargetMono : IGuidedRefTargetMono
	{
		if (targetMono == null)
		{
			string text = ((debugCaller == null) ? "UNSUPPLIED_CALLER_NAME" : debugCaller.name);
			Debug.LogError("GuidedRefHub: Cannot register null target from \"" + text + "\".", debugCaller);
		}
		else
		{
			if (targetMono.GRefTargetInfo.targetId == null)
			{
				return;
			}
			globalHubsTransientList.Clear();
			targetMono.transform.GetComponentsInParent(includeInactive: true, globalHubsTransientList);
			if (hasRootInstance)
			{
				globalHubsTransientList.Add(rootInstance);
			}
			bool flag = false;
			foreach (GuidedRefHub globalHubsTransient in globalHubsTransientList)
			{
				if (hubIds == null || hubIds.Length <= 0 || Array.IndexOf(hubIds, globalHubsTransient.hubId) != -1)
				{
					flag = true;
					globalHubsTransient.RegisterTarget_Internal(targetMono);
				}
			}
			if (!flag && Application.isPlaying)
			{
				Debug.LogError("GuidedRefHub: Could not find hub for target: \"" + targetMono.transform.GetPath() + "\"", targetMono.transform);
			}
		}
	}

	public static void UnregisterTarget<TIGuidedRefTargetMono>(TIGuidedRefTargetMono targetMono, bool destroyed = true) where TIGuidedRefTargetMono : IGuidedRefTargetMono
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		if (targetMono == null)
		{
			Debug.LogError("GuidedRefHub: Cannot unregister null target.");
			return;
		}
		int instanceID = targetMono.GetInstanceID();
		if (!globalLookupHubsThatHaveRegisteredInstId.TryGetValue(instanceID, out var value))
		{
			return;
		}
		foreach (GuidedRefHub item in value)
		{
			if (!item.lookupRelayInfoByTargetId.TryGetValue(targetMono.GRefTargetInfo.targetId, out var value2))
			{
				continue;
			}
			foreach (RegisteredReceiverFieldInfo registeredField in value2.registeredFields)
			{
				if (registeredField.receiverMono != null)
				{
					registeredField.receiverMono.OnGuidedRefTargetDestroyed(registeredField.fieldId);
					kReceiversWaitingToFullyResolve.Remove(registeredField.receiverMono);
					value2.resolvedFields.Remove(registeredField);
					value2.registeredFields.Add(registeredField);
					registeredField.receiverMono.GuidedRefsWaitingToResolveCount++;
				}
			}
		}
		foreach (GuidedRefHub item2 in value)
		{
			if (item2.lookupRelayInfoByTargetId.TryGetValue(targetMono.GRefTargetInfo.targetId, out var value3))
			{
				value3.targetMono = null;
			}
			globalLookupRefInstIDsByHub[item2].Remove(instanceID);
		}
		globalLookupHubsThatHaveRegisteredInstId.Remove(instanceID);
	}

	public static void ReceiverFullyRegistered<TIGuidedRefReceiverMono>(TIGuidedRefReceiverMono receiverMono) where TIGuidedRefReceiverMono : IGuidedRefReceiverMono
	{
		kReceiversFullyRegistered.Add(receiverMono);
		kReceiversWaitingToFullyResolve.Add(receiverMono);
		CheckAndNotifyIfReceiverFullyResolved(receiverMono);
	}

	private static void CheckAndNotifyIfReceiverFullyResolved<TIGuidedRefReceiverMono>(TIGuidedRefReceiverMono receiverMono) where TIGuidedRefReceiverMono : IGuidedRefReceiverMono
	{
		if (receiverMono.GuidedRefsWaitingToResolveCount == 0 && kReceiversFullyRegistered.Contains(receiverMono))
		{
			kReceiversWaitingToFullyResolve.Remove(receiverMono);
			receiverMono.OnAllGuidedRefsResolved();
		}
	}

	private void RegisterReceiverField(RegisteredReceiverFieldInfo registeredReceiverFieldInfo, GuidedRefTargetIdSO targetId)
	{
		globalLookupRefInstIDsByHub[this].Add(registeredReceiverFieldInfo.receiverMono.GetInstanceID());
		GetHubsThatHaveRegisteredInstId(registeredReceiverFieldInfo.receiverMono.GetInstanceID()).Add(this);
		RelayInfo orAddRelayInfoByTargetId = GetOrAddRelayInfoByTargetId(targetId);
		orAddRelayInfoByTargetId.registeredFields.Add(registeredReceiverFieldInfo);
		ResolveReferences(orAddRelayInfoByTargetId);
	}

	private static void RegisterReceiverField_Internal<TIGuidedRefReceiverMono>(GuidedRefHubIdSO hubId, TIGuidedRefReceiverMono receiverMono, int fieldId, GuidedRefTargetIdSO targetId, int index) where TIGuidedRefReceiverMono : IGuidedRefReceiverMono
	{
		if (receiverMono == null)
		{
			Debug.LogError("GuidedRefHub: Cannot register null receiver.");
			return;
		}
		globalHubsTransientList.Clear();
		receiverMono.transform.GetComponentsInParent(includeInactive: true, globalHubsTransientList);
		if (hasRootInstance)
		{
			globalHubsTransientList.Add(rootInstance);
		}
		RegisteredReceiverFieldInfo registeredReceiverFieldInfo = new RegisteredReceiverFieldInfo
		{
			receiverMono = receiverMono,
			fieldId = fieldId,
			index = index
		};
		bool flag = false;
		foreach (GuidedRefHub globalHubsTransient in globalHubsTransientList)
		{
			if (!(hubId != null) || !(globalHubsTransient.hubId != hubId))
			{
				flag = true;
				globalHubsTransient.RegisterReceiverField(registeredReceiverFieldInfo, targetId);
				break;
			}
		}
		if (flag)
		{
			int guidedRefsWaitingToResolveCount = receiverMono.GuidedRefsWaitingToResolveCount;
			receiverMono.GuidedRefsWaitingToResolveCount = guidedRefsWaitingToResolveCount + 1;
		}
		else
		{
			Debug.LogError("Could not find matching GuidedRefHub to register with for receiver at: " + receiverMono.transform.GetPath(), receiverMono.transform);
		}
	}

	public static void RegisterReceiverField<TIGuidedRefReceiverMono>(TIGuidedRefReceiverMono receiverMono, string fieldIdName, ref GuidedRefReceiverFieldInfo fieldInfo) where TIGuidedRefReceiverMono : IGuidedRefReceiverMono
	{
		if (GRef.ShouldResolveNow(fieldInfo.resolveModes))
		{
			fieldInfo.fieldId = Shader.PropertyToID(fieldIdName);
			RegisterReceiverField_Internal(fieldInfo.hubId, receiverMono, fieldInfo.fieldId, fieldInfo.targetId, -1);
		}
	}

	public static void RegisterReceiverArray<TIGuidedRefReceiverMono, T>(TIGuidedRefReceiverMono receiverMono, string fieldIdName, ref T[] receiverArray, ref GuidedRefReceiverArrayInfo arrayInfo) where TIGuidedRefReceiverMono : IGuidedRefReceiverMono where T : UnityEngine.Object
	{
		if (GRef.ShouldResolveNow(arrayInfo.resolveModes))
		{
			if (receiverArray == null)
			{
				receiverArray = new T[arrayInfo.targets.Length];
			}
			else if (receiverArray.Length != arrayInfo.targets.Length)
			{
				Array.Resize(ref receiverArray, arrayInfo.targets.Length);
			}
			arrayInfo.fieldId = Shader.PropertyToID(fieldIdName);
			for (int i = 0; i < arrayInfo.targets.Length; i++)
			{
				RegisterReceiverField_Internal(arrayInfo.hubId, receiverMono, arrayInfo.fieldId, arrayInfo.targets[i], i);
			}
		}
	}

	public static void UnregisterReceiver<TIGuidedRefReceiverMono>(TIGuidedRefReceiverMono receiverMono) where TIGuidedRefReceiverMono : IGuidedRefReceiverMono
	{
		if (receiverMono == null)
		{
			Debug.LogError("GuidedRefHub: Cannot unregister null receiver.");
			return;
		}
		int instanceID = receiverMono.GetInstanceID();
		if (!globalLookupHubsThatHaveRegisteredInstId.TryGetValue(instanceID, out var value))
		{
			Debug.LogError("Tried to unregister a receiver before it was registered.");
			return;
		}
		IGuidedRefReceiverMono iReceiverMono = receiverMono;
		foreach (GuidedRefHub item in value)
		{
			foreach (RelayInfo value2 in item.lookupRelayInfoByTargetId.Values)
			{
				value2.registeredFields.RemoveAll((RegisteredReceiverFieldInfo fieldInfo) => fieldInfo.receiverMono == iReceiverMono);
			}
			globalLookupRefInstIDsByHub[item].Remove(instanceID);
		}
		globalLookupHubsThatHaveRegisteredInstId.Remove(instanceID);
		receiverMono.GuidedRefsWaitingToResolveCount = 0;
	}

	private RelayInfo GetOrAddRelayInfoByTargetId(GuidedRefTargetIdSO targetId)
	{
		if (targetId == null)
		{
			Debug.LogError("GetOrAddRelayInfoByTargetId cannot register null target id");
			return null;
		}
		if (!lookupRelayInfoByTargetId.TryGetValue(targetId, out var value))
		{
			value = new RelayInfo
			{
				targetMono = null,
				registeredFields = new List<RegisteredReceiverFieldInfo>(1),
				resolvedFields = new List<RegisteredReceiverFieldInfo>(1)
			};
			lookupRelayInfoByTargetId[targetId] = value;
			static_relayInfo_to_targetId[value] = targetId;
		}
		return value;
	}

	public static List<GuidedRefHub> GetHubsThatHaveRegisteredInstId(int instanceId)
	{
		if (!globalLookupHubsThatHaveRegisteredInstId.TryGetValue(instanceId, out var value))
		{
			value = new List<GuidedRefHub>(1);
			globalLookupHubsThatHaveRegisteredInstId[instanceId] = value;
		}
		return value;
	}

	private static void ResolveReferences(RelayInfo relayInfo)
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		if (relayInfo == null)
		{
			Debug.LogError("GuidedRefHub.ResolveReferences: (this should never happen) relayInfo is null.");
		}
		else if (relayInfo.registeredFields == null)
		{
			GuidedRefTargetIdSO guidedRefTargetIdSO = static_relayInfo_to_targetId[relayInfo];
			string text = ((guidedRefTargetIdSO != null) ? guidedRefTargetIdSO.name : "NULL");
			Debug.LogError("GuidedRefHub.ResolveReferences: (this should never happen) \"" + text + "\"relayInfo.registeredFields is null.");
		}
		else
		{
			if (relayInfo.targetMono == null)
			{
				return;
			}
			for (int num = relayInfo.registeredFields.Count - 1; num >= 0; num--)
			{
				RegisteredReceiverFieldInfo item = relayInfo.registeredFields[num];
				if (item.receiverMono.GuidedRefTryResolveReference(new GuidedRefTryResolveInfo
				{
					fieldId = item.fieldId,
					index = item.index,
					targetMono = relayInfo.targetMono
				}))
				{
					relayInfo.registeredFields.RemoveAt(num);
					CheckAndNotifyIfReceiverFullyResolved(item.receiverMono);
					relayInfo.resolvedFields.Add(item);
				}
			}
		}
	}

	public static bool TryResolveField<TIGuidedRefReceiverMono, T>(TIGuidedRefReceiverMono receiverMono, ref T refReceiverObj, GuidedRefReceiverFieldInfo receiverFieldInfo, GuidedRefTryResolveInfo tryResolveInfo) where TIGuidedRefReceiverMono : IGuidedRefReceiverMono where T : UnityEngine.Object
	{
		if (tryResolveInfo.index > -1 || tryResolveInfo.fieldId != receiverFieldInfo.fieldId || refReceiverObj != null)
		{
			return false;
		}
		int num;
		object obj;
		if (tryResolveInfo.targetMono != null)
		{
			num = ((tryResolveInfo.targetMono.GuidedRefTargetObject != null) ? 1 : 0);
			if (num != 0)
			{
				obj = tryResolveInfo.targetMono.GuidedRefTargetObject as T;
				goto IL_006e;
			}
		}
		else
		{
			num = 0;
		}
		obj = null;
		goto IL_006e;
		IL_006e:
		T val = (T)obj;
		if (num == 0)
		{
			string fieldNameByID = GetFieldNameByID(tryResolveInfo.fieldId);
			Debug.LogError("TryResolveField: Receiver \"" + receiverMono.transform.name + "\" with field \"" + fieldNameByID + "\": was already assigned to something other than matching target id! Assigning to found target anyway. Make the receiving field null before attempting to resolve to prevent this message. " + $"fieldId={tryResolveInfo.fieldId}, receiver path=\"{receiverMono.transform.GetPath()}\"");
		}
		else if (refReceiverObj != null && refReceiverObj != val)
		{
			Debug.LogError("was assigned didn't match assigning anyway");
			string fieldNameByID2 = GetFieldNameByID(tryResolveInfo.fieldId);
			Debug.LogError("TryResolveField: Receiver \"" + receiverMono.transform.name + "\" with field \"" + fieldNameByID2 + "\" was already assigned to something other than matching target id! Assigning to found target anyway. Make the receiving field null before attempting to resolve to prevent this message. " + $"fieldId={tryResolveInfo.fieldId}, receiver path=\"{receiverMono.transform.GetPath()}\"");
		}
		refReceiverObj = val;
		int guidedRefsWaitingToResolveCount = receiverMono.GuidedRefsWaitingToResolveCount;
		receiverMono.GuidedRefsWaitingToResolveCount = guidedRefsWaitingToResolveCount - 1;
		return true;
	}

	public static bool TryResolveArrayItem<TIGuidedRefReceiverMono, T>(TIGuidedRefReceiverMono receiverMono, IList<T> receivingArray, GuidedRefReceiverArrayInfo receiverArrayInfo, GuidedRefTryResolveInfo tryResolveInfo) where TIGuidedRefReceiverMono : IGuidedRefReceiverMono where T : UnityEngine.Object
	{
		bool arrayResolved;
		return TryResolveArrayItem(receiverMono, receivingArray, receiverArrayInfo, tryResolveInfo, out arrayResolved);
	}

	public static bool TryResolveArrayItem<TIGuidedRefReceiverMono, T>(TIGuidedRefReceiverMono receiverMono, IList<T> receivingArray, GuidedRefReceiverArrayInfo receiverArrayInfo, GuidedRefTryResolveInfo tryResolveInfo, out bool arrayResolved) where TIGuidedRefReceiverMono : IGuidedRefReceiverMono where T : UnityEngine.Object
	{
		arrayResolved = false;
		if (tryResolveInfo.index <= -1 && receiverArrayInfo.fieldId != tryResolveInfo.fieldId)
		{
			return false;
		}
		if (receivingArray == null)
		{
			string fieldNameByID = GetFieldNameByID(tryResolveInfo.fieldId);
			Debug.LogError("TryResolveArrayItem: Receiver \"" + receiverMono.transform.name + "\" with array \"" + fieldNameByID + "\": Receiving array cannot be null!" + $"fieldId={tryResolveInfo.fieldId}, receiver path=\"{receiverMono.transform.GetPath()}\"");
			return false;
		}
		if (receiverArrayInfo.targets == null)
		{
			string fieldNameByID2 = GetFieldNameByID(tryResolveInfo.fieldId);
			Debug.LogError("TryResolveArrayItem: Receiver component \"" + receiverMono.transform.name + "\" with array \"" + fieldNameByID2 + "\": Targets array is null! It must have been set to null after registering. If this intentional than the you need to unregister first." + $"fieldId={tryResolveInfo.fieldId}, receiver path=\"{receiverMono.transform.GetPath()}\"");
			return false;
		}
		int num = receiverArrayInfo.targets.Length;
		if (num <= receiverArrayInfo.resolveCount)
		{
			string fieldNameByID3 = GetFieldNameByID(tryResolveInfo.fieldId);
			Debug.LogError("TryResolveArrayItem: Receiver component \"" + receiverMono.transform.name + "\" with array \"" + fieldNameByID3 + "\": Targets array size is equal or smaller than resolve count. Did you change the size of the array before it finished resolving or before unregistering?" + $"fieldId={tryResolveInfo.fieldId}, receiver path=\"{receiverMono.transform.GetPath()}\"");
			return false;
		}
		if (num != receivingArray.Count)
		{
			string fieldNameByID4 = GetFieldNameByID(tryResolveInfo.fieldId);
			Debug.LogError("TryResolveArrayItem: Receiver component \"" + receiverMono.transform.name + "\" with array \"" + fieldNameByID4 + "\": The sizes of `receivingList` and `receiverArrayInfo.fieldInfos` are not equal. They must be the same length before calling." + $"fieldId={tryResolveInfo.fieldId}, receiver path=\"{receiverMono.transform.GetPath()}\"");
			return false;
		}
		T val = tryResolveInfo.targetMono.GuidedRefTargetObject as T;
		if (val == null)
		{
			string fieldNameByID5 = GetFieldNameByID(tryResolveInfo.fieldId);
			Debug.LogError("TryResolveArrayItem: Receiver \"" + receiverMono.transform.name + "\" with field \"" + fieldNameByID5 + "\" found a matching target id but target object was null! Was it destroyed without unregistering? " + $"fieldId={tryResolveInfo.fieldId}, receiver path=\"{receiverMono.transform.GetPath()}\"");
		}
		if (receivingArray[tryResolveInfo.index] != null && receivingArray[tryResolveInfo.index] != val)
		{
			string fieldNameByID6 = GetFieldNameByID(tryResolveInfo.fieldId);
			Debug.LogError("TryResolveArrayItem: Receiver \"" + receiverMono.transform.name + "\" with array \"" + fieldNameByID6 + "\" " + $"at index {tryResolveInfo.index}: Already assigned to something other than matching target id! " + "Assigning to found target anyway. Make the receiving field null before attempting to resolve to prevent this message. " + $"fieldId={tryResolveInfo.fieldId}, receiver path=\"{receiverMono.transform.GetPath()}\"");
		}
		arrayResolved = ++receiverArrayInfo.resolveCount >= num;
		int guidedRefsWaitingToResolveCount = receiverMono.GuidedRefsWaitingToResolveCount;
		receiverMono.GuidedRefsWaitingToResolveCount = guidedRefsWaitingToResolveCount - 1;
		receivingArray[tryResolveInfo.index] = val;
		return true;
	}

	public static string GetFieldNameByID(int fieldId)
	{
		return "FieldNameOnlyAvailableInEditor";
	}

	int IGuidedRefObject.GetInstanceID()
	{
		return GetInstanceID();
	}
}
