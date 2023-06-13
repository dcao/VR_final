using System;
using UnityEngine;
using Meta.WitAi.Json;
using Meta.WitAi.Data.Entities;
using TMPro;

namespace Meta.WitAi.Composer.Samples
{
    public class OrderFlow : MonoBehaviour
    {
        public GameObject console;
        public GameObject status;
        float cps = 80.0f;

        private string inputID = "foods";
        private string entityID = "food";
        TextMeshProUGUI statusText;
        ConsoleController consoleCtrl;

        void Awake() {
            consoleCtrl = console.GetComponent<ConsoleController>();
            statusText = status.GetComponent<TextMeshProUGUI>();
        }

        public void SetCps(float val) {
            cps = val;
        }

        public void OnError(string a, string b) {
            consoleCtrl.AddLine("<color=red>Error: </color>" + a + ": " + b);
            // TODO: Terminate early?
        }

        // Called whenever the user speaks
        public void OnSpeak(string line) {
            consoleCtrl.AddLine("User: <color=green>" + line + "</color>");
        }

        // Called whenever a response is returned.
        public void OnResponse(ComposerSessionData sessionData) {
            consoleCtrl.AddLineCharwise("<b>Cashier</b>: " + sessionData.responseData.responsePhrase, cps);
        }

        public void OnEnd(ComposerSessionData sessionData) {
            consoleCtrl.AddLine("Conversation complete.");
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

            WitResponseArray foodArrayArray = sessionData.contextMap.Data[inputID].AsArray;

            bool foundFood = false;

            for(int i = 0; i < foodArrayArray?.Count; i++) {
                var foodArray = foodArrayArray[i];
                for(int j = 0; j < foodArray?.Count; j++) {
                    WitEntityData foodEntity = foodArray[j].AsWitEntity();
                    foundFood = foundFood || GetFood(foodEntity);
                }
            }

            if (!foundFood) {
                string target = transform.parent.gameObject.GetComponent<GameController>().goalFood;
                consoleCtrl.AddLineCharwise("<color=purple>Try again!</color> Your goal is " + target, cps);
            }
        }

        private bool GetFood(WitEntityData foodEntity)
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

                    string target = transform.parent.gameObject.GetComponent<GameController>().goalFood;
                    if (child.name == target) {
                        consoleCtrl.AddLineCharwise("<color=green>Success!</color> Hit Restart to try again.", cps);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
