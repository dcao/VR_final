using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // Consts
    public const int customerCount = 4;
    public const float waitPerCustomer = 10.0f;
    public readonly Vector3 xfOffset = new Vector3(-3.0f, 0.0f, 0.0f);
    public const float moveTime = 1.5f;

    // State
    ConsoleController consoleCtrl;
    List<GameObject> customers;

    // Start is called before the first frame update
    void Start()
    {
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

    // Update is called once per frame
    void Update()
    {
    }

    // Begin the main interaction loop
    void BeginInteraction() {}
}
