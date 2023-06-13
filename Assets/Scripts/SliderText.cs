using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[ExecuteInEditMode]
public class SliderText : MonoBehaviour
{
    public string prefix = "";
    public string suffix = "";

    TextMeshProUGUI textUI;

    // Start is called before the first frame update
    void Awake()
    {
        textUI = GetComponent<TextMeshProUGUI>();
    }

    public void UpdateText(float val) {
        textUI.text = prefix + val + suffix;
    }
}
