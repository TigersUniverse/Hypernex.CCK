using System;
using UnityEngine;

namespace Hypernex.CCK.Unity
{
    [Serializable]
    public class AssetIdentifier : MonoBehaviour
    {
        [SerializeField] private string Id;

        /// <summary>
        /// Sets the ID of the Asset Identifier. DO NOT TOUCH! This should be assigned by whatever gets AvatarMeta
        /// </summary>
        /// <param name="id">The Id of the Asset</param>
        public void SetId(string id) => Id = id;

        /// <summary>
        /// Gets the Id of the Asset
        /// </summary>
        /// <returns>The Id of the Asset</returns>
        public string GetId() => Id;
    }
}