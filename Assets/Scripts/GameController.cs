using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;   // XR support
using UnityEngine.Assertions;
using Oculus.Interaction;
using UnityEngine.Events;

[System.Serializable]
public class VoiceEvent : UnityEvent <string> {}

// The core game controller. In charge of maintaining game state.
public class GameController : MonoBehaviour
{

    // Params
    public GameObject vrCam;
    public GameObject console;
    public GameObject customerPrefab;
    public GameObject voiceExperience;
    public GameObject chessPrefab;    // To indicate the teleport destination

    public GameObject rayInteractorL;
    public GameObject rayInteractorR;

    public GameObject gdObjectL;
    public GameObject gdObjectR;

    public LineRenderer rRayRenderer;
    public LineRenderer lRayRenderer;

    public VoiceEvent Speaker;

    // Consts
    public const int customerCount = 4;
    public float waitPerCustomer = 1.5f;
    public readonly Vector3 xfOffset = new Vector3(-2.0f, 0.0f, 0.0f);
    public float moveTime = 1.5f;
    private const float maxDistance = 10.0f;
    private const float focusDistance = 0.25f;

    GestureDetector rightGD;
    GestureDetector leftGD;
    WitActivation wit;
    ConsoleController consoleCtrl;
    string prevGesture = "";

    List<GameObject> customers;
    public string goalFood;

    // private:
    private GameObject chess;
    private GameObject selected;
    private bool useVR;
    private Ray rRay;
    private Ray lRay;
    private float step;  // sensitivity of movement

    void Start()
    {
        useVR = XRSettings.isDeviceActive;
        Debug.Log(string.Format("VR device (headset + controller) is detected: {0}", useVR));

        chess = Instantiate(chessPrefab);
        chess.SetActive(false);  // invisible by default

        wit = voiceExperience.GetComponent<WitActivation>();
        consoleCtrl = console.GetComponent<ConsoleController>();
        customers = new List<GameObject>();

        rightGD = gdObjectR.GetComponent<GestureDetector>();
        leftGD = gdObjectL.GetComponent<GestureDetector>();

        Reinitialize();
    }

    public void Reinitialize() {
        StopAllCoroutines();

        foreach (GameObject c in customers) {
            Destroy(c);
        }
        customers = new List<GameObject>();
        if (wit != null) {
            wit.Deactivate();
        }

        int r = Random.Range(0, 2);
        if (r == 0) {
            goalFood = "Pizza";
        } else if (r == 1) {
            goalFood = "Cheeseburger";
        } else {
            goalFood = "Coffee";
        }

        consoleCtrl.AddLine("Initializing scenario. Goal food: <b>" + goalFood + "</b>");

        // First, set the VR camera's transform to this current transform.
        vrCam.transform.position = transform.position;
        vrCam.transform.rotation = transform.rotation;

        // Spawn the other customers
        for (int i = 1; i <= customerCount - 1; i++) {
            customers.Add(Instantiate(customerPrefab, transform.position + new Vector3(0.0f, 2.0f, 0.0f) + i * xfOffset, Quaternion.identity));
        }

        // We need this line to make the line movement work, not 100% sure why
        customers.Reverse();

        // Begin the line movement procedure
        StartCoroutine(LineMovement());
    }

    public void SetWait(float wait) {
        waitPerCustomer = wait;
    }

    public void SetMove(float move) {
        moveTime = move;
    }

    IEnumerator LineMovement() {
        for (int i = 0; i < customerCount - 1; i++) {
            // Log status.
            consoleCtrl.AddLine("Line status: <b><color=orange>position " + (customerCount - 1 - i) + "</color></b>");

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

        // Once we've gotten to this point, start the microphone procedure!
        
        Speaker.Invoke("Hello!");
        yield return new WaitForSeconds(0.75f);
        consoleCtrl.AddLine("Your turn to order! Speak your order into the mic. <color=green>Mic is enabled.</color>");
        wit.ActivateSpeaking();

        // Because of consequences with how we coded this, all order flow interactiosn are handled by OrderFlow.
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

    // Update is called once per frame
    void Update()
    {
        step = 5.0f * Time.deltaTime;

        updateRightRay();
        // updateLeftRay();

        moveAround();
        teleport();

        manipulateObjectVR();
    }

    // Object manipulation!
    void manipulateObjectVR() {
        if (!useVR) {return;}

        // Check gestures.
        Gesture rightGesture = rightGD.Recognize();

        // Next, do manip test
        RaycastHit hit;
        if (Physics.Raycast(rRay, out hit, maxDistance)) {
            if (hit.collider.gameObject.GetComponent<Rigidbody>() != null && rightGesture.name == "fist_R") {
                if (selected == null) {
                    selected = hit.collider.gameObject;
                    selected.GetComponent<Rigidbody>().isKinematic = true;
                    hit.collider.gameObject.transform.position = rRay.GetPoint(focusDistance);

                    selected.transform.rotation = Quaternion.FromToRotation(selected.transform.up, -rRay.direction);
                } else {
                    selected.transform.position = rRay.GetPoint(focusDistance);
                }
            }

            // selected.GetComponent<Highlight>()?.ToggleHighlight(true);

            // Rotation check
            // if (OVRInput.Get(OVRInput.RawButton.RHandTrigger) && OVRInput.Get(OVRInput.RawButton.LHandTrigger)) {
            //     hit.transform.Rotate(0, 0, 90.0f * Time.deltaTime);
            // } else if (OVRInput.Get(OVRInput.RawButton.RHandTrigger)) {
            //     // Scale up check
            //     float scaleFactor = 1.0f + 0.5f * Time.deltaTime;
            //     hit.transform.localScale = new Vector3(hit.transform.localScale.x * scaleFactor, hit.transform.localScale.y * scaleFactor, hit.transform.localScale.z * scaleFactor);
            // } else if (OVRInput.Get(OVRInput.RawButton.LHandTrigger)) {
            //     // Scale down check
            //     float scaleFactor = 1.0f - 0.5f * Time.deltaTime;
            //     hit.transform.localScale = new Vector3(hit.transform.localScale.x * scaleFactor, hit.transform.localScale.y * scaleFactor, hit.transform.localScale.z * scaleFactor);
            // }
        } else {
            if (selected != null) {
                selected.GetComponent<Rigidbody>().isKinematic = false;
            }

            selected = null;
        }
    }

    // helper function: shoot a Ray from right-hand (Secondary) controller
    // it updates rControllerRay
    // Only available in VR mode
    void updateRightRay() {
        if (useVR) {
            // Get controller position and rotation
            RayInteractor ri = rayInteractorR.GetComponent<RayInteractor>();
            Vector3 controllerPosition = ri.Origin;
            // Quaternion controllerRotation = ri.Rotation;

            // Calculate ray direction
            // Vector3 rayDirection = controllerRotation * ri.Forward;

            // Update the global ray's position and direction
            // rRay.origin = eyeCamera.transform.position + new Vector3(0.25f, -0.25f, 0.25f);
            rRay.origin = controllerPosition;
            rRay.direction = ri.Forward;
        } else {
            rRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        }

        // Set the line renderer's positions to match the ray
        rRayRenderer.SetPosition(0, rRay.origin);
        rRayRenderer.SetPosition(1, rRay.origin + rRay.direction * maxDistance);
    }

    void updateLeftRay() {
        if (useVR) {
            // Get controller position and rotation
            RayInteractor li = rayInteractorL.GetComponent<RayInteractor>();
            Vector3 controllerPosition = li.Origin;
            // Quaternion controllerRotation = li.Rotation;

            // Calculate ray direction
            // Vector3 rayDirection = controllerRotation * li.Forward;

            // Update the global ray's position and direction
            lRay.origin = controllerPosition;
            lRay.direction = li.Forward;
        }

        // Set the line renderer's positions to match the ray
        lRayRenderer.SetPosition(0, lRay.origin);
        lRayRenderer.SetPosition(1, lRay.origin + lRay.direction * maxDistance);
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

        // Check gestures.
        Gesture rightGesture = rightGD.Recognize();

        // Perform raycast
        RaycastHit hit;
        if (Physics.Raycast(rRay, out hit, maxDistance))
        {
            if (rightGesture.name == "thumb_R") {
                chess.transform.position = new Vector3(hit.point.x, 1.5f, hit.point.z);
                chess.transform.rotation = Quaternion.identity;
                chess.SetActive(true);
            } else if (rightGesture.name != "thumb_R" && prevGesture == "thumb_R") {
                vrCam.transform.position = new Vector3(hit.point.x, transform.position.y, hit.point.z);
                // hide chess indicator after transform
                chess.SetActive(false);
                rightGesture.name = "";
            }
        }

        prevGesture = string.Copy(rightGesture.name);
        return;
    }
}
