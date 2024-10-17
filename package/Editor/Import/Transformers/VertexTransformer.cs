using System.Collections.Generic;
using UnityEngine;

namespace ChemicalCrux.AttributeImporter.Transformers
{
    /// <summary>
    /// A VertexTransformer is applied to a single attribute, before the attribute's data is written
    /// into the vertex data of the mesh. The "data" list contains the value for each vertex.
    /// </summary>
    [System.Serializable]
    public abstract class VertexTransformer
    {
        public abstract void Transform(Mesh mesh, List<Vector4> data);
    }
}