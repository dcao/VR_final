using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// This script provides an API for adding text to the console
public class ConsoleController : MonoBehaviour
{
    TextMeshProUGUI text;

    void Awake()
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
        bool closing = false;
        bool pushed = false;

        string curTag = "";
        Stack<string> tags = new Stack<string>();

        for (int i = 0; i < line.Length; i++) {
            built = string.Concat(built, line[i]);

            if (line[i] == '<') {
                tagOpen = true;
                continue;
            }

            if (tagOpen) {
                if (line[i] == '>') {
                    // If it was a closing tag, pop off the stack
                    if (closing) {
                        tags.Pop();
                    } else if (!pushed) {
                        tags.Push(new string(curTag));
                    }

                    // Reset state
                    tagOpen = false;
                    closing = false;
                    pushed = false;
                    curTag = "";
                } else {
                    // The branch for if we need to continue

                    if (line[i] == '=') {
                        // Push to stack and reset curTag
                        pushed = true;
                        tags.Push(new string(curTag));
                    } else if (line[i] == '/') {
                        // Record that this is a closing tag
                        closing = true;
                    } else if (!closing) {
                        curTag += line[i];
                    }

                    continue;
                }
            }

            string preLine = new string(built);
            foreach (string c in tags) {
                preLine += "</" + c + ">";
            }

            text.text = string.Concat(string.Concat(preLine, "\n"), prev);
            if (cps != 0.0f) {
                yield return new WaitForSeconds(1.0f / cps);
            }
        }
    }
}
