using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AirplaneConfig))]
public class AirplaneController : MonoBehaviour
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

    [Range(-1, 1)]
    public float Pitch;
    [Range(-1, 1)]
    public float Yaw;
    [Range(-1, 1)]
    public float Roll;
    [Range(0, 1)]
    public float Flap;
    [SerializeField]
    TextMeshProUGUI displayText = null;

    float thrustPercent;
    float brakesTorque;

    AircraftPhysics aircraftPhysics;
    Rigidbody rb;

    [SerializeField] Rotator propeller;

    AirplaneConfig airplaneConfig;

    public void SetThrustPercent(float percent)
    {
        thrustPercent = percent;
    }

    private void Start()
    {
        airplaneConfig = GetComponent<AirplaneConfig>();
        aircraftPhysics = GetComponent<AircraftPhysics>();
        rb = GetComponent<Rigidbody>();
    }

    public Rigidbody GetRigidBody()
    {
        return rb;
    }

    private void Update()
    {

        if (displayText != null)
        {
            displayText.text = "V: " + ((int)rb.velocity.magnitude).ToString("D3") + " m/s\n";
            displayText.text += "A: " + ((int)transform.position.y).ToString("D4") + " m\n";
            displayText.text += "T: " + (int)(thrustPercent * 100) + "%\n";
            displayText.text += brakesTorque > 0 ? "B: ON" : "B: OFF";
        }
        else
        {
            Debug.Log("Please assign DisplayText");
        }

        if (airplaneConfig.Dead) return;

        Pitch = Input.GetAxis("Vertical");
        Roll = Input.GetAxis("Horizontal");
        Yaw = Input.GetAxis("Yaw");

        //Debug.Log("Pitch is: " + Pitch + ", Roll is: " + Roll + ", Yaw is: " + Yaw);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            thrustPercent = thrustPercent > 0 ? 0 : 1f;
        }
        if(propeller != null)
        {
            propeller.speed = thrustPercent * 1500f;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            Flap = Flap > 0 ? 0 : 0.3f;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            brakesTorque = brakesTorque > 0 ? 0 : 100f;
        }

        if(Input.GetKey(KeyCode.Mouse0))
        {
            airplaneConfig.cannonFiring = true;
        }
        else
        {
            airplaneConfig.cannonFiring = false;
        }
    }

    private void FixedUpdate()
    {
        if (airplaneConfig.Dead) return;

        SetControlSurfecesAngles(Pitch, Roll, Yaw, Flap);
        aircraftPhysics.SetThrustPercent(thrustPercent);
        if(wheels != null)
        {
            foreach (var wheel in wheels)
            {
                wheel.brakeTorque = brakesTorque;
                // small torque to wake up wheel collider
                wheel.motorTorque = 0.01f;
            }
        }

        //float dt = Time.fixedDeltaTime;
        //UpdateWeaponCooldown(dt);
        //UpdateCannon();
    }

    public void SetControlSurfecesAngles(float pitch, float roll, float yaw, float flap)
    {
        if(controlSurfaces != null)
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
        if (!Application.isPlaying)
            SetControlSurfecesAngles(Pitch, Roll, Yaw, Flap);
    }
}
