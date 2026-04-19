using UnityEngine;

public class Rotator : MonoBehaviour
{
    public float speed;
    private AirplaneConfig config;

    private void Awake()
    {
        config = gameObject.GetComponentInParent<AirplaneConfig>();
    }

    private void Update()
    {
        if(config != null)
        {
            if (config.Dead)
            {
                speed = 0;
            }
        }
        transform.localRotation *= Quaternion.AngleAxis(speed * Time.deltaTime , Vector3.up);
    }
}
