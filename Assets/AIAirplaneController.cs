using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AirplaneConfig))]
public class AIAirplaneController : MonoBehaviour
{
    [SerializeField]
    List<AeroSurface> controlSurfaces = null;
    [SerializeField]
    List<WheelCollider> wheels = null;
    [SerializeField]
    float rollControlSensitivity = 0.2f;
    [SerializeField]
    float pitchControlSensitivity = 0.2f;
    [SerializeField]
    float yawControlSensitivity = 0.2f;
    [SerializeField]
    float thrustPercent = 0.8f;

    [Range(-1, 1)]
    public float Pitch;
    [Range(-1, 1)]
    public float Yaw;
    [Range(-1, 1)]
    public float Roll;
    [Range(0, 1)]
    public float Flap;

    float brakesTorque;
    AircraftPhysics aircraftPhysics;
    AirplaneConfig airplaneConfig;
    Rigidbody rb;

    [SerializeField] Rotator propeller;

    [SerializeField]
    bool shouldMaintainAltitude = false;
    [SerializeField]
    float altitudeMaintainSensitivity = 0.1f; // Adjusting speed for maintaining altitude, plane specific, adjust when needed
    [SerializeField]
    float altitudeAcceptanceRadius = 10f; // Allowable margin for desired altitude
    private float currentAltitude;
    private float desiredAltitude = -1; // -1 means no altitude control

    [SerializeField]
    bool shouldGoToTarget = false;
    [SerializeField]
    float detectonRadius = 1000f;
    [SerializeField]
    Transform target; // Target location to fly toward
    [SerializeField]
    bool shouldPatrol = false;
    [SerializeField]
    List<Transform> patrolWaypoints = new List<Transform>();
    [SerializeField]
    float acceptanceRadius = 10f; // Radius within which the target is considered reached

    [SerializeField]
    bool shouldAttackTarget = false;
    [SerializeField]
    AirplaneConfig attackTarget; // Target location to fly toward to attack
    [SerializeField]
    float attackRange = 500f; // Distance at which to start attacking
    [SerializeField]
    float attackFieldOfView = 10f; // Field of view (cone) to check if the target is in front

    public void SetThrustPercent(float percent)
    {
        thrustPercent = percent;
    }

    public void SetDesiredAltitude(float altitude)
    {
        desiredAltitude = altitude;
    }
    public Rigidbody GetRigidBody()
    {
        return rb;
    }

    private void Start()
    {
        airplaneConfig = GetComponent<AirplaneConfig>();
        aircraftPhysics = GetComponent<AircraftPhysics>();
        rb = GetComponent<Rigidbody>();

        if (target == null)
        {
            Debug.LogError("Target not assigned to AIAirplaneController.");
        }
    }

    private void Update()
    {
        if (propeller != null)
        {
            propeller.speed = thrustPercent * 1500f;
        }

        // Update the current altitude from the transform position
        currentAltitude = transform.position.y;

        if (airplaneConfig.Dead) return;
        //SetTriggers();
        MaintainAltitude();
        GoToTarget(target);
        AttackTarget();
        Patrol();
    }

    private void SetTriggers()
    {
        if(airplaneConfig.Dead)
        {
            shouldAttackTarget = false;
            shouldPatrol = false;
            shouldMaintainAltitude = false;
        }

        if(attackTarget != null && !attackTarget.Dead && Vector3.Distance(transform.position, attackTarget.transform.position) <= detectonRadius)
        {
            shouldAttackTarget = true;
            shouldPatrol = false;
        }
        else
        {
            shouldAttackTarget = false;
            shouldPatrol = true;
        }
    }

    private void FixedUpdate()
    {
        SetControlSurfacesAngles(Pitch, Roll, Yaw, Flap);
        aircraftPhysics.SetThrustPercent(thrustPercent);

        if (wheels != null)
        {
            foreach (var wheel in wheels)
            {
                wheel.brakeTorque = brakesTorque;
                // Small torque to wake up wheel collider
                wheel.motorTorque = 0.01f;
            }
        }

        //float dt = Time.fixedDeltaTime;
        //UpdateWeaponCooldown(dt);
        //UpdateCannon();
    }

    public void AttackTarget()
    {
        if (shouldAttackTarget && attackTarget != null && !attackTarget.Dead)
        {
            GoToTargetSub(attackTarget.transform);

            float distanceToTarget = Vector3.Distance(transform.position, attackTarget.transform.position);

            if (distanceToTarget <= attackRange && IsTargetInFront() && !attackTarget.Dead)
            {
                airplaneConfig.cannonFiring = true;
            }
            else
            {
                airplaneConfig.cannonFiring = false;
            }


            if (attackTarget.Dead)
            {
                shouldAttackTarget = false;
                shouldMaintainAltitude = true;
                airplaneConfig.cannonFiring = false;
            }
        }
    }

    // Check if the target is in front of the airplane
    private bool IsTargetInFront()
    {
        // Calculate the direction vector from the airplane to the target
        Vector3 directionToTarget = (attackTarget.transform.position - transform.position).normalized;

        // Check the angle between the airplane's forward direction and the target direction
        float angle = Vector3.Angle(transform.forward, directionToTarget);

        // If the angle is less than the field of view (target is in front), return true
        return angle <= attackFieldOfView;
    }

    public void GoToTarget(Transform target)
    {
        if (shouldGoToTarget && target != null)
        {
            GoToTargetSub(target);
        }
        else if (target == null)
        {
            Debug.LogWarning("Target is not set, but 'shouldGoToTarget' is true.");
        }
    }

    private void GoToTargetSub(Transform target)
    {
        // Calculate the direction to the target
        Vector3 targetDirection = (target.position - transform.position).normalized;
        Vector3 localTargetDirection = transform.InverseTransformDirection(targetDirection);

        if (NeedsToBank(target))
        {
            Pitch = 0;
            Yaw = 0;
        }

        // Calculate target pitch (nose up/down) and yaw (left/right)
        float targetPitch = Mathf.Atan2(-localTargetDirection.y, localTargetDirection.z);
        float targetYaw = Mathf.Atan2(localTargetDirection.x, localTargetDirection.z);

        // Set pitch and yaw to guide the aircraft toward the target
        Pitch = Mathf.Clamp(targetPitch, -1f, 1f);
        Yaw = Mathf.Clamp(targetYaw, -1f, 1f);

        // Roll Options
        // Correct roll to stay right side up
        if(CheckUpsideDown())
        {
            Vector3 localUp = transform.InverseTransformDirection(Vector3.up); // Local "up" vector
            float rollCorrection = Mathf.Atan2(localUp.x, localUp.z); // Correct for "tilt"
            rollCorrection *= rollSensitivity;
            Roll = Mathf.Clamp(rollCorrection, -1f, 1f);
        }

        // Check if the target has been reached
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget <= acceptanceRadius)
        {
            Debug.Log("Target reached.");
            shouldGoToTarget = false; // Stop navigating toward the target
            Pitch = 0;
            Yaw = 0;
            Roll = 0;
        }
    }

    [SerializeField] private float bankAngle = 30f;
    [SerializeField] private float rollSensitivity = 0.1f;
    [SerializeField] private float pitchCompensationFactor = 0.1f;
    private bool NeedsToBank(Transform target)
    {
        // Define the cone angle (in degrees). The wider the angle, the less banking is needed.
        float coneAngle = bankAngle;

        // Get the direction to the target in world space
        Vector3 targetDirection = (target.position - transform.position).normalized;

        // Get the forward direction of the aircraft
        Vector3 forwardDirection = transform.forward;

        // Compute the angle between the aircraft's forward direction and the target direction
        float angleToTarget = Vector3.Angle(forwardDirection, targetDirection);

        // If the target is inside the cone, no need to bank
        if (angleToTarget <= coneAngle)
        {
            Roll = 0;
            return false;
        }

        // Determine whether to bank left or right
        Vector3 rightDirection = transform.right;
        float sideDot = Vector3.Dot(targetDirection, rightDirection);

        // Normalize roll amount based on how far the target is outside the cone
        float excessAngle = angleToTarget - bankAngle;
        float rollAmount = Mathf.Clamp01(excessAngle / (90f - bankAngle)) * 1; // Scale roll between 0 and maxRoll
        rollAmount *= rollSensitivity;

        // Apply roll in the correct direction
        Roll = sideDot > 0 ? rollAmount : -rollAmount;

        //// ---- Pitch Compensation for Angled Banking ----
        //Vector3 upDirection = transform.up;
        //float verticalOffset = Vector3.Dot(targetDirection, upDirection); // How much target is above/below
        //verticalOffset *= pitchCompensationFactor;

        //// Adjust pitch based on vertical offset
        //Pitch = Mathf.Clamp(-verticalOffset, -1f, 1f);

        return true;
    }

    private bool CheckUpsideDown()
    {
        // Get the aircraft's "up" vector
        Vector3 localUp = transform.up;

        // Compare it with the world's up vector
        float dot = Vector3.Dot(localUp, Vector3.up);

        // If the dot product is negative, the aircraft is upside down
        return dot < 0;
    }

    private int currentWaypointIndex = 0; // Tracks the current waypoint in the patrol
    private void Patrol()
    {
        if (shouldPatrol && patrolWaypoints != null && patrolWaypoints.Count > 0)
        {
            // Get the current waypoint
            Transform currentWaypoint = patrolWaypoints[currentWaypointIndex];

            if (currentWaypoint != null)
            {
                // Navigate toward the current waypoint
                GoToTargetSub(currentWaypoint);

                // Check if the airplane has reached the current waypoint
                float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint.position);
                if (distanceToWaypoint <= acceptanceRadius)
                {
                    // Move to the next waypoint
                    currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Count;
                    Debug.Log("Reached waypoint. Moving to next waypoint: " + currentWaypointIndex);
                }
            }
            else
            {
                Debug.LogWarning("Current waypoint is null. Skipping to next waypoint.");
                currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Count;
            }
        }
        else if (shouldPatrol && (patrolWaypoints == null || patrolWaypoints.Count == 0))
        {
            Debug.LogWarning("Patrol is enabled, but no waypoints are assigned.");
        }
    }

    public void MaintainAltitude()
    {
        if (shouldMaintainAltitude)
        {
            if (desiredAltitude < 0)
                desiredAltitude = currentAltitude;

            // Calculate altitude error
            float altitudeError = desiredAltitude - currentAltitude;

            // Check if altitude is within acceptance radius
            AdjustForAltitude(altitudeError);
        }
    }
    private void AdjustForAltitude(float altitudeError)
    {
        // Direct adjustment to altitude when out of bounds (larger, more immediate corrective action)
        float adjustmentFactor = -altitudeError * altitudeMaintainSensitivity;
        Pitch = Mathf.Clamp(adjustmentFactor, -1f, 1f);
    }

    public void SetControlSurfacesAngles(float pitch, float roll, float yaw, float flap)
    {
        if (controlSurfaces != null)
        {
            foreach (var surface in controlSurfaces)
            {
                if (surface == null || !surface.IsControlSurface) continue;
                switch (surface.InputType)
                {
                    case ControlInputType.Pitch:
                        surface.SetFlapAngle(pitch * pitchControlSensitivity * surface.InputMultiplyer);
                        break;
                    case ControlInputType.Roll:
                        surface.SetFlapAngle(roll * rollControlSensitivity * surface.InputMultiplyer);
                        break;
                    case ControlInputType.Yaw:
                        surface.SetFlapAngle(yaw * yawControlSensitivity * surface.InputMultiplyer);
                        break;
                    case ControlInputType.Flap:
                        surface.SetFlapAngle(Flap * surface.InputMultiplyer);
                        break;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(target.position, acceptanceRadius);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectonRadius);

        // Debug draw the banking cone
        DrawBankingCone(bankAngle, detectonRadius); // Adjust angle and length as needed

        if (!Application.isPlaying)
        {
            SetControlSurfacesAngles(Pitch, Roll, Yaw, Flap);
        }
    }

    private void DrawBankingCone(float angle, float length)
    {
        Gizmos.color = Color.green;
        Vector3 forward = transform.forward * length;

        // Create a set of directions for the cone edges
        Vector3 up = Quaternion.AngleAxis(angle, transform.right) * forward;
        Vector3 down = Quaternion.AngleAxis(-angle, transform.right) * forward;
        Vector3 left = Quaternion.AngleAxis(-angle, transform.up) * forward;
        Vector3 right = Quaternion.AngleAxis(angle, transform.up) * forward;

        // Draw lines representing the cone
        Gizmos.DrawLine(transform.position, transform.position + up);
        Gizmos.DrawLine(transform.position, transform.position + down);
        Gizmos.DrawLine(transform.position, transform.position + left);
        Gizmos.DrawLine(transform.position, transform.position + right);

        // Draw the base of the cone as a rough circle
        int segments = 20;
        float step = 360f / segments;
        Vector3 previousPoint = transform.position + Quaternion.AngleAxis(0, transform.forward) * left;

        for (int i = 1; i <= segments; i++)
        {
            float angleStep = step * i;
            Vector3 newPoint = transform.position + Quaternion.AngleAxis(angleStep, transform.forward) * left;
            Gizmos.DrawLine(previousPoint, newPoint);
            previousPoint = newPoint;
        }
    }
}
