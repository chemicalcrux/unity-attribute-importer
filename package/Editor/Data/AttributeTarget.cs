namespace ChemicalCrux.AttributeImporter
{
    /// <summary>
    /// Represents where a single component of an attribute should be written to.
    /// </summary>
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

        public override string ToString()
        {
            switch (kind)
            {
                case Kind.None:
                    return "Target: None";
                case Kind.UV:
                    return "Target: " + uvTarget.ToString();
                case Kind.VertexColor:
                    return "Target: " + vertexColorTarget.ToString();
            }

            return "Target: Invalid Kind";
        }
    }

}