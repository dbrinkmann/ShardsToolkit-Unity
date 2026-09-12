using UnityEngine;
using System.Collections;

public class ZoomCameraSpline : CameraSpline 
{	
	public float ZoomSpeed = 5.0f;
	public float ZoomMaxAcceleration = 5.0f;
    public float LookAtYOffset = 0f;
    public float LookAtYOffsetZoomed = 0.7f;
    public Transform LookAtTrans;
    public float MovementZoom 
	{
		get { return 0f; }
	}

	// Use this for initialization
	protected override void Start() 
	{
		base.Start();
		
		if( AssignedCameraTrans != null )
		{
			MoveToClosestPosition(AssignedCameraTrans);
        }
		zoomTarget = curPathPos;

		AssignedCameraTrans.LookAt(LookAtTrans.position + (new Vector3(0.0f, LookAtYOffset, 0.0f)));	
	}

	public void AddInput(float input)
	{
		if (input != 0)			
		{			
			zoomTarget += Mathf.Clamp(input * 10.0f, ZoomMaxAcceleration * -1, ZoomMaxAcceleration);
			zoomTarget = Mathf.Clamp(zoomTarget,0,pathLength);
		}
	}

	void Update()
	{
		if( AssignedCameraTrans != null )
		{
			// As a falloff we use the distance between position and target		
			// results in faster Movement at higher distances		
			float falloff = Mathf.Abs(curPathPos-zoomTarget);
			float travelDistance = Time.deltaTime * falloff * ZoomSpeed;
            //Debug.Log(zoomTarget + " is zoom target");
            //Debug.Log(curPathPos + " is cur path pos");
            // 0.001 is our deadzone            

            if (curPathPos+0.001 < zoomTarget)
			{	
				MoveCameraAlongPath(AssignedCameraTrans,travelDistance,false);
                float zoomPercent = curPathPos / pathLength;
                float yOffset = Mathf.Lerp(LookAtYOffset, LookAtYOffsetZoomed, zoomPercent);
                AssignedCameraTrans.LookAt(LookAtTrans.position + (new Vector3(0.0f, yOffset, 0.0f)));	
			}		
			else if (curPathPos-0.001 > zoomTarget)			
			{			
				MoveCameraAlongPath(AssignedCameraTrans,-travelDistance,false);
                float zoomPercent = curPathPos / pathLength;
                float yOffset = Mathf.Lerp(LookAtYOffset, LookAtYOffsetZoomed, zoomPercent);
                AssignedCameraTrans.LookAt(LookAtTrans.position + (new Vector3(0.0f, yOffset, 0.0f)));	
			}
		}
	}

    public void ResetCameraPosition()
    {
        if (curPathPos + ZoomSpeed > MovementZoom)
        {
            zoomTarget -= Mathf.Abs(curPathPos - MovementZoom);
        }
        else if (curPathPos - ZoomSpeed < MovementZoom)
        {
            zoomTarget += Mathf.Abs(curPathPos - MovementZoom);
        }
        else
        {
            zoomTarget = 0;
            curPathPos = MovementZoom;
        }
    }

	private float zoomTarget = -1;
}
