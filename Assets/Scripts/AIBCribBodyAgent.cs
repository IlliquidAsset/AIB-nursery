using System.Collections.Generic;
using System.Text;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Supine crib body-schema agent.
///
/// This is intentionally not a locomotion controller: actions target joint poses,
/// and the torso only moves through physics/contact. It is the Unity half of the
/// DEBT-011/DEBT-015 body-schema platform.
/// </summary>
public class AIBCribBodyAgent : Agent
{
    public const int ActionCount = 14;

    [Header("Body Root")]
    public Rigidbody torso;

    [Header("Joints")]
    public ConfigurableJoint neck;
    public ConfigurableJoint leftShoulder;
    public ConfigurableJoint leftElbow;
    public ConfigurableJoint rightShoulder;
    public ConfigurableJoint rightElbow;
    public ConfigurableJoint leftHip;
    public ConfigurableJoint leftKnee;
    public ConfigurableJoint rightHip;
    public ConfigurableJoint rightKnee;

    [Header("Telemetry")]
    public bool logTelemetry = true;
    public float limitHitDegrees = 3f;
    public AIBCribContactSensor headContact;
    public AIBCribContactSensor torsoContact;
    public AIBCribContactSensor leftUpperArmContact;
    public AIBCribContactSensor leftLowerArmContact;
    public AIBCribContactSensor rightUpperArmContact;
    public AIBCribContactSensor rightLowerArmContact;
    public AIBCribContactSensor leftUpperLegContact;
    public AIBCribContactSensor leftLowerLegContact;
    public AIBCribContactSensor rightUpperLegContact;
    public AIBCribContactSensor rightLowerLegContact;

    private readonly Dictionary<ConfigurableJoint, Vector3> _targets = new Dictionary<ConfigurableJoint, Vector3>();
    private readonly List<ConfigurableJoint> _jointOrder = new List<ConfigurableJoint>();
    private readonly float[] _lastCommands = new float[ActionCount];
    private Vector3 _lastTorsoPosition;
    private Quaternion _lastTorsoRotation;
    private string _lastTelemetry = string.Empty;

    public static readonly string[] ActionNames = new[]
    {
        "neck_yaw",
        "neck_pitch",
        "left_shoulder_pitch",
        "left_shoulder_abduct",
        "left_elbow_flex",
        "right_shoulder_pitch",
        "right_shoulder_abduct",
        "right_elbow_flex",
        "left_hip_pitch",
        "left_hip_abduct",
        "left_knee_flex",
        "right_hip_pitch",
        "right_hip_abduct",
        "right_knee_flex"
    };

    public override void Initialize()
    {
        if (torso == null)
        {
            torso = GetComponent<Rigidbody>();
        }

        _jointOrder.Clear();
        AddIfPresent(neck);
        AddIfPresent(leftShoulder);
        AddIfPresent(leftElbow);
        AddIfPresent(rightShoulder);
        AddIfPresent(rightElbow);
        AddIfPresent(leftHip);
        AddIfPresent(leftKnee);
        AddIfPresent(rightHip);
        AddIfPresent(rightKnee);

        _lastTorsoPosition = transform.position;
        _lastTorsoRotation = transform.rotation;
    }

    public override void OnEpisodeBegin()
    {
        if (torso != null)
        {
            torso.linearVelocity = Vector3.zero;
            torso.angularVelocity = Vector3.zero;
        }

        foreach (ConfigurableJoint joint in _jointOrder)
        {
            if (joint == null || joint.GetComponent<Rigidbody>() == null)
            {
                continue;
            }

            Rigidbody rb = joint.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        _lastTorsoPosition = transform.position;
        _lastTorsoRotation = transform.rotation;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Contract: 14 action channels × (angle, angular_velocity, command, limit_hit) = 56.
        AddChannelObservation(sensor, neck, Axis.Y, _lastCommands[0]);
        AddChannelObservation(sensor, neck, Axis.X, _lastCommands[1]);
        AddChannelObservation(sensor, leftShoulder, Axis.X, _lastCommands[2]);
        AddChannelObservation(sensor, leftShoulder, Axis.Z, _lastCommands[3]);
        AddChannelObservation(sensor, leftElbow, Axis.X, _lastCommands[4]);
        AddChannelObservation(sensor, rightShoulder, Axis.X, _lastCommands[5]);
        AddChannelObservation(sensor, rightShoulder, Axis.Z, _lastCommands[6]);
        AddChannelObservation(sensor, rightElbow, Axis.X, _lastCommands[7]);
        AddChannelObservation(sensor, leftHip, Axis.X, _lastCommands[8]);
        AddChannelObservation(sensor, leftHip, Axis.Z, _lastCommands[9]);
        AddChannelObservation(sensor, leftKnee, Axis.X, _lastCommands[10]);
        AddChannelObservation(sensor, rightHip, Axis.X, _lastCommands[11]);
        AddChannelObservation(sensor, rightHip, Axis.Z, _lastCommands[12]);
        AddChannelObservation(sensor, rightKnee, Axis.X, _lastCommands[13]);

        // Contract: contact flags in the order specified by supine-crib-telemetry-contract.md.
        sensor.AddObservation(ContactFlag(headContact));
        sensor.AddObservation(ContactFlag(torsoContact));
        sensor.AddObservation(ContactFlag(leftUpperArmContact));
        sensor.AddObservation(ContactFlag(leftLowerArmContact));
        sensor.AddObservation(ContactFlag(rightUpperArmContact));
        sensor.AddObservation(ContactFlag(rightLowerArmContact));
        sensor.AddObservation(ContactFlag(leftUpperLegContact));
        sensor.AddObservation(ContactFlag(leftLowerLegContact));
        sensor.AddObservation(ContactFlag(rightUpperLegContact));
        sensor.AddObservation(ContactFlag(rightLowerLegContact));

        // Contract: torso position, torso delta, torso orientation, head orientation = 12.
        sensor.AddObservation(transform.position);
        sensor.AddObservation(transform.position - _lastTorsoPosition);
        Vector3 torsoEuler = SignedEuler(transform.rotation.eulerAngles) * Mathf.Deg2Rad;
        sensor.AddObservation(torsoEuler);
        Vector3 headEuler = headContact != null ? SignedEuler(headContact.transform.rotation.eulerAngles) * Mathf.Deg2Rad : Vector3.zero;
        sensor.AddObservation(headEuler);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        ActionSegment<float> a = actionBuffers.ContinuousActions;
        _targets.Clear();
        for (int i = 0; i < ActionCount; i++)
        {
            _lastCommands[i] = Read(a, i);
        }

        Set(neck, Axis.Y, _lastCommands[0], -80f, 80f);     // neck_yaw
        Set(neck, Axis.X, _lastCommands[1], -55f, 65f);     // neck_pitch

        Set(leftShoulder, Axis.X, _lastCommands[2], -35f, 150f);
        Set(leftShoulder, Axis.Z, _lastCommands[3], -20f, 145f);
        Set(leftElbow, Axis.X, _lastCommands[4], 0f, 140f);

        Set(rightShoulder, Axis.X, _lastCommands[5], -35f, 150f);
        Set(rightShoulder, Axis.Z, _lastCommands[6], -145f, 20f);
        Set(rightElbow, Axis.X, _lastCommands[7], 0f, 140f);

        Set(leftHip, Axis.X, _lastCommands[8], -10f, 125f);
        Set(leftHip, Axis.Z, _lastCommands[9], -20f, 60f);
        Set(leftKnee, Axis.X, _lastCommands[10], 0f, 140f);

        Set(rightHip, Axis.X, _lastCommands[11], -10f, 125f);
        Set(rightHip, Axis.Z, _lastCommands[12], -60f, 20f);
        Set(rightKnee, Axis.X, _lastCommands[13], 0f, 140f);

        foreach (KeyValuePair<ConfigurableJoint, Vector3> entry in _targets)
        {
            entry.Key.targetRotation = Quaternion.Euler(entry.Value);
        }

        if (logTelemetry)
        {
            _lastTelemetry = BuildTelemetry(a);
        }

        _lastTorsoPosition = transform.position;
        _lastTorsoRotation = transform.rotation;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuous = actionsOut.ContinuousActions;
        for (int i = 0; i < continuous.Length; i++)
        {
            continuous[i] = 0f;
        }
    }

    public string GetLastTelemetry()
    {
        return _lastTelemetry;
    }

    private void AddIfPresent(ConfigurableJoint joint)
    {
        if (joint != null)
        {
            _jointOrder.Add(joint);
        }
    }

    private static float Read(ActionSegment<float> actions, int index)
    {
        return index < actions.Length ? Mathf.Clamp(actions[index], -1f, 1f) : 0f;
    }

    private void Set(ConfigurableJoint joint, Axis axis, float action, float minDegrees, float maxDegrees)
    {
        if (joint == null)
        {
            return;
        }

        if (!_targets.TryGetValue(joint, out Vector3 euler))
        {
            euler = Vector3.zero;
        }

        float degrees = Mathf.Lerp(minDegrees, maxDegrees, (action + 1f) * 0.5f);
        switch (axis)
        {
            case Axis.X:
                euler.x = degrees;
                break;
            case Axis.Y:
                euler.y = degrees;
                break;
            case Axis.Z:
                euler.z = degrees;
                break;
        }

        _targets[joint] = euler;
    }

    private Vector3 GetTargetEuler(ConfigurableJoint joint)
    {
        if (joint == null)
        {
            return Vector3.zero;
        }

        return _targets.TryGetValue(joint, out Vector3 euler) ? euler : Vector3.zero;
    }

    private void AddChannelObservation(VectorSensor sensor, ConfigurableJoint joint, Axis axis, float command)
    {
        float angle = joint != null ? GetSignedLocalAxisRadians(joint.transform, axis) : 0f;
        float angularVelocity = 0f;
        if (joint != null)
        {
            Rigidbody rb = joint.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 localAngularVelocity = joint.transform.InverseTransformDirection(rb.angularVelocity);
                angularVelocity = AxisValue(localAngularVelocity, axis);
            }
        }

        float limitHit = Mathf.Abs(command) > 0.98f ? 1f : 0f;
        sensor.AddObservation(angle);
        sensor.AddObservation(angularVelocity);
        sensor.AddObservation(command);
        sensor.AddObservation(limitHit);
    }

    private static float ContactFlag(AIBCribContactSensor sensor)
    {
        return sensor != null && sensor.IsInContact ? 1f : 0f;
    }

    private static float GetSignedLocalAxisRadians(Transform transform, Axis axis)
    {
        return AxisValue(SignedEuler(transform.localRotation.eulerAngles), axis) * Mathf.Deg2Rad;
    }

    private static Vector3 SignedEuler(Vector3 euler)
    {
        return new Vector3(NormalizeAngle(euler.x), NormalizeAngle(euler.y), NormalizeAngle(euler.z));
    }

    private static float NormalizeAngle(float degrees)
    {
        while (degrees > 180f) degrees -= 360f;
        while (degrees < -180f) degrees += 360f;
        return degrees;
    }

    private static float AxisValue(Vector3 value, Axis axis)
    {
        switch (axis)
        {
            case Axis.X:
                return value.x;
            case Axis.Y:
                return value.y;
            case Axis.Z:
                return value.z;
            default:
                return 0f;
        }
    }

    private string BuildTelemetry(ActionSegment<float> actions)
    {
        var sb = new StringBuilder();
        sb.Append("commands=");
        for (int i = 0; i < ActionNames.Length; i++)
        {
            if (i > 0) sb.Append(';');
            sb.Append(ActionNames[i]).Append(':').Append(Read(actions, i).ToString("F3"));
        }

        Vector3 torsoDelta = transform.position - _lastTorsoPosition;
        Quaternion rotationDelta = transform.rotation * Quaternion.Inverse(_lastTorsoRotation);
        sb.Append("|torso_delta=").Append(torsoDelta.x.ToString("F4")).Append(',')
            .Append(torsoDelta.y.ToString("F4")).Append(',')
            .Append(torsoDelta.z.ToString("F4"));
        sb.Append("|torso_rot_delta=").Append(rotationDelta.eulerAngles.x.ToString("F2")).Append(',')
            .Append(rotationDelta.eulerAngles.y.ToString("F2")).Append(',')
            .Append(rotationDelta.eulerAngles.z.ToString("F2"));
        sb.Append("|joints=");
        for (int i = 0; i < _jointOrder.Count; i++)
        {
            ConfigurableJoint joint = _jointOrder[i];
            if (i > 0) sb.Append(';');
            sb.Append(joint.name).Append(':').Append(GetTargetEuler(joint));
        }
        return sb.ToString();
    }

    private enum Axis
    {
        X,
        Y,
        Z
    }
}
