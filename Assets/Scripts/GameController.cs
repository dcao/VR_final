using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;   // XR support
using UnityEngine.Assertions;

// Our game is a state machine that progresses in four stages:
enum GameState {
    // First, you're standing in line.
    InLine,
    // Next, you're prompted to speak into the mic to order something.
    Ordering,
    // You're asked for any adjustments
    Adjustments,
    // Is that ok? If yes, we're good. Otherwise, go back to Ordering.
    Confirm,
}

// The core game controller. In charge of maintaining game state.
public class GameController : MonoBehaviour
{

    // Params
    public Camera vrCam;
    public GameObject console;
    public GameObject customerPrefab;
    public GameObject chessPrefab;    // To indicate the teleport destination
    public LineRenderer rRayRenderer;

    // Consts
    public const int customerCount = 4;
    public const float waitPerCustomer = 10.0f;
    public readonly Vector3 xfOffset = new Vector3(-3.0f, 0.0f, 0.0f);
    public const float moveTime = 1.5f;
    private const float maxDistance = 10.0f;

    // State
    ConsoleController consoleCtrl;
    List<GameObject> customers;

    // private:
    private GameObject chess;
    private bool useVR;
    private Ray rRay;
    private float step;  // sensitivity of movement

    // Start is called before the first frame update
    void Start()
    {
        useVR = XRSettings.isDeviceActive;
        Debug.Log(string.Format("VR device (headset + controller) is detected: {0}", useVR));
        rRayRenderer = GetComponent<LineRenderer>();

        chess = Instantiate(chessPrefab);
        chess.SetActive(false);  // invisible by default

        // First, set the VR camera's transform to this current transform.
        vrCam.transform.position = transform.position;
        vrCam.transform.rotation = transform.rotation;

        consoleCtrl = console.GetComponent<ConsoleController>();
        customers = new List<GameObject>();

        // Spawn the other customers
        for (int i = 1; i <= customerCount - 1; i++) {
            customers.Add(Instantiate(customerPrefab, transform.position + new Vector3(0.0f, -1.5f, 0.0f) + i * xfOffset, Quaternion.identity));
        }

        // We need this line to make the line movement work, not 100% sure why
        customers.Reverse();

        // Begin the line movement procedure
        StartCoroutine(LineMovement());
    }

    // Update is called once per frame
    void Update()
    {
        step = 5.0f * Time.deltaTime;

        updateRightRay();
        moveAround();
        teleport();
    }


    IEnumerator LineMovement() {
        for (int i = 0; i < customerCount - 1; i++) {
            // Log status.
            consoleCtrl.AddLineCharwise("Line status: <b><color=orange>position " + (customerCount - 1 - i) + "</color></b>", 80);

            // First, wait for the waitPerCustomer.
            yield return new WaitForSeconds(waitPerCustomer);

            // Get the ith customer and despawn them
            Destroy(customers[0]);
            customers.RemoveAt(0);

            // Move the rest of the customers up
            for (int j = 0; j < customers.Count; j++) {
                yield return StartCoroutine(LerpPos(customers[j], xfOffset, moveTime));
            }
        }
    }

    IEnumerator LerpPos(GameObject obj, Vector3 delta, float time)
    {
       Vector3 start = obj.transform.position;
       Vector3 target = start + delta;

       float elapsedTime = 0;

       while (elapsedTime < time)
       {
           obj.transform.position = Vector3.Lerp(start, target, (elapsedTime / time));
           elapsedTime += Time.deltaTime;
           yield return null;
       }

       // At the end of this, set the game state.
       // TODO: do we want to do state machine style or just coroutine function call?
    }


    // helper function: shoot a Ray from right-hand (Secondary) controller
    // it updates rControllerRay
    // Only available in VR mode
    void updateRightRay() {
        if (useVR) {
            // Get controller position and rotation
            Vector3 controllerPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
            Quaternion controllerRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);

            // Calculate ray direction
            Vector3 rayDirection = controllerRotation * Vector3.forward;

            // Update the global ray's position and direction
            // rRay.origin = eyeCamera.transform.position + new Vector3(0.25f, -0.25f, 0.25f);
            rRay.origin = vrCam.transform.position + controllerPosition;
            rRay.direction = rayDirection;
        } else {
            rRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        }

        // Set the line renderer's positions to match the ray
        rRayRenderer.SetPosition(0, rRay.origin);
        rRayRenderer.SetPosition(1, rRay.origin + rRay.direction * maxDistance);
    }


    void moveAround() {
        Vector3 forward = vrCam.transform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = Quaternion.Euler(new Vector3(0, 90, 0)) * forward;
        Vector3 deltaPos;

        // Grab user input, which is position change in local space
        if (useVR) {
            // returns a Vector2 of the primary (Left) thumbstick’s current state.
            // (X/Y range of -1.0f to 1.0f)
            Vector2 deltaXY = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
            deltaPos = (forward * deltaXY.y + right * deltaXY.x) * step;

        } else { // use arrow keys
            // Get the horizontal and vertical axis input
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");

            deltaPos = (forward * verticalInput + right * horizontalInput) * step;
        }

        vrCam.transform.position += deltaPos;
    }


    void teleport() {
        if (!useVR) {return;}

        // Perform raycast
        RaycastHit hit;
        if (Physics.Raycast(rRay, out hit, maxDistance))
        {
            // Debug log information about the hit object
            Debug.Log("Hit object: " + hit.collider.gameObject.name);
            Debug.Log("Hit point: " + hit.point);
            Debug.Log("Hit normal: " + hit.normal);

            if (true/* Mathf.Abs(hit.point.y) < 1e-2 */) { // close to ground, can teleport
                // draw a chess to indicate teleport destination
                if (OVRInput.GetDown(OVRInput.Button.Four)) {
                    chess.transform.position = new Vector3(hit.point.x, chessY, hit.point.z);
                    chess.transform.rotation = Quaternion.identity;
                    chess.SetActive(true);
                }
                // teleport if user release left-hand Y button
                if (OVRInput.GetUp(OVRInput.Button.Four)) {
                    vrCam.transform.position = new Vector3(hit.point.x, eyeY, hit.point.z);
                    // hide chess indicator after transform
                    chess.SetActive(false);
                }
            }
        }
        return;
    }


    // Begin the main interaction loop
    void BeginInteraction() {}
}
