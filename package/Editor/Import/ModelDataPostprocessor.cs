using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace ChemicalCrux.AttributeImporter
{
    public class ModelDataPostprocessor : AssetPostprocessor
    {
        public override uint GetVersion()
        {
            return 16;
        }

        void OnPostprocessModel(GameObject gameObject)
        {
            string path = Path.ChangeExtension(assetPath, "attrdata");
            var modelImporter = assetImporter as ModelImporter;

            if (!File.Exists(path))
                return;

            context.DependsOnArtifact(path);
            context.DependsOnSourceAsset(path);

            var vertexMetadata = AssetDatabase.LoadAssetAtPath<AttributeMetadata>(path);

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
                        if (Settings.instance.LogWarning)
                            Debug.LogWarning(gameObject + " has a name collision: " + meshFilter.name);
                    }
                }

                foreach (var skinnedMeshRenderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (!meshLookup.TryAdd(skinnedMeshRenderer.name, skinnedMeshRenderer.sharedMesh))
                    {
                        if (Settings.instance.LogWarning)
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
                    if (Settings.instance.LogWarning)
                        Debug.LogWarning($"Expected to find a mesh on the root of {gameObject}. Aborting import.");
                    return;
                }
            }


            using var stream = File.Open(path, FileMode.Open);
            using var reader = new BinaryReader(stream);
            DataParser parser = new(reader);

            if (!parser.ReadHeader())
            {
                return;
            }

            for (int mesh = 0; mesh < parser.NumObjects; ++mesh)
            {
                parser.ReadObjectHeader();

                if (Settings.instance.LogDebug)
                    Debug.Log("Name: " + parser.ObjectName);

                Mesh targetMesh;

                if (!useNames)
                {
                    targetMesh = singleMesh;
                }
                else if (!meshLookup.TryGetValue(parser.ObjectName, out targetMesh))
                {
                    if (Settings.instance.LogError)
                        Debug.LogError($"Expected to find an object named {parser.ObjectName} on {gameObject.name}. Aborting import.");
                    return;
                }

                ReadSingleMesh(vertexMetadata, parser, targetMesh);
            }
        }

        void ReadSingleMesh(AttributeMetadata vertexMetadata, DataParser parser, Mesh mesh)
        {
            if (Settings.instance.LogDebug)
                Debug.Log($"Vertex count on {mesh.name}: {mesh.vertexCount}");

            List<Vector2> lookup = new();
            mesh.GetUVs(parser.VertexSource.ChannelIndex, lookup);

            int verts = mesh.vertexCount;

            List<int> indexLookup = new();

            for (int i = 0; i < verts; ++i)
            {
                float xCoord = lookup[i][parser.VertexSource.ComponentIndex];
                int index = Mathf.RoundToInt(xCoord);
                indexLookup.Add(index);
            }

            List<string> usedAttributes = parser.PeekRecordHeaders();

            IntermediateVertexData intermediateVertexData = new(vertexMetadata.attributeConfigs, usedAttributes, parser.VertexCount, mesh);

            for (int recordIndex = 0; recordIndex < parser.NumRecords; ++recordIndex)
                ReadSingleRecord(vertexMetadata, intermediateVertexData, parser, mesh, indexLookup);
            
            intermediateVertexData.Flush();
        }

        void ReadSingleRecord(AttributeMetadata attributeMetadata, IntermediateVertexData intermediateVertexData, DataParser parser, Mesh mesh, List<int> indexLookup)
        {
            parser.ReadRecordHeader();

            if (Settings.instance.LogDebug)
                Debug.Log($"Reading {parser.AttributeName} into {mesh.name}");

            var config = attributeMetadata.attributeConfigs.Find(config => config.name == parser.AttributeName);
            var targets = config.targets;

            if (Settings.instance.LogDebug)
            {
                string logLine = "Targets: ";
                logLine += string.Join(", ", targets);
                Debug.Log(logLine);
            }

            if (Settings.instance.LogDebug)
                Debug.Log($"Reading {parser.VertexCount} verts with {parser.Dimensions} dimensions");

            int meshVertices = mesh.vertexCount;

            List<Vector4> results = new(parser.VertexCount);
            List<Vector4> mappedResults = new(meshVertices);
            
            parser.ReadRecordData(results);

            for (int vertex = 0; vertex < meshVertices; ++vertex)
            {
                int index = indexLookup[vertex];
                
                if (index < 0 || index >= results.Count)
                {
                    if (Settings.instance.LogError)
                        Debug.LogError($"Bogus index of {index} for {mesh.name}; only {results.Count}/{parser.VertexCount} vertices should be there Did the UV map get overwritten?");
                    return;
                }
                
                mappedResults.Add(results[index]);
            }

            foreach (var transformer in config.transformers)
            {
                transformer.Transform(mesh, mappedResults);
            }

            for (int vertex = 0; vertex < meshVertices; ++vertex)
            {
                Vector4 vec = mappedResults[vertex];

                for (int targetIndex = 0; targetIndex < targets.Count; ++targetIndex)
                    intermediateVertexData.Write(targets[targetIndex], vertex, vec[targetIndex]);
            }
        }
    }
}
