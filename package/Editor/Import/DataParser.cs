using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;

namespace ChemicalCrux.AttributeImporter
{
    public class DataParser
    {
        private const int expectedVersion = 1;

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

        /// <summary>
        /// This should be called exactly once to read the header.
        /// </summary>
        /// <returns>Whether or not the file was valid</returns>
        public bool ReadHeader()
        {
            int version = reader.ReadInt32();

            if (version != expectedVersion)
            {
                if (Settings.instance.LogError)
                    Debug.LogError($"Expected version {expectedVersion}, but the attribute file's version is {version}");
                return false;
            }

            NumObjects = reader.ReadInt32();

            return true;
        }

        /// <summary>
        /// Call this method to start reading an object's data. <see cref="ReadRecordHeader"/>
        /// and either <see cref="ReadRecordData"/> or <see cref="SkipRecordData"/> must be called
        /// the correct number of times before another object header is read.
        /// </summary>
        public void ReadObjectHeader()
        {
            ObjectName = ReadString();

            UVTarget source = default;
            source.channel = (UVChannel)reader.ReadInt32();
            source.component = (UVComponent)reader.ReadInt32();
            VertexSource = source;

            NumRecords = reader.ReadInt32();
        }

        /// <summary>
        /// Reads a single record's header. This must be called as many times as there are records.
        /// <see cref="ReadRecordData"/> or <see cref="SkipRecordData"/> must be called before another
        /// record's header is read.
        /// </summary>
        public void ReadRecordHeader()
        {
            AttributeName = ReadString();
            VertexCount = reader.ReadInt32();
            Dimensions = reader.ReadInt32();
            FloatCount = VertexCount * Dimensions;
        }

        /// <summary>
        /// Reads a single record's data into a list. Must be called exactly once after <see cref="ReadRecordHeader"/>.
        /// See <see cref="SkipRecordData"/> if the data is not needed.
        /// </summary>
        /// <param name="output">A list to be filled with data. The list will not be cleared.</param>
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

        /// <summary>
        /// Skips the data for a single record. Must be called exactly once after <see cref="ReadRecordHeader"/>.
        /// See <see cref="ReadRecordData"/> if the data is needed.
        /// </summary>
        public void SkipRecordData()
        {
            reader.BaseStream.Seek(sizeof(float) * VertexCount * Dimensions, SeekOrigin.Current);
        }

        /// <summary>
        /// Reads all of the record headers for the current object, then rewinds
        /// back to where it started. May only be called immediately after <see cref="ReadObjectHeader"/>.
        /// 
        /// Used to learn the names of the attributes in a record.
        /// </summary>
        /// <returns>A list of attribute names</returns>
        public List<string> PeekRecordHeaders()
        {
            List<string> results = new();

            long position = reader.BaseStream.Position;

            for (int i = 0; i < NumRecords; ++i)
            {
                ReadRecordHeader();
                results.Add(AttributeName);
                SkipRecordData();
            }

            reader.BaseStream.Seek(position, SeekOrigin.Begin);

            return results;
        }

        /// <summary>
        /// Reads a length, then reads that many bytes to decode to a string, then skips bytes to
        /// get back to a 4-byte alignment.
        /// </summary>
        /// <returns>The parsed string</returns>
        private string ReadString()
        {
            int length = reader.ReadInt32();
            byte[] nameBytes = reader.ReadBytes(length);
            reader.ReadBytes((4 - (length % 4)) % 4);
            return Encoding.UTF8.GetString(nameBytes);
        }
    }
}
