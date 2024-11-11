using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class MoveToGoalAgent : Agent
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform blockTransform;
    [SerializeField] private Transform block1Transform;
    [SerializeField] private Transform block2Transform;

    [SerializeField] private Material winMaterial;
    [SerializeField] private Material loseMaterial;
    [SerializeField] private MeshRenderer floorMeshRenderer;

    private float startingDistanceToTarget;


    public override void OnEpisodeBegin()
    {
        startingDistanceToTarget = Vector3.Distance(transform.localPosition, targetTransform.localPosition);
        float randomYRotation = Random.Range(0f, 360f);
        transform.rotation = Quaternion.Euler(0f, randomYRotation, 0f);
        transform.localPosition = new Vector3(Random.Range(0.0f, 60.0f), 0, Random.Range(-3f, 0f));
        targetTransform.localPosition = new Vector3(Random.Range(0f, 60f), 0, Random.Range(50f, 60f));
        blockTransform.localPosition = new Vector3(Random.Range(0f, 60f), 0, Random.Range(10f, 40f));
        blockTransform.localRotation = transform.rotation;
        block1Transform.localPosition = new Vector3(Random.Range(0f, 60f), 0, Random.Range(10f, 40f));
        block1Transform.localRotation = transform.rotation;
        block2Transform.localPosition = new Vector3(Random.Range(0f, 60f), 0, Random.Range(10f, 40f));
        block2Transform.localRotation = transform.rotation;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 3 observations: Agent's position
        sensor.AddObservation(transform.localPosition);

        // 3 observations: Direction to target
        Vector3 directionToTarget = targetTransform.localPosition - transform.localPosition;
        sensor.AddObservation(directionToTarget);

        // 3 observations: Raycasts for forward, left, and right distances
        RaycastHit hit;
        float raycastDistance = 5.0f;

        if (Physics.Raycast(transform.position, transform.forward, out hit, raycastDistance))
            sensor.AddObservation(hit.distance / raycastDistance);
        else
            sensor.AddObservation(1f);

        if (Physics.Raycast(transform.position, transform.right, out hit, raycastDistance))
            sensor.AddObservation(hit.distance / raycastDistance);
        else
            sensor.AddObservation(1f);

        if (Physics.Raycast(transform.position, -transform.right, out hit, raycastDistance))
            sensor.AddObservation(hit.distance / raycastDistance);
        else
            sensor.AddObservation(1f);

        // 6 observations: Relative positions to two blocks instead of three
        Vector3 directionToBlock = blockTransform.localPosition - transform.localPosition;
        Vector3 directionToBlock1 = block1Transform.localPosition - transform.localPosition;
        sensor.AddObservation(directionToBlock);
        sensor.AddObservation(directionToBlock1);
    }





    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        float moveSpeed = 3f;
        transform.localPosition += new Vector3(moveX, 0, moveZ) * Time.deltaTime * moveSpeed;

        float currentDistanceToTarget = Vector3.Distance(transform.localPosition, targetTransform.localPosition);
        float distanceProgress = (startingDistanceToTarget - currentDistanceToTarget) / startingDistanceToTarget;

        // Reward or penalize based on the normalized distance progress
        AddReward(distanceProgress * 0.1f);

        // Small step penalty to encourage reaching the target quickly
        AddReward(-0.001f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Goal>(out Goal goal))
        {
            SetReward(+2.5f); // Reward for reaching the goal
            floorMeshRenderer.material = winMaterial;
            EndEpisode();
        }
        else if (other.TryGetComponent<Wall>(out Wall wall))
        {
            AddReward(-2.0f);  // Higher penalty for hitting a wall
            floorMeshRenderer.material = loseMaterial;
            EndEpisode();
        }
    }


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");
    }
    
}
