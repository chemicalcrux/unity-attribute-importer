using System.Collections.Generic;
using UnityEngine;

namespace ChemicalCrux.AttributeImporter
{
    /// <summary>
    /// Configuration data for a single attribute.
    /// </summary>
    [System.Serializable]
    public class AttributeConfig
    {
        [HideInInspector] public string name;
        [HideInInspector, SerializeField] internal bool exists;

        public List<AttributeTarget> targets;
    }
}
