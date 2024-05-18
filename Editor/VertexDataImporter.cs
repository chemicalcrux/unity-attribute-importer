using System.Collections.Generic;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
using System.Linq;

namespace ChemicalCrux.UVImporter
{
    [System.Serializable]
    public class AttributeTarget
    {
        public enum Kind
        {
            None,
            UV,
            VertexColor
        }

        public Kind kind;

        public UVTarget uvTarget;
        public VertexColorTarget vertexColorTarget;
    }

    [ScriptedImporter(7, "uv")]
    public class VertexDataImporter : ScriptedImporter
    {
        [System.Serializable]
        public class ObjectConfig
        {
            [HideInInspector] public string name;
        }

        [System.Serializable]
        public class AttributeConfig
        {
            [HideInInspector] public string name;
            [HideInInspector, SerializeField] internal bool exists;

            public List<AttributeTarget> targets;
        }

        [SerializeField, HideInInspector] internal List<string> objectNames;
        [SerializeField, HideInInspector] internal List<string> attributeNames;

        [SerializeField] internal List<AttributeConfig> attributeConfigs;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            objectNames.Clear();
            HashSet<string> attributeNameSet = new();

            using var stream = File.Open(ctx.assetPath, FileMode.Open);
            using var reader = new BinaryReader(stream);
            DataParser parser = new(reader);

            parser.ReadHeader();

            for (int objectIndex = 0; objectIndex < parser.NumObjects; ++objectIndex)
            {
                parser.ReadObjectHeader();

                objectNames.Add(parser.ObjectName);

                for (int recordIndex = 0; recordIndex < parser.NumRecords; ++recordIndex)
                {
                    parser.ReadRecordHeader();

                    attributeNameSet.Add(parser.AttributeName);

                    parser.SkipRecordData();
                }
            }

            attributeNames.Clear();
            attributeNames.AddRange(attributeNameSet);

            UpdateConfigs();

            VertexMetadata metadata = ScriptableObject.CreateInstance<VertexMetadata>();
            metadata.Setup(this);

            ctx.AddObjectToAsset("Vertex Metadata", metadata);
            ctx.SetMainObject(metadata);
        }

        void UpdateConfigs()
        {
            foreach (var attributeName in attributeNames)
            {
                AttributeConfig config = attributeConfigs.FirstOrDefault(config => config.name == attributeName);

                if (config == null)
                {
                    config = new()
                    {
                        name = attributeName,
                        targets = new()
                    };

                    attributeConfigs.Add(config);
                }

                // TODO variable number of dimensinos
                if (config.targets.Count != 4)
                {
                    for (int i = 0; i < 4; ++i)
                    {
                        AttributeTarget target = new()
                        {
                            kind = AttributeTarget.Kind.UV,
                            uvTarget = new()
                            {
                                channel = UVChannel.UV1,
                                component = (UVComponent)i
                            }
                        };

                        config.targets.Add(target);
                    }
                }
            }

            foreach (var config in attributeConfigs)
            {
                config.exists = attributeNames.Contains(config.name);
            }
        }
    }
}
