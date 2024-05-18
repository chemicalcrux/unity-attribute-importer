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
    }

}