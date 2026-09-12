using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioZone : MonoBehaviour {

	public static List<AudioZone> MapAudioZones = new List<AudioZone>();

	public float ZoneRadius = 10.0f;

	public string MusicEntry = "";
	public string AmbienceEntry = "";
    public string AmbienceNightEntry = "";

	public void Start()
	{
		MapAudioZones.Add(this);
	}

	public void OnDestroy()
	{
		MapAudioZones.Remove(this);
	}

	public bool IsInside(Transform obj)
	{
		Vector2 audioPos2 = new Vector2(transform.position.x,transform.position.z);
		Vector2 objPos2 = new Vector2(obj.position.x,obj.position.z);
		return Vector2.Distance(audioPos2,objPos2) < ZoneRadius;
	}

	void OnDrawGizmosSelected() {
		Gizmos.DrawWireSphere(transform.position,ZoneRadius);
	}
}
