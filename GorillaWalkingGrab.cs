using UnityEngine;

public class GorillaWalkingGrab : MonoBehaviour
{
	public GameObject handToStickTo;

	public float ratioToUse;

	public float forceMultiplier;

	public int historySteps;

	public Rigidbody playspaceRigidbody;

	private Rigidbody thisRigidbody;

	private Vector3 lastPosition;

	private Vector3 maybeLastPositionIDK;

	private Vector3[] positionHistory;

	private int historyIndex;

	private void Start()
	{
		thisRigidbody = base.gameObject.GetComponent<Rigidbody>();
		positionHistory = new Vector3[historySteps];
		historyIndex = 0;
	}

	private void FixedUpdate()
	{
		historyIndex++;
		if (historyIndex >= historySteps)
		{
			historyIndex = 0;
		}
		positionHistory[historyIndex] = handToStickTo.transform.position;
		thisRigidbody.MovePosition(handToStickTo.transform.position);
		base.transform.rotation = handToStickTo.transform.rotation;
	}

	private bool MakeJump()
	{
		return false;
	}

	private void OnCollisionStay(Collision collision)
	{
		if (!MakeJump())
		{
			Vector3 vector = Vector3.ProjectOnPlane(positionHistory[(historyIndex != 0) ? (historyIndex - 1) : (historySteps - 1)] - handToStickTo.transform.position, collision.GetContact(0).normal);
			Vector3 vector2 = thisRigidbody.transform.position - handToStickTo.transform.position;
			playspaceRigidbody.MovePosition(playspaceRigidbody.transform.position + vector - vector2);
		}
	}
}
