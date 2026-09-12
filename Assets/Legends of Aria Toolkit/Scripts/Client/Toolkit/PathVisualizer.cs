// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PathVisualizer : MonoBehaviour {

	public Color pathColor = Color.white;
	public int PathSmoothness = 10;

	public Transform[] PathNodes
	{
		get
		{
			return GetComponentsInChildren<Transform>().Where(trans => trans != transform).OrderBy(trans => Int32.Parse(trans.gameObject.name) ).ToArray();
		}
	}

	public void OnDrawGizmos()
	{
		// spline path
		if (PathNodes.Length >= 2)
		{								
			// we want to interpolate along the local positions
			IEnumerable<Vector3> cameraPoints = PathNodes.Select(node => node.position);
			IEnumerator sequence =  Interpolate.NewCatmullRom(cameraPoints.ToArray(), PathSmoothness, false).GetEnumerator();
			
			var firstPoint = PathNodes[0].position;
			Vector3 segmentStart = firstPoint;
			sequence.MoveNext();
			while (sequence.MoveNext())
			{
				Vector3 segmentEnd = (Vector3)sequence.Current;
				//Gizmos.DrawSphere(segmentEnd, 0.1f);
				Gizmos.color = pathColor;
				Gizmos.DrawLine(segmentStart, segmentEnd);
				segmentStart = segmentEnd;
				if (segmentStart == firstPoint) { break; }
			}
		}
	}
}
