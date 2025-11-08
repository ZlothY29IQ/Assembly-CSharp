using System.Collections;
using UnityEngine;

public class DeployedChild : MonoBehaviour
{
	[SerializeField]
	private Rigidbody _rigidbody;

	[SerializeReference]
	private DeployableObject _parent;

	private bool _isRemote;

	public void Deploy(DeployableObject parent, Vector3 launchPos, Quaternion launchRot, Vector3 releaseVel, bool isRemote = false)
	{
		_parent = parent;
		_parent.DeployChild();
		Transform obj = base.transform;
		obj.position = launchPos;
		obj.rotation = launchRot;
		obj.localScale = _parent.transform.lossyScale;
		_rigidbody.linearVelocity = releaseVel;
		_isRemote = isRemote;
	}

	public void ReturnToParent(float delay)
	{
		if (delay > 0f)
		{
			StartCoroutine(ReturnToParentDelayed(delay));
		}
		else if (_parent != null)
		{
			_parent.ReturnChild();
		}
	}

	private IEnumerator ReturnToParentDelayed(float delay)
	{
		float start = Time.time;
		while (Time.time < start + delay)
		{
			yield return null;
		}
		if (_parent != null)
		{
			_parent.ReturnChild();
		}
	}
}
