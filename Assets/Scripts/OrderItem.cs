using System;
using UnityEngine;

public class OrderItem : MonoBehaviour
{
  
    public void Order(string value)
    {

        string itemString = value;

        Debug.Log(value);
         if (string.IsNullOrEmpty(itemString)) return;


         foreach (Transform child in transform) // iterate through all children of the gameObject.
         {
             if (child.name.IndexOf(itemString, StringComparison.OrdinalIgnoreCase) != -1) // if the name exists
             {
                 child.gameObject.SetActive(true);
             }
         }
    }
}
