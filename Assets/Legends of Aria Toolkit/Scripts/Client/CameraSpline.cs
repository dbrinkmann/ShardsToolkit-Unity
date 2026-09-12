using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSpline : MonoBehaviour
{
	public Int32 PathSmoothness = 10;
	public Transform AssignedCameraTrans;
	public bool UseLocalSpace = true;
    

	public Transform[] CameraPathNodes
	{
		get
		{
			return GetComponentsInChildren<Transform>().Where(trans => trans != transform).OrderBy(trans => trans.gameObject.name ).ToArray();
		}
	}

	public IEnumerable<Vector3> GetCameraPath()
	{
		IEnumerable<Vector3> cameraPoints = null;
		if( UseLocalSpace )
		{
			// we want to interpolate along the local positions
			cameraPoints = CameraPathNodes.Select(trans => trans.localPosition);
		}
		else
		{
			cameraPoints = CameraPathNodes.Select(trans => trans.position);
		}

        if (cameraPoints.Count() > 1)
        {
            return Interpolate.NewCatmullRom(cameraPoints.ToArray(), PathSmoothness, false);
        }
        else
        {
            staticCameraPos = cameraPoints.First();
            return null;
        }
	}

	protected virtual void Start()
	{
		// cache the path
		runtimeCameraPath = GetCameraPath();

        if (runtimeCameraPath != null)
        {
            IEnumerator<Vector3> sequence = runtimeCameraPath.GetEnumerator();
            sequence.MoveNext();
            Vector3 oldVec = (Vector3)sequence.Current;
            while (sequence.MoveNext())
            {
                Vector3 curVec = (Vector3)sequence.Current;
                pathLength += Vector3.Distance(oldVec, curVec);
                oldVec = curVec;
            }
        }
	}

	public void MoveCameraAlongPath(Transform cameraTrans, float distance, bool shouldWrap = false)
	{
        if (runtimeCameraPath == null)
        {
            if (UseLocalSpace && cameraTrans.localPosition != staticCameraPos)
            {
                cameraTrans.localPosition = staticCameraPos;
            }
            else if (cameraTrans.position != staticCameraPos)
            {
                cameraTrans.position = staticCameraPos;
            }            
        }
        else
        {
            curPathPos = curPathPos + distance;
            if (curPathPos > pathLength)
            {
                if (shouldWrap)
                {
                    curPathPos = pathLength - curPathPos;
                }
                else
                {
                    curPathPos = pathLength;
                }
            }
            else if (curPathPos < 0)
            {
                if (shouldWrap)
                {
                    curPathPos = pathLength - curPathPos;
                }
                else
                {
                    curPathPos = 0;
                }
            }

            IEnumerator<Vector3> sequence = runtimeCameraPath.GetEnumerator();
            sequence.MoveNext();
            Vector3 oldVec = (Vector3)sequence.Current;
            float distTravelled = 0.0f;
            Vector3 newCameraPos = Vector3.zero;
            while (sequence.MoveNext())
            {
                Vector3 curVec = (Vector3)sequence.Current;
                float curDist = Vector3.Distance(oldVec, curVec);
                if (distTravelled + curDist > curPathPos)
                {
                    float lerpPos = (curPathPos - distTravelled) / curDist;
                    newCameraPos = Vector3.Lerp(oldVec, curVec, lerpPos);
                    break;
                }
                distTravelled += curDist;
                oldVec = curVec;
            }

            if (newCameraPos != Vector3.zero)
            {

                //Vector3 dir = new Vector3(newCameraPos.x - AssignedCameraTrans.transform.position.x,0.0f,newCameraPos.z - AssignedCameraTrans.transform.position.z);
                //float yAngle = Vector3.Angle(transform.forward,dir);
                //AssignedCameraTrans.transform.rotation = Quaternion.Euler(35, yAngle, 0);
                if (UseLocalSpace)
                {
                    cameraTrans.localPosition = newCameraPos;
                }
                else
                {
                    cameraTrans.position = newCameraPos;
                }
            }
        }
	}

	public void MoveToClosestPosition(Transform cameraTrans)
	{
		Vector3 closestPos = Vector3.zero;
		float closestDist = float.MaxValue;
		float closestPathPos = 0f;

		Vector3 oldPos = runtimeCameraPath.First();
		Vector3 cameraPos = UseLocalSpace ? cameraTrans.localPosition : cameraTrans.position;
		float foundPathPos = 0f;

		foreach(Vector3 splinePos in runtimeCameraPath)
		{
			float curDist = Vector3.Distance(splinePos,cameraPos);
			foundPathPos += Vector3.Distance(splinePos,oldPos);
			if( curDist < closestDist )
			{
				closestPos = splinePos;
				closestDist = curDist;
				closestPathPos = foundPathPos;
			}
			oldPos = splinePos;
		}

		if( UseLocalSpace )
		{
			cameraTrans.localPosition = closestPos;
		}
		else
		{
			cameraTrans.position = closestPos;
		}
		curPathPos = closestPathPos;
	}

	void OnDrawGizmos()
	{		
		// path nodes
		foreach(var node in CameraPathNodes)
		{
			Gizmos.DrawSphere(node.position, 0.25f);
		}
		
		// spline path
		if (CameraPathNodes.Length >= 2)
		{								
			// we want to interpolate along the local positions
			IEnumerable<Vector3> cameraPoints = CameraPathNodes.Select(node => node.position);
			IEnumerator sequence =  Interpolate.NewCatmullRom(cameraPoints.ToArray(), PathSmoothness, false).GetEnumerator();
			
			var firstPoint = CameraPathNodes[0].position;
			Vector3 segmentStart = firstPoint;
			sequence.MoveNext();
			while (sequence.MoveNext())
			{
				Vector3 segmentEnd = (Vector3)sequence.Current;
				Gizmos.DrawSphere(segmentEnd, 0.1f);
				Gizmos.DrawLine(segmentStart, segmentEnd);
				segmentStart = segmentEnd;
				if (segmentStart == firstPoint) { break; }
			}
		}
	}

	protected IEnumerable<Vector3> runtimeCameraPath;
	protected float pathLength = 0.0f;
	protected float curPathPos = 0.0f;
    protected Vector3 staticCameraPos = Vector3.zero;
}
