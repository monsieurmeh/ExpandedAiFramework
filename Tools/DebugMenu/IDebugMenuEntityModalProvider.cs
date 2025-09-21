using UnityEngine;
using System.Collections.Generic;

namespace ExpandedAiFramework.DebugMenu
{
    /// <summary>
    /// Interface for tab providers to implement custom entity modal population
    /// </summary>
    public interface IDebugMenuEntityModalProvider
    {
        /// <summary>
        /// Populates the entity modal with custom form fields for the given entity
        /// </summary>
        /// <param name="entity">The entity to create form fields for</param>
        /// <param name="modalContent">The parent GameObject to add form fields to</param>
        /// <param name="onValueChanged">Callback for when field values change</param>
        void PopulateEntityModal<T>(T entity, GameObject modalContent, System.Action<string, object> onValueChanged) where T : ISerializedData;
        
        /// <summary>
        /// Applies changes from the modal form fields back to the entity
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="fieldValues">Dictionary of field names to their new values</param>
        /// <returns>True if the entity was successfully updated</returns>
        bool ApplyEntityChanges<T>(T entity, Dictionary<string, object> fieldValues) where T : ISerializedData;
        
        /// <summary>
        /// Gets the display title for the entity modal
        /// </summary>
        /// <param name="entity">The entity to get the title for</param>
        /// <returns>Modal title string</returns>
        string GetEntityModalTitle<T>(T entity) where T : ISerializedData;
    }
}
