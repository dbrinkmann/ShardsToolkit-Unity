using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
	private LocalPlayer _playerTarget = null;
	private const float _rotateSpeed = 0.35f;
	private const float _zoomSpeed = 5f;
	[SerializeField] private ZoomCameraSpline _spline;

	
	public void SetPlayerTarget(LocalPlayer playerTarget)
	{
		_playerTarget = playerTarget;
		_spline.LookAtTrans = playerTarget.transform;
		StartCoroutine(DelayedStartCameraPath());
	}

	private IEnumerator DelayedStartCameraPath()
	{
		yield return new WaitForEndOfFrame();
		ZoomCamera(1f);
		yield return new WaitForEndOfFrame();
		ZoomCamera(-1f);
	}

	void Update()
	{
		if (_playerTarget)
		{
			transform.position = _playerTarget.transform.position;
		}
	}

	public void RotateCamera(float mouseDeltaX)
	{
		transform.Rotate(Vector3.up, mouseDeltaX * _rotateSpeed);
	}

	public void ZoomCamera(float zoom)
	{
		_spline.AddInput(zoom * _zoomSpeed * Time.deltaTime);
	}
}
