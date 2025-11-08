using GorillaExtensions;
using UnityEngine;

public class SpringyWobbler : MonoBehaviour
{
	[SerializeField]
	private float stabilizingForce;

	[SerializeField]
	private float drag;

	[SerializeField]
	private float maxDisplacement;

	private Transform[] children;

	[SerializeField]
	private Vector3 idealEndpointLocalPos;

	[SerializeField]
	private Vector3 rotateToFaceLocalPos;

	[SerializeField]
	private float startStiffness;

	[SerializeField]
	private float endStiffness;

	private Vector3 lastIdealEndpointWorldPos;

	private Vector3 lastEndpointWorldPos;

	private Vector3 endpointVelocity;

	private void Start()
	{
		int num = 1;
		Transform child = base.transform;
		while (child.childCount > 0)
		{
			child = child.GetChild(0);
			num++;
		}
		children = new Transform[num];
		child = base.transform;
		children[0] = child;
		int num2 = 1;
		while (child.childCount > 0)
		{
			child = child.GetChild(0);
			children[num2] = child;
			num2++;
		}
		lastEndpointWorldPos = children[children.Length - 1].transform.position;
	}

	private void Update()
	{
		float x = base.transform.lossyScale.x;
		Vector3 vector = base.transform.TransformPoint(idealEndpointLocalPos);
		endpointVelocity += (vector - lastEndpointWorldPos) * stabilizingForce * x * Time.deltaTime;
		Vector3 vector2 = lastEndpointWorldPos + endpointVelocity * Time.deltaTime;
		float num = maxDisplacement * x;
		if ((vector2 - vector).IsLongerThan(num))
		{
			vector2 = vector + (vector2 - vector).normalized * num;
		}
		endpointVelocity = (vector2 - lastEndpointWorldPos) * (1f - drag) / Time.deltaTime;
		Vector3 vector3 = base.transform.TransformPoint(rotateToFaceLocalPos);
		Vector3 upwards = base.transform.TransformDirection(Vector3.up);
		Vector3 position = base.transform.position;
		Vector3 ctrl = position + base.transform.TransformDirection(idealEndpointLocalPos) * startStiffness * x;
		Vector3 vector4 = vector2;
		Vector3 ctrl2 = vector4 + (vector3 - vector4).normalized * endStiffness * x;
		for (int i = 1; i < children.Length; i++)
		{
			float num2 = (float)i / (float)(children.Length - 1);
			Vector3 vector5 = BezierUtils.BezierSolve(num2, position, ctrl, ctrl2, vector4);
			Vector3 vector6 = BezierUtils.BezierSolve(num2 + 0.1f, position, ctrl, ctrl2, vector4);
			children[i].transform.position = vector5;
			children[i].transform.rotation = Quaternion.LookRotation(vector6 - vector5, upwards);
		}
		lastIdealEndpointWorldPos = vector;
		lastEndpointWorldPos = vector2;
	}
}
