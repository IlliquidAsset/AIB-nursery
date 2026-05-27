using UnityEngine;

/// <summary>
/// Minimal contact flag source for crib body-schema telemetry.
/// </summary>
public class AIBCribContactSensor : MonoBehaviour
{
    public bool IsInContact { get; private set; }
    public Vector3 LastContactNormal { get; private set; }

    private int _contactCount;

    private void OnCollisionEnter(Collision collision)
    {
        _contactCount++;
        IsInContact = _contactCount > 0;
        if (collision.contactCount > 0)
        {
            LastContactNormal = collision.GetContact(0).normal;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        IsInContact = true;
        if (collision.contactCount > 0)
        {
            LastContactNormal = collision.GetContact(0).normal;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        _contactCount = Mathf.Max(0, _contactCount - 1);
        IsInContact = _contactCount > 0;
        if (!IsInContact)
        {
            LastContactNormal = Vector3.zero;
        }
    }
}
