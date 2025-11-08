using Photon.Pun;
using UnityEngine;

public class SplineWalker : MonoBehaviour, IPunObservable
{
	public BezierSpline spline;

	public LinearSpline linearSpline;

	public float duration;

	public bool lookForward;

	public SplineWalkerMode mode;

	public bool walkLinearPath;

	public bool useWorldPosition;

	public float progress;

	private bool goingForward = true;

	public bool DoNetworkSync = true;

	private PhotonView _view;

	private void Awake()
	{
		_view = GetComponent<PhotonView>();
	}

	private void Update()
	{
		if (goingForward)
		{
			progress += Time.deltaTime / duration;
			if (progress > 1f)
			{
				if (mode == SplineWalkerMode.Once)
				{
					progress = 1f;
				}
				else if (mode == SplineWalkerMode.Loop)
				{
					progress -= 1f;
				}
				else
				{
					progress = 2f - progress;
					goingForward = false;
				}
			}
		}
		else
		{
			progress -= Time.deltaTime / duration;
			if (progress < 0f)
			{
				progress = 0f - progress;
				goingForward = true;
			}
		}
		if (linearSpline != null && walkLinearPath)
		{
			Vector3 vector = linearSpline.Evaluate(progress);
			if (useWorldPosition)
			{
				base.transform.position = vector;
			}
			else
			{
				base.transform.localPosition = vector;
			}
			if (lookForward)
			{
				base.transform.LookAt(vector + linearSpline.GetForwardTangent(progress));
			}
		}
		else if (spline != null)
		{
			Vector3 point = spline.GetPoint(progress);
			if (useWorldPosition)
			{
				base.transform.position = point;
			}
			else
			{
				base.transform.localPosition = point;
			}
			if (lookForward)
			{
				base.transform.LookAt(point + spline.GetDirection(progress));
			}
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		stream.Serialize(ref progress);
	}
}
