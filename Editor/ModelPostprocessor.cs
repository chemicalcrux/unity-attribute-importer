using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace ChemicalCrux.UVImporter
{
    public class ModelPostprocessor : AssetPostprocessor
    {
        public override uint GetVersion()
        {
            return 14;
        }

        void OnPostprocessModel(GameObject gameObject)
        {
            string path = Path.ChangeExtension(assetPath, "uv");
            var modelImporter = assetImporter as ModelImporter;

            if (!File.Exists(path))
                return;

            context.DependsOnArtifact(path);
            context.DependsOnSourceAsset(path);

            var vertexMetadata = AssetDatabase.LoadAssetAtPath<VertexMetadata>(path);

            if (!vertexMetadata)
                return;

            Dictionary<string, Mesh> meshLookup = new();
            Dictionary<Mesh, List<int>> indexTableLookup = new();

            Mesh singleMesh = null;

            // if preserveHierarchy is off and there are no children, then we lose the name from Blender
            bool useNames = modelImporter.preserveHierarchy || gameObject.transform.childCount > 0;

            if (useNames)
            {
                foreach (var meshFilter in gameObject.GetComponentsInChildren<MeshFilter>())
                {
                    if (!meshLookup.TryAdd(meshFilter.name, meshFilter.sharedMesh))
                    {
                        Debug.LogWarning(gameObject + " has a name collision: " + meshFilter.name);
                    }
                }

                foreach (var skinnedMeshRenderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (!meshLookup.TryAdd(skinnedMeshRenderer.name, skinnedMeshRenderer.sharedMesh))
                    {
                        Debug.LogWarning(gameObject + " has a name collision: " + skinnedMeshRenderer.name);
                    }
                }
            }
            else
            {
                if (gameObject.TryGetComponent(out MeshFilter meshFilter))
                    singleMesh = meshFilter.sharedMesh;
                else if (gameObject.TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
                    singleMesh = skinnedMeshRenderer.sharedMesh;
                else
                {
                    Debug.LogWarning($"Expected to find a mesh on the root of {gameObject}. Aborting import.");
                    return;
                }
            }


            using var stream = File.Open(path, FileMode.Open);
            using var reader = new BinaryReader(stream);
            DataParser parser = new(reader);

            parser.ReadHeader();

            for (int mesh = 0; mesh < parser.NumObjects; ++mesh)
            {
                parser.ReadObjectHeader();

                if (Settings.instance.Debug)
                    Debug.Log("Name: " + parser.ObjectName);

                Mesh targetMesh;

                if (!useNames)
                {
                    targetMesh = singleMesh;
                }
                else if (!meshLookup.TryGetValue(parser.ObjectName, out targetMesh))
                {
                    Debug.LogWarning($"Expected to find an object named {parser.ObjectName} on {gameObject.name}. Aborting import.");
                    return;
                }

                ReadSingleMesh(vertexMetadata, parser, targetMesh);
            }
        }

        public class IntermediateVertexData
        {
            private Dictionary<UVChannel, List<Vector4>> uvData = new();
            private List<Vector4> colorData = new();
            private int vertexCount;
            private Mesh mesh;

            private List<UVChannel> usedUVChannels = new();
            private bool usedVertexColors;

            public IntermediateVertexData(List<VertexDataImporter.AttributeConfig> attributeConfigs, List<string> usedAttributes, int vertexCount, Mesh mesh)
            {
                this.vertexCount = vertexCount;
                this.mesh = mesh;


                foreach (var config in attributeConfigs)
                {
                    if (usedAttributes.All(attribute => attribute != config.name))
                    {
                        if (Settings.instance.debug)
                            Debug.Log($"Skipping {config.name} because it's unused");
                        continue;
                    }

                    foreach (var target in config.targets)
                    {
                        switch (target.kind)
                        {
                            case AttributeTarget.Kind.None:
                                {
                                    break;
                                }
                            case AttributeTarget.Kind.UV:
                                {
                                    UVChannel channel = target.uvTarget.channel;
                                    if (usedUVChannels.Contains(channel))
                                        break;

                                    usedUVChannels.Add(channel);

                                    List<Vector4> output = new();
                                    mesh.GetUVs(target.uvTarget.ChannelIndex, output);

                                    Debug.Log(output.Count);

                                    if (output.Count != mesh.vertexCount)
                                    {
                                        Debug.Log($"Expected {mesh.vertexCount} items; ignoring original UVs");
                                        output.Clear();
                                        for (int i = 0; i < mesh.vertexCount; ++i)
                                        {
                                            output.Add(default);
                                        }
                                    }

                                    uvData[channel] = output;

                                    break;
                                }
                            case AttributeTarget.Kind.VertexColor:
                                {
                                    if (usedVertexColors)
                                        break;

                                    usedVertexColors = true;

                                    colorData = new();
                                    colorData.AddRange(mesh.colors.Select(color => (Vector4)color));

                                    Debug.Log(colorData.Count);

                                    if (colorData.Count != mesh.vertexCount)
                                    {
                                        Debug.Log($"Expected {mesh.vertexCount} items; ignoring original colors");
                                        colorData.Clear();
                                        for (int i = 0; i < mesh.vertexCount; ++i)
                                        {
                                            colorData.Add(default);
                                        }
                                    }
                                    break;
                                }
                        }
                    }
                }
            }

            public void Write(AttributeTarget target, int index, float value)
            {
                switch (target.kind)
                {
                    case AttributeTarget.Kind.None:
                        {
                            break;
                        }
                    case AttributeTarget.Kind.UV:
                        {
                            Vector4 current = uvData[target.uvTarget.channel][index];
                            current[target.uvTarget.ComponentIndex] = value;
                            uvData[target.uvTarget.channel][index] = current;
                            break;
                        }
                    case AttributeTarget.Kind.VertexColor:
                        {
                            Vector4 current = colorData[index];
                            current[target.vertexColorTarget.ComponentIndex] = value;
                            colorData[index] = current;
                            break;
                        }
                }
            }

            public void Flush()
            {
                foreach (var channel in usedUVChannels)
                {
                    Debug.Log($"Flushing {channel}");

                    mesh.SetUVs((int) channel, uvData[channel]);
                }

                if (usedVertexColors)
                {
                    Debug.Log($"Flushing colors");

                    mesh.SetColors(colorData.Select(vec => (Color) vec).ToArray());
                }
            }
        }

        void ReadSingleMesh(VertexMetadata vertexMetadata, DataParser parser, Mesh mesh)
        {
            if (Settings.instance.Debug)
                Debug.Log($"Vertex count on {mesh.name}: {mesh.vertexCount}");

            List<Vector2> lookup = new();
            mesh.GetUVs(parser.VertexSource.ChannelIndex, lookup);

            int verts = mesh.vertexCount;

            List<int> indexLookup = new();

            for (int i = 0; i < verts; ++i)
            {
                float xCoord = lookup[i][parser.VertexSource.ComponentIndex];
                int index = 0;

                // type-pun the float back into an int to retrieve the original vertex index.
                unsafe
                {
                    float* ptr = &xCoord;
                    index = *(int*)ptr;
                }

                indexLookup.Add(index);
            }

            List<string> usedAttributes = parser.PeekRecordHeaders();

            IntermediateVertexData intermediateVertexData = new(vertexMetadata.attributeConfigs, usedAttributes, parser.VertexCount, mesh);

            for (int recordIndex = 0; recordIndex < parser.NumRecords; ++recordIndex)
                ReadSingleRecord(vertexMetadata.attributeConfigs, intermediateVertexData, parser, mesh, indexLookup);

            intermediateVertexData.Flush();
        }

        void ReadSingleRecord(List<VertexDataImporter.AttributeConfig> configs, IntermediateVertexData intermediateVertexData, DataParser parser, Mesh mesh, List<int> indexLookup)
        {
            parser.ReadRecordHeader();

            if (Settings.instance.Debug)
                Debug.Log($"Reading {parser.AttributeName} into {mesh.name}");

            var config = configs.Find(config => config.name == parser.AttributeName);
            var targets = config.targets;

            foreach (var target in targets)
            {
                Debug.Log(target.uvTarget);
            }

            Debug.Log($"Reading {parser.VertexCount} verts with {parser.Dimensions} dims into UV whatever");

            List<Vector4> results = new(parser.VertexCount);

            parser.ReadRecordData(results);

            int meshVertices = mesh.vertexCount;

            for (int vertex = 0; vertex < meshVertices; ++vertex)
            {
                int index = indexLookup[vertex];

                if (index < 0 || index >= results.Count)
                {
                    Debug.LogError($"Bogus index of {index} for {mesh.name}; only {results.Count}/{parser.VertexCount} vertices should be there Did the UV map get overwritten?");
                    return;
                }

                Vector4 vec = results[index];

                for (int targetIndex = 0; targetIndex < 4; ++targetIndex)
                    intermediateVertexData.Write(targets[targetIndex], vertex, vec[targetIndex]);
            }
        }
    }
}
