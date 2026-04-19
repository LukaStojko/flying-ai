using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SpitfireAnimatonController : MonoBehaviour
{
    private Animator _animator;

    [SerializeField]
    bool landingGearWorks = true;
    [SerializeField]
    bool landingGearOut = false;



    [SerializeField]
    Rigidbody aircraftRigidbody;

    void Start()
    {
        // Reference the Animator component attached to the plane
        _animator = GetComponent<Animator>();

        if (landingGearWorks && landingGearOut)
        {
            SpawnWithLandingGearOut();
        }

        if (aircraftRigidbody == null)
        {
            Debug.LogError("Rigidbody for the aircraft is not assigned.");
        }
    }

    void Update()
    {
        if(landingGearWorks)
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                landingGearOut = !landingGearOut;
            }

            DeployLandingGear();
        }
    }

    public void DeployLandingGear()
    {
        // Activate the DeployLandingGear trigger
        if(landingGearWorks)
        {
            _animator.SetBool("DeployLandingGear", landingGearOut);
        }
    }

    private void SpawnWithLandingGearOut()
    {
        _animator.Play("Deploy Landing Gear", 0, 1f); // Skip to the end of the deploy state
    }
}
