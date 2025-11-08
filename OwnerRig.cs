using UnityEngine;

public class OwnerRig : MonoBehaviour, IVariable<VRRig>, IVariable, IRigAware
{
	[SerializeField]
	private VRRig _rig;

	public void TryFindRig()
	{
		_rig = GetComponentInParent<VRRig>();
		if (!(_rig != null))
		{
			_rig = GetComponentInChildren<VRRig>();
		}
	}

	public VRRig Get()
	{
		return _rig;
	}

	public void Set(VRRig value)
	{
		_rig = value;
	}

	public void Set(GameObject obj)
	{
		_rig = ((obj != null) ? obj.GetComponentInParent<VRRig>() : null);
	}

	void IRigAware.SetRig(VRRig rig)
	{
		_rig = rig;
	}

	public static implicit operator bool(OwnerRig or)
	{
		if ((object)or == null)
		{
			return false;
		}
		if (or == null)
		{
			return false;
		}
		if ((object)or._rig == null)
		{
			return false;
		}
		if (or._rig == null)
		{
			return false;
		}
		return true;
	}

	public static implicit operator VRRig(OwnerRig or)
	{
		if (!or)
		{
			return null;
		}
		return or._rig;
	}
}
