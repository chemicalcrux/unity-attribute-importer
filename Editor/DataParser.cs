using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace ChemicalCrux.UVImporter
{
    public struct VertexSource
    {
        public int sourceChannel;
        public int sourceComponent;
    }

    public class DataParser
    {
        private BinaryReader reader;

        // header
        public int NumObjects { get; private set; }

        // object
        public int NameLength { get; private set; }
        public string Name { get; private set; }
        public VertexSource VertexSource { get; private set; }
        public int NumRecords { get; private set; }

        // record
        public int VertexCount { get; private set; }
        public int Dimensions { get; private set; }
        
        public DataParser(BinaryReader binaryReader)
        {
            this.reader = binaryReader;
        }

        public void ReadHeader()
        {
            NumObjects = reader.ReadInt32();
        }

        public void ReadObjectHeader()
        {
            NameLength = reader.ReadInt32();

            byte[] nameBytes = reader.ReadBytes(NameLength);
            reader.ReadBytes((4 - (NameLength % 4)) % 4);
            Name = Encoding.UTF8.GetString(nameBytes);

            VertexSource source = default;
            source.sourceChannel = reader.ReadInt32();
            source.sourceComponent = reader.ReadInt32();
            VertexSource = source;

            NumRecords = reader.ReadInt32();
        }

        public void ReadRecordHeader()
        {
            VertexCount = reader.ReadInt32();
            Dimensions = reader.ReadInt32();
        }

        public void ReadRecordData(List<Vector4> output)
        {
            for (int vertex = 0; vertex < VertexCount; ++vertex)
            {
                Vector4 result = default;

                for (int dimension = 0; dimension < Dimensions; ++dimension)
                {
                    result[dimension] = reader.ReadSingle();
                }

                output.Add(result);
            }
        }

        public void SkipRecordData()
        {
            reader.BaseStream.Seek(sizeof(float) * VertexCount * Dimensions, SeekOrigin.Current);
        }
    }
}
