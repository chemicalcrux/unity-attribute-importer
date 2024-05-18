using System.Collections.Generic;
using UnityEngine;

namespace ChemicalCrux.UVImporter
{
    public class VertexMetadata : ScriptableObject
    {
        public class Data
        {
            public string attribute;
            public AttributeTarget target;
        }

        public List<VertexDataImporter.AttributeConfig> attributeConfigs;

        public void Setup(VertexDataImporter importer)
        {
            attributeConfigs = new();

            foreach (var attributeName in importer.attributeNames)
            {
                var config = importer.attributeConfigs.Find(config => config.name == attributeName);

                Debug.Log(attributeName + ": " + config);
                if (config != null)
                    attributeConfigs.Add(config);
            }
        }

    }
}