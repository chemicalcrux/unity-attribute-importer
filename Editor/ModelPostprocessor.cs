using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace ChemicalCrux.UVImporter
{
    public class ModelPostprocessor : AssetPostprocessor
    {
        public override uint GetVersion()
        {
            return 6;
        }

        void OnPostprocessModel(GameObject gameObject)
        {
            string path = Path.ChangeExtension(assetPath, "uv");
            var modelImporter = assetImporter as ModelImporter;

            if (!File.Exists(path))
                return;

            context.DependsOnArtifact(path);
            context.DependsOnSourceAsset(path);

            var exportedVertexData = AssetImporter.GetAtPath(path) as VertexDataImporter;

            if (!exportedVertexData)
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
                    Debug.Log("Name: " + parser.Name);

                Mesh targetMesh;

                if (!useNames)
                {
                    targetMesh = singleMesh;
                }
                else if (!meshLookup.TryGetValue(parser.Name, out targetMesh))
                {
                    Debug.LogWarning($"Expected to find an object named {parser.Name} on {gameObject.name}. Aborting import.");
                    return;
                }

                ReadSingleMesh(exportedVertexData, parser, targetMesh);
            }
        }

        void ReadSingleMesh(VertexDataImporter exportedVertexData, DataParser parser, Mesh mesh)
        {
            if (Settings.instance.Debug)
                Debug.Log("Vertex count: " + mesh.vertexCount);

            List<Vector2> lookup = new();
            mesh.GetUVs(parser.VertexSource.sourceChannel, lookup);

            int verts = mesh.vertexCount;

            List<int> indexLookup = new();

            for (int i = 0; i < verts; ++i)
            {
                float xCoord = lookup[i][parser.VertexSource.sourceComponent];
                int index = 0;

                // type-pun the float back into an int to retrieve the original vertex index.
                unsafe
                {
                    float* ptr = &xCoord;
                    index = *(int*)ptr;
                }
                
                indexLookup.Add(index);
            }

            for (int record = 0; record < parser.NumRecords; ++record)
            {
                ReadSingleRecord(exportedVertexData, parser, mesh, indexLookup);
            }
        }

        void ReadSingleRecord(VertexDataImporter exportedVertexData, DataParser parser, Mesh mesh, List<int> indexLookup)
        {
            parser.ReadRecordHeader();

            Debug.Log($"Reading {parser.VertexCount} verts with {parser.Dimensions} dims into UV whatever");


            List<Vector4> results = new(parser.VertexCount);

            parser.ReadRecordData(results);

            int meshVertices = mesh.vertexCount;
            List<Vector4> uvs = new(meshVertices);

            for (int vertex = 0; vertex < meshVertices; ++vertex)
            {
                int index = indexLookup[vertex];

                if (index < 0 || index >= results.Count)
                {
                    Debug.LogError($"Bogus index of {index} for {mesh.name}. Did the UV map get overwritten?");
                    return;
                }

                uvs.Add(results[indexLookup[vertex]]);
            }

            // TODO assign target layer!!!
            mesh.SetUVs(1, uvs);
        }
    }
}
