using System;
using UnityEngine;
using Meta.WitAi.Json;
using Meta.WitAi.Data.Entities;

namespace Meta.WitAi.Composer.Samples
{
    public class OrderItem : MonoBehaviour
    {
        [SerializeField] private string entityID = "food";
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
