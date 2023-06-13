// Copied from our Project 2 :^)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

[System.Serializable]
public struct Gesture 
{
    public string name;
    public List<Vector3> fingerData;
    public UnityEvent onRecognized;
}

public class GestureDetector : MonoBehaviour
{
    // Public
    public OVRSkeleton skeleton;
    public List<Gesture> gestures;
    private List<OVRBone> fingerBones;
    public float threshold = 0.05f;  // hand gesture detection sensitivity

    // Private:
    public bool debugMode = true;
    // Start is called before the first frame update
    IEnumerator Start()
    {
        while (skeleton.Bones.Count == 0) {
            yield return null;
        }

        fingerBones = new List<OVRBone>(skeleton.Bones);
        Debug.Log("Check size of fingerBones data: " + fingerBones.Count);
    }

    // Update is called once per frame
    void Update()
    {
        if (debugMode && Input.GetKeyDown("r"))
        {
            Debug.Log("trying to save");
            SaveGesture();
            Debug.Log("Saved a new Gesture: " + gestures[gestures.Count-1].name);
        }
    }

    /// <summary>
    /// Save a hand gesture in the List<Gesture> gestures;
    /// </summary>
    void SaveGesture() {
        Gesture g = new Gesture();
        g.name = "New Gesture";
        List<Vector3> data = new List<Vector3>();
        foreach (var bone in fingerBones) {
            // use finger tip's local position relative to the finger root
            data.Add(skeleton.transform.InverseTransformPoint(
                bone.Transform.position
            ));
        }
        g.fingerData = data;
        gestures.Add(g);
    }


    /// <summary>
    /// Recognize user gesture in the current frame.
    /// </summary>
    /// <returns>
    /// A pre-defined gesture that matches the best in the List.
    /// If no close match, return a new Gesture();
    /// </returns>
    public Gesture Recognize() {
        Gesture currG = new Gesture();
        float currMin = Mathf.Infinity;

        if (fingerBones == null) {
            return currG;
        }

        foreach (var gesture in gestures) {
            // how likely the user gesture matches the current gesture in the List
            // depends on the total bone distance
            float sumDist = 0;
            bool toDiscard = false;
            // compare distance on each bone
            for (int i=0; i<fingerBones.Count; i++) {
                Vector3 currData = skeleton.transform.InverseTransformPoint(
                    fingerBones[i].Transform.position
                );
                float dist = Vector3.Distance(currData, gesture.fingerData[i]);
                // a particular bone is too far away
                if (dist > threshold)
                {
                    // break inner loop: stop comparing bones
                    toDiscard = true;
                    break;
                }

                sumDist += dist;
            }

            if (!toDiscard && sumDist < currMin) {
                currMin = sumDist;
                currG = gesture;
            }
        }

        return currG;
    }


}
