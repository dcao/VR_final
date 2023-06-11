using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The core game controller. In charge of maintaining game state.
public class GameController : MonoBehaviour
{

    public Camera vrCam;
    public GameObject customerPrefab;

    public const int customers = 3;

    int index = customers - 1;

    // Start is called before the first frame update
    void Start()
    {
        // First, set the VR camera's transform to this current transform.
        vrCam.transform.position = transform.position;
        vrCam.transform.rotation = transform.rotation;

        // Spawn the other customers
        for (int i = 1; i <= customers - 1; i++) {
            Instantiate(customerPrefab, transform.position + new Vector3(0.0f, -1.5f, 0.0f) + i * new Vector3(-3.0f, 0.0f, 0.0f), Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}
