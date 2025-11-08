using GorillaExtensions;
using UnityEngine;

public class SlingshotProjectileTrail : MonoBehaviour
{
	public TrailRenderer trailRenderer;

	public Color defaultColor = Color.white;

	public Color orangeColor = new Color(1f, 0.5f, 0f, 1f);

	public Color blueColor = new Color(0f, 0.72f, 1f, 1f);

	private GameObject followObject;

	private Transform followXform;

	private float timeToDie = -1f;

	private float initialScale;

	private float initialWidthMultiplier;

	private void Awake()
	{
		initialWidthMultiplier = trailRenderer.widthMultiplier;
	}

	public void AttachTrail(GameObject obj, bool blueTeam, bool redTeam, bool shouldOverrideColor = false, Color overrideColor = default(Color))
	{
		followObject = obj;
		followXform = followObject.transform;
		Transform transform = base.transform;
		transform.position = followXform.position;
		initialScale = transform.localScale.x;
		transform.localScale = followXform.localScale;
		trailRenderer.widthMultiplier = initialWidthMultiplier * followXform.localScale.x;
		trailRenderer.Clear();
		if (shouldOverrideColor)
		{
			SetColor(overrideColor);
		}
		else if (blueTeam)
		{
			SetColor(blueColor);
		}
		else if (redTeam)
		{
			SetColor(orangeColor);
		}
		else
		{
			SetColor(defaultColor);
		}
		timeToDie = -1f;
	}

	protected void LateUpdate()
	{
		if (followObject.IsNull())
		{
			ObjectPools.instance.Destroy(base.gameObject);
			return;
		}
		base.gameObject.transform.position = followXform.position;
		if (!followObject.activeSelf && timeToDie < 0f)
		{
			timeToDie = Time.time + trailRenderer.time;
		}
		if (timeToDie > 0f && Time.time > timeToDie)
		{
			base.transform.localScale = Vector3.one * initialScale;
			ObjectPools.instance.Destroy(base.gameObject);
		}
	}

	public void SetColor(Color color)
	{
		TrailRenderer obj = trailRenderer;
		Color startColor = (trailRenderer.endColor = color);
		obj.startColor = startColor;
	}
}
