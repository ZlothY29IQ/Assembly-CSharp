using UnityEngine;
using UnityEngine.Rendering;

public class ReparentOnAwakeWithRenderer : MonoBehaviour, IBuildValidation
{
	public Transform newParent;

	public MeshRenderer myRenderer;

	[Tooltip("We're mostly using this for UI elements like text and images, so this will help you separate these in whatever target parent object.Keep images and texts together, otherwise you'll get extra draw calls. Put images above text or they'll overlap weird tho lol")]
	public bool sortLast;

	public bool BuildValidationCheck()
	{
		if (GetComponent<MeshRenderer>() != null && myRenderer == null)
		{
			Debug.Log(base.name + " needs a reference to its renderer since it has one - ");
			return false;
		}
		return true;
	}

	private void OnEnable()
	{
		base.transform.SetParent(newParent, worldPositionStays: true);
		if (sortLast)
		{
			base.transform.SetAsLastSibling();
		}
		else
		{
			base.transform.SetAsFirstSibling();
		}
		if (myRenderer != null)
		{
			myRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
			myRenderer.lightProbeUsage = LightProbeUsage.Off;
			myRenderer.probeAnchor = newParent;
		}
	}

	[ContextMenu("Set Renderer")]
	public void SetMyRenderer()
	{
		myRenderer = GetComponent<MeshRenderer>();
	}
}
