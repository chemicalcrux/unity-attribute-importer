using System.Collections.Generic;
using ChemicalCrux.AttributeImporter.Transformers;
using NUnit.Framework.Constraints;
using UnityEngine;

namespace ChemicalCrux.AttributeImporter
{
    public class AttributeMetadata : ScriptableObject
    {
        public List<DataImporter.AttributeConfig> attributeConfigs;

        public void Setup(DataImporter importer)
        {
            attributeConfigs = new();

            foreach (var info in importer.activeAttributeInfo)
            {
                var config = importer.attributeConfigs.Find(config => config.name == info.name);

                if (Settings.instance.LogDebug)
                    Debug.Log(info.name + ": " + config);

                if (config != null)
                    attributeConfigs.Add(config);
            }
        }

    }
}