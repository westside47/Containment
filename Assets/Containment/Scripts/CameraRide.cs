using UnityEngine;

public class CameraRide : MonoBehaviour
{
    public float speed = 0.1f;

    void Update()
    {
        // only while not logged in yet
        if (Player.localPlayer) Destroy(this);

        // move backwards
        transform.position -= transform.forward * speed * Time.deltaTime;
    }
}
