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
            return 5;
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

            int meshes = reader.ReadInt32();

            for (int mesh = 0; mesh < meshes; ++mesh)
            {
                int nameLength = reader.ReadInt32();
                byte[] nameBytes = reader.ReadBytes(nameLength);
                reader.ReadBytes((4 - (nameLength % 4)) % 4);

                string name = Encoding.UTF8.GetString(nameBytes);

                if (Settings.instance.Debug)
                    Debug.Log("Name: " + name);

                Mesh targetMesh;

                if (!useNames)
                {
                    targetMesh = singleMesh;
                }
                else if (!meshLookup.TryGetValue(name, out targetMesh))
                {
                    Debug.LogWarning($"Expected to find an object named {name} on {gameObject.name}. Aborting import.");
                    return;
                }

                ReadSingleMesh(exportedVertexData, reader, targetMesh);
            }
        }

        void ReadSingleMesh(VertexDataImporter exportedVertexData, BinaryReader reader, Mesh mesh)
        {
            int sourceLayer = reader.ReadInt32();
            int sourceDim = reader.ReadInt32();
            int records = reader.ReadInt32();

            if (Settings.instance.Debug)
                Debug.Log("Vertex count: " + mesh.vertexCount);

            List<Vector2> lookup = new();
            mesh.GetUVs(sourceLayer, lookup);

            int verts = mesh.vertexCount;

            List<int> indexLookup = new();

            for (int i = 0; i < verts; ++i)
            {
                float xCoord = lookup[i][sourceDim];
                int index = 0;

                // type-pun the float back into an int to retrieve the original vertex index.
                unsafe
                {
                    float* ptr = &xCoord;
                    index = *(int*)ptr;
                }
                
                indexLookup.Add(index);
            }

            for (int record = 0; record < records; ++record)
            {
                ReadSingleRecord(exportedVertexData, reader, mesh, indexLookup);
            }
        }

        void ReadSingleRecord(VertexDataImporter exportedVertexData, BinaryReader reader, Mesh mesh, List<int> indexLookup)
        {
            int targetLayer = reader.ReadInt32();
            int dimensions = reader.ReadInt32();
            int vertices = reader.ReadInt32();

            Debug.Log($"Reading {vertices} verts with {dimensions} dims into UV{targetLayer}");

            List<Vector4> results = new(vertices);
            List<Vector4> uvs = new(vertices);

            for (int vertex = 0; vertex < vertices; ++vertex)
            {
                Vector4 coordinate = default;

                for (int dimension = 0; dimension < dimensions; ++dimension)
                {
                    coordinate[dimension] = reader.ReadSingle();
                }

                results.Add(coordinate);
            }

            for (int vertex = 0; vertex < mesh.vertexCount; ++vertex)
            {
                int index = indexLookup[vertex];

                if (index < 0 || index >= results.Count)
                {
                    Debug.LogError($"Bogus index of {index} for {mesh.name}. Did the UV map get overwritten?");
                    return;
                }

                uvs.Add(results[indexLookup[vertex]]);
            }

            mesh.SetUVs(targetLayer, uvs);
        }
    }
}
