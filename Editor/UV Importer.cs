using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace ChemicalCrux.UVImporter
{
    public class UVImporter : AssetPostprocessor
    {
        public override uint GetVersion()
        {
            return 5;
        }

        void OnPostprocessModel(GameObject gameObject)
        {
            Debug.Log(gameObject);

            string path = Path.ChangeExtension(assetPath, "uv");

            if (File.Exists(path))
            {
                Debug.Log("A file exists!!!");

                context.DependsOnSourceAsset(path);

                using var stream = File.Open(path, FileMode.Open);
                using var reader = new BinaryReader(stream);

                int sourceLayer = reader.ReadInt32();
                int sourceDim = reader.ReadInt32();
                int targetLayer = reader.ReadInt32();
                int dimensions = reader.ReadInt32();
                int vertices = reader.ReadInt32();

                Debug.Log($"Reading {vertices} verts with {dimensions} dims based on UV{sourceLayer}'s dim{sourceDim} into UV{targetLayer}");

                List<Vector4> results = new(vertices);

                for (int vertex = 0; vertex < vertices; ++vertex)
                {
                    Vector4 coordinate = default;

                    for (int dimension = 0; dimension < dimensions; ++dimension)
                    {
                        coordinate[dimension] = reader.ReadSingle();
                    }

                    results.Add(coordinate);
                }

                MeshFilter filter = gameObject.GetComponentInChildren<MeshFilter>();

                if (filter)
                {
                    Debug.Log("Got a filter");
                    Debug.Log("Vertex count: " + filter.sharedMesh.vertexCount);

                    List<Vector2> lookup = new();
                    filter.sharedMesh.GetUVs(sourceLayer, lookup);

                    List<Vector4> uvs = new();

                    int verts = filter.sharedMesh.vertexCount;

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

                        uvs.Add(results[index]);
                    }

                    filter.sharedMesh.SetUVs(targetLayer, uvs);
                }
            }
        }
    }
}
