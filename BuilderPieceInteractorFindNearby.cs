public class BuilderPieceInteractorFindNearby : MonoBehaviourPostTick
{
	public BuilderPieceInteractor pieceInteractor;

	private void Awake()
	{
	}

	public override void PostTick()
	{
		if (pieceInteractor != null)
		{
			pieceInteractor.StartFindNearbyPieces();
		}
	}
}
