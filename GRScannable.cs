using UnityEngine;

public class GRScannable : MonoBehaviour
{
	public GameEntity gameEntity;

	[SerializeField]
	protected string titleText;

	[SerializeField]
	protected string bodyText;

	[SerializeField]
	protected string annotationText;

	public virtual void Start()
	{
		if (gameEntity == null)
		{
			gameEntity = GetComponent<GameEntity>();
		}
	}

	public virtual string GetTitleText(GhostReactor reactor)
	{
		return titleText;
	}

	public virtual string GetBodyText(GhostReactor reactor)
	{
		return bodyText;
	}

	public virtual string GetAnnotationText(GhostReactor reactor)
	{
		return annotationText;
	}
}
