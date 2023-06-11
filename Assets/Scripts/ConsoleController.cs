using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// This script provides an API for adding text to the console
public class ConsoleController : MonoBehaviour
{
    TextMeshProUGUI text;

    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();

        text.text = "";
    }

    public void AddLine(string line) {
        // TODO: this is lw inefficient but meh
        AddLineCharwise(line, 0.0f);
    }

    public void AddLineCharwise(string line, float cps) {
        StartCoroutine(BuildLine(line, cps));
    }

    // Add a new line to the console, but add it character-by-character, to emulate dialogue.
    public IEnumerator BuildLine(string line, float cps) {
        string prev = text.text;
        string built = "";
        bool tagOpen = false;

        for (int i = 0; i < line.Length; i++) {
            built = string.Concat(built, line[i]);

            if (line[i] == '<' || (tagOpen && line[i] != '>')) {
                tagOpen = true;
                continue;
            } else {
                tagOpen = false;
            }

            text.text = string.Concat(string.Concat(built, "\n"), prev);
            if (cps != 0.0f) {
                yield return new WaitForSeconds(1.0f / cps);
            }
        }
    }
}
