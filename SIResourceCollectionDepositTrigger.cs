using UnityEngine;

public class SIResourceCollectionDepositTrigger : MonoBehaviour
{
	public GameObject parentCollection;

	private ISIResourceDeposit resourceDeposit;

	private void Awake()
	{
		resourceDeposit = parentCollection.GetComponent<ISIResourceDeposit>();
	}

	private void OnTriggerEnter(Collider other)
	{
		SIResource componentInParent = other.GetComponentInParent<SIResource>();
		if (!(componentInParent == null) && componentInParent.CanDeposit())
		{
			resourceDeposit.ResourceDeposited(componentInParent);
		}
	}
}
