using System;
using UnityEngine;
using Meta.WitAi.Json;
using Meta.WitAi.Data.Entities;

namespace Meta.WitAi.Composer.Samples
{
    public class OrderFlow : MonoBehaviour
    {
        public GameObject console;
        float cps = 80.0f;

        [SerializeField] string entityID = "food";
        ConsoleController consoleCtrl;

        void Awake() {
            consoleCtrl = console.GetComponent<ConsoleController>();
        }

        public void SetCps(float val) {
            cps = val;
        }

        // Called whenever the user speaks
        public void OnSpeak(string line) {
            consoleCtrl.AddLineCharwise("User: <color=green>" + line + "</color>", cps);
        }

        // Called whenever a response is returned.
        public void OnResponse(ComposerSessionData sessionData) {
            string error = sessionData.responseData.error;
            string response = sessionData.responseData.responsePhrase;

            if (error != "") {
                // TODO: Terminate early?
                consoleCtrl.AddLine("<color=red>Error: </color>" + error);
            } else {
                consoleCtrl.AddLineCharwise("<b>Cashier</b>: " + response, cps);
            }
        }

        public void OrderFood(ComposerSessionData sessionData)
        {
            // Check context map
            if (sessionData.contextMap == null || sessionData.contextMap.Data == null)
            {
                VLog.E("Order Item Action Failed - No Context Map");
                return;
            }
            if (!sessionData.contextMap.Data.HasChild(entityID))
            {
                VLog.E($"Order Item Action Failed - Context map does not contain {entityID}");
                return;
            }

            // Get color name from context map
            WitResponseArray foodArray = sessionData.contextMap.Data[entityID].AsArray;

            for(int i = 0; i < foodArray?.Count; i++) {
                WitEntityData foodEntity = foodArray[i].AsWitEntity();
                GetFood(foodEntity);
            }
        }

        private void GetFood(WitEntityData foodEntity)
        {
            string foodName = foodEntity?.value;
            if (string.IsNullOrEmpty(foodName))
            {
                VLog.E($"Order Item Action Failed - No {entityID} value found");
            }

            Debug.Log(foodName);

            foreach (Transform child in transform) // iterate through all children of the gameObject.
            {
                if (child.name.IndexOf(foodName, StringComparison.OrdinalIgnoreCase) != -1) // if the name exists
                {
                    // found matching object
                    child.gameObject.SetActive(true);
                }
            }

        }
    }
}
