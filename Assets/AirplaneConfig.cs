using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AircraftPhysics))]
public class AirplaneConfig : MonoBehaviour
{
    AircraftPhysics aircraftPhysics;
    Rigidbody rb;

    AirplaneController airplaneController;
    AIAirplaneController aiAirplaneController;
    [SerializeField] bool startInAir = false;

    // New serialized field for starting speed
    [SerializeField] float startingSpeed = 100f; // Default speed of 200 m/s

    [SerializeField] float desiredAltitude = -1; // -1 means no altitude control

    [SerializeField]
    float maxHealth;
    [SerializeField]
    float health;

    public float MaxHealth
    {
        get
        {
            return maxHealth;
        }
        set
        {
            maxHealth = Mathf.Max(0, value);
        }
    }

    public float Health
    {
        get
        {
            return health;
        }
        private set
        {
            health = Mathf.Clamp(value, 0, maxHealth);

            if (health == 0 && MaxHealth != 0 && !Dead)
            {
                Die();
            }
        }
    }
    public bool Dead { get; private set; }

    [Tooltip("Firing rate in Rounds Per Minute")]
    [SerializeField]
    float cannonFireRate;
    [SerializeField]
    float cannonDebounceTime;
    [SerializeField]
    float cannonSpread;
    [SerializeField]
    List<Transform> cannonSpawnPoints;
    [SerializeField]
    GameObject bulletPrefab;

    public bool cannonFiring;
    float cannonDebounceTimer;
    float cannonFiringTimer;

    public void ApplyDamage(float damage)
    {
        Health -= damage;
    }

    void Die()
    {
        Dead = true;
        cannonFiring = false;

        if (airplaneController != null)
        {
            airplaneController.SetThrustPercent(0);
        }
        if (aiAirplaneController != null)
        {
            aiAirplaneController.SetThrustPercent(0);
        }

        if (rb != null)
        {
            // Disable aerodynamic lift
            DisableAerodynamicSurfaces();

            // Enable gravity and retain drag
            rb.useGravity = true;

            // Adjust velocity to create falling motion based on current direction
            Vector3 currentVelocity = rb.velocity;
            Vector3 downwardForce = Vector3.down * rb.mass * 50f; // Emphasize downward pull

            // Add a strong downward force proportional to forward speed and align descent with current direction
            Vector3 fallingVelocity = Vector3.ProjectOnPlane(currentVelocity, Vector3.up).normalized * currentVelocity.magnitude * 0.3f;
            fallingVelocity += Vector3.down * currentVelocity.magnitude * 0.7f; // Strong downward tilt
            rb.velocity = fallingVelocity;

            // Add instability via angular velocity
            ApplyDeathPhysics();

            // Add visual instability by tilting the aircraft slightly downward
            TiltAircraftForNosedive();
        }
    }

    private void DisableAerodynamicSurfaces()
    {
        if (aircraftPhysics != null && aircraftPhysics.GetSurfaces() != null)
        {
            foreach (var surface in aircraftPhysics.GetSurfaces())
            {
                surface.enabled = false; // Disable the aerodynamic force calculations
            }
        }
    }
    private void ApplyDeathPhysics()
    {
        // Apply random torque for instability
        Vector3 randomTorque = Random.insideUnitSphere * rb.mass * 20f;
        rb.AddTorque(randomTorque, ForceMode.Impulse);

        // Add a light tumbling effect by modifying angular velocity
        rb.angularVelocity += Random.insideUnitSphere * 2f;
    }

    private void TiltAircraftForNosedive()
    {
        // Orient the plane to face downward based on current velocity
        Vector3 forwardDirection = rb.velocity.normalized; // Current flight direction
        Vector3 downwardTilt = Vector3.Lerp(forwardDirection, Vector3.down, 5f); // Bias toward falling

        // Apply rotation gradually toward the downward tilt direction
        Quaternion targetRotation = Quaternion.LookRotation(downwardTilt, Vector3.up);
        rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.deltaTime * 5f); // Smooth tilt
    }

    public Rigidbody GetRigidBody()
    {
        return rb;
    }

    void Awake()
    {
    }

    // Start is called before the first frame update
    void Start()
    {
        aircraftPhysics = GetComponent<AircraftPhysics>();
        airplaneController = GetComponent<AirplaneController>();
        aiAirplaneController = GetComponent<AIAirplaneController>();
        rb = GetComponent<Rigidbody>();

        if (startInAir)
        {
            // Set the starting velocity in the forward direction
            rb.velocity = transform.forward * startingSpeed;

            // Optionally set initial thrust
            if (airplaneController != null)
            {
                airplaneController.SetThrustPercent(1); // Full thrust for in-air control
            }
            if (aiAirplaneController != null)
            {
                aiAirplaneController.SetThrustPercent(1);
            }
        }

        if(desiredAltitude > 0)
        {
            aiAirplaneController.SetDesiredAltitude(desiredAltitude);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {
        if (Dead)
        {

            return;
        }

        float dt = Time.fixedDeltaTime;
        UpdateWeaponCooldown(dt);
        UpdateCannon();
    }

    void UpdateWeaponCooldown(float dt)
    {
        cannonDebounceTimer = Mathf.Max(0, cannonDebounceTimer - dt);
        cannonFiringTimer = Mathf.Max(0, cannonFiringTimer - dt);
    }

    private void UpdateCannon()
    {
        if (cannonFiring && cannonFiringTimer == 0)
        {
            cannonFiringTimer = 60f / cannonFireRate;

            var spread = Random.insideUnitCircle * cannonSpread;

            foreach (var cannonSpawnPoint in cannonSpawnPoints)
            {
                var bulletGO = Instantiate(bulletPrefab, cannonSpawnPoint.position, cannonSpawnPoint.rotation * Quaternion.Euler(spread.x, spread.y, 0));
                var bullet = bulletGO.GetComponent<Bullet>();
                bullet.FireAlt(this);
            }
        }
    }
}
