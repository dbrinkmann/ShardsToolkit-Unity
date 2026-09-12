using UnityEngine;
using System.Collections;

public class ParticleBugs : MonoBehaviour {

    float c_BugRotationTime = 500.0f;
    ParticleSystem.Particle[] bugs = null;

	// Use this for initialization
	void Start () {
        ParticleSystem bugSwarm = GetComponent<ParticleSystem>();
        bugSwarm.Simulate(1.0f);
	}
	
	// Update is called once per frame
    void LateUpdate()
    {
        ParticleSystem bugSwarm = GetComponent<ParticleSystem>();
        bugSwarm.simulationSpace = ParticleSystemSimulationSpace.Local;
        int numBugs = bugSwarm.GetParticles(bugs);
        Debug.Log("Number of bugs is " + numBugs,gameObject);
        //Debug.Log("bug position 1 is " + bugs[1].position);
       // Debug.Log("bug velocity 1 is " + bugs[1].velocity);
        for (int i = 0; i < numBugs;i++ )
        {
            float newVelocityX = bugs[i].velocity.x - bugs[i].position.x / (c_BugRotationTime / 2);
            float newVelocityY = bugs[i].velocity.y - bugs[i].position.y / (c_BugRotationTime / 2);
            float newVelocityZ = bugs[i].velocity.z - bugs[i].position.z / (c_BugRotationTime / 2);
            bugs[i].velocity = new Vector3(newVelocityX, newVelocityY, newVelocityZ);
        }
        //bugSwarm.SetParticles(bugs,numBugs);
	}
}
