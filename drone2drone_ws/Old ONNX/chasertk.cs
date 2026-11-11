using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

public class chasertk : Agent
{
    Rigidbody rigidBody;
    public float inputDrag, drag;
    public float maxSpeed;
    public float speed;
    public float rawYaw, rawElv, rawRoll, rawPitch;
    private float yaw, elv, roll, pitch;
    public Transform target;
    public Transform chaserfriend;
    public Transform box;
    private float orgdistance;
    private float currentdistance;
    private float currentdistance1;
    private float improvement;
    private Vector3 directiontotarget;
    private Vector3 initialspawn;

    public void Start()
    {
        //initialspawn = this.transform.localPosition;
        rigidBody = GetComponent<Rigidbody>();
    }

    public void TakeOff()
    {
        this.transform.localRotation = Quaternion.identity;
        this.transform.localPosition = new Vector3(Random.value * 4.5f - 1.2f, Random.value * 2.2f - 0.5f, Random.value * 4.7f - 0.5f);
        gameObject.GetComponent<Rigidbody>().useGravity = true;
    }


    public override void OnActionReceived(ActionBuffers actionBuffers)
    {

        rawElv = actionBuffers.ContinuousActions[0];
        rawRoll = actionBuffers.ContinuousActions[1];
        rawPitch = actionBuffers.ContinuousActions[2];
        //rawYaw = actionBuffers.ContinuousActions[3];
        rigidBody.AddForce(transform.up * 9.81f);
        elv = rawElv * speed;
        roll = rawRoll * speed;
        pitch = rawPitch * speed;
        yaw = rawYaw * speed;

        var angularVelocity = rigidBody.angularVelocity;
        var velocity = rigidBody.velocity;
        var yawAngle = -Mathf.PI / 180f * this.transform.localRotation.eulerAngles.y;
        velocity.x = roll * Mathf.Cos(yawAngle) - pitch * Mathf.Sin(yawAngle);
        velocity.y = elv;
        velocity.z = roll * Mathf.Sin(yawAngle) + pitch * Mathf.Cos(yawAngle);
        angularVelocity.y = yaw;

        rigidBody.velocity = velocity;
        rigidBody.angularVelocity = angularVelocity;

        if (rigidBody.velocity.magnitude > maxSpeed)
        {
            rigidBody.velocity = rigidBody.velocity.normalized * maxSpeed;
        }

        AddReward(-10000 / this.MaxStep);
        //if (Vector3.Distance(this.transform.localPosition, box.localPosition) < 0.4)
        //{
        //    AddReward(-1000.0f);
        //    EndEpisode();
        //    Debug.Log("too close to box");
        //}
        //if (Vector3.Distance(this.transform.localPosition, box.localPosition) < 0.7)
        //{
        //    AddReward(20000 / this.MaxStep);
        //    //Debug.Log("optimal range");
        //}
        if (Vector3.Distance(this.transform.localPosition, chaserfriend.localPosition) < 0.5)
        {
            AddReward(-1000.0f);
            EndEpisode();
            Debug.Log("friendly collision");
        }

    }

    public override void OnEpisodeBegin()
    {
        TakeOff();
        rigidBody.inertiaTensor = new Vector3(1, 1, 1);
        this.rigidBody.velocity = Vector3.zero;
        orgdistance = Vector3.Distance(this.transform.localPosition, target.localPosition);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(this.transform.localPosition - target.localPosition); //3
        sensor.AddObservation(this.transform.localPosition - chaserfriend.localPosition);//3
        //sensor.AddObservation(this.transform.localPosition - box.localPosition);//3
        ///sensor.AddObservation(Vector3.Distance(this.transform.localPosition, box.localPosition));//1
        sensor.AddObservation(Vector3.Distance(this.transform.localPosition, chaserfriend.localPosition));//1
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        //continuousActionsOut[3] = Input.GetAxis("Keyboard Yaw");
        continuousActionsOut[0] = Input.GetAxis("Keyboard Elv");
        continuousActionsOut[1] = Input.GetAxis("Keyboard Roll");
        continuousActionsOut[2] = Input.GetAxis("Keyboard Pitch");
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("wall"))
        {
            AddReward(-1000.0f);
            EndEpisode();
            Debug.Log("chaser hit wall");
        }
        if (other.gameObject.CompareTag("runner"))
        {
            AddReward(1000.0f);
            EndEpisode();
            Debug.Log("chaser found runner");
        }
        //if (other.gameObject.CompareTag("chaser"))
        //{
        //    AddReward(-1000.0f);
        //    EndEpisode();
        //    Debug.Log("friendly collision");
        //}
    }
}
