namespace GorillaTag.GuidedRefs;

public interface IGuidedRefReceiverMono : IGuidedRefMonoBehaviour, IGuidedRefObject
{
	int GuidedRefsWaitingToResolveCount { get; set; }

	bool GuidedRefTryResolveReference(GuidedRefTryResolveInfo target);

	void OnAllGuidedRefsResolved();

	void OnGuidedRefTargetDestroyed(int fieldId);
}
