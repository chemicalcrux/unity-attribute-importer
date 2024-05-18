using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;

namespace ChemicalCrux.UVImporter
{
    public class DataParser
    {
        private BinaryReader reader;

        // header
        public int NumObjects { get; private set; }

        // object
        public string ObjectName { get; private set; }
        public UVTarget VertexSource { get; private set; }
        public int NumRecords { get; private set; }

        // record
        public string AttributeName { get; private set; }
        public int VertexCount { get; private set; }
        public int Dimensions { get; private set; }
        public int FloatCount { get; private set; }

        public DataParser(BinaryReader binaryReader)
        {
            reader = binaryReader;
        }

        public void ReadHeader()
        {
            NumObjects = reader.ReadInt32();
        }

        public void ReadObjectHeader()
        {
            ObjectName = ReadString();

            UVTarget source = default;
            source.channel = (UVChannel)reader.ReadInt32();
            source.component = (UVComponent)reader.ReadInt32();
            VertexSource = source;

            NumRecords = reader.ReadInt32();
        }

        public void ReadRecordHeader()
        {
            AttributeName = ReadString();
            VertexCount = reader.ReadInt32();
            Dimensions = reader.ReadInt32();
            FloatCount = VertexCount * Dimensions;
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

        /// <summary>
        /// Reads all of the record headers for the current object, then rewinds
        /// back to where it started.
        /// </summary>
        /// <returns>A list of attribute names</returns>
        public List<string> PeekRecordHeaders()
        {
            List<string> results = new();

            long position = reader.BaseStream.Position;

            Debug.Log(reader.BaseStream.Position);
            for (int i = 0; i < NumRecords; ++i)
            {
                ReadRecordHeader();
                results.Add(AttributeName);
                SkipRecordData();
            }

            reader.BaseStream.Seek(position, SeekOrigin.Begin);
            Debug.Log(reader.BaseStream.Position);

            return results;
        }

        private string ReadString()
        {
            int length = reader.ReadInt32();
            byte[] nameBytes = reader.ReadBytes(length);
            reader.ReadBytes((4 - (length % 4)) % 4);
            return Encoding.UTF8.GetString(nameBytes);
        }
    }
}
