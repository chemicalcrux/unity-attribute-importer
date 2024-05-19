using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChemicalCrux.AttributeImporter
{
    public class IntermediateVertexData
    {
        private Dictionary<UVChannel, List<Vector4>> uvData = new();
        private List<Vector4> colorData = new();
        private int vertexCount;
        private Mesh mesh;

        private List<UVChannel> usedUVChannels = new();
        private bool usedVertexColors;

        public IntermediateVertexData(List<DataImporter.AttributeConfig> attributeConfigs, List<string> usedAttributes, int vertexCount, Mesh mesh)
        {
            this.vertexCount = vertexCount;
            this.mesh = mesh;


            foreach (var config in attributeConfigs)
            {
                if (usedAttributes.All(attribute => attribute != config.name))
                {
                    if (Settings.instance.debug)
                        Debug.Log($"Skipping {config.name} because it's unused");
                    continue;
                }

                foreach (var target in config.targets)
                {
                    switch (target.kind)
                    {
                        case AttributeTarget.Kind.None:
                            {
                                break;
                            }
                        case AttributeTarget.Kind.UV:
                            {
                                UVChannel channel = target.uvTarget.channel;
                                if (usedUVChannels.Contains(channel))
                                    break;

                                usedUVChannels.Add(channel);

                                List<Vector4> output = new();
                                mesh.GetUVs(target.uvTarget.ChannelIndex, output);

                                Debug.Log(output.Count);

                                if (output.Count != mesh.vertexCount)
                                {
                                    Debug.Log($"Expected {mesh.vertexCount} items; ignoring original UVs");
                                    output.Clear();
                                    for (int i = 0; i < mesh.vertexCount; ++i)
                                    {
                                        output.Add(default);
                                    }
                                }

                                uvData[channel] = output;

                                break;
                            }
                        case AttributeTarget.Kind.VertexColor:
                            {
                                if (usedVertexColors)
                                    break;

                                usedVertexColors = true;

                                colorData = new();
                                colorData.AddRange(mesh.colors.Select(color => (Vector4)color));

                                Debug.Log(colorData.Count);

                                if (colorData.Count != mesh.vertexCount)
                                {
                                    Debug.Log($"Expected {mesh.vertexCount} items; ignoring original colors");
                                    colorData.Clear();
                                    for (int i = 0; i < mesh.vertexCount; ++i)
                                    {
                                        colorData.Add(default);
                                    }
                                }
                                break;
                            }
                    }
                }
            }
        }

        public void Write(AttributeTarget target, int index, float value)
        {
            switch (target.kind)
            {
                case AttributeTarget.Kind.None:
                    {
                        break;
                    }
                case AttributeTarget.Kind.UV:
                    {
                        Vector4 current = uvData[target.uvTarget.channel][index];
                        current[target.uvTarget.ComponentIndex] = value;
                        uvData[target.uvTarget.channel][index] = current;
                        break;
                    }
                case AttributeTarget.Kind.VertexColor:
                    {
                        Vector4 current = colorData[index];
                        current[target.vertexColorTarget.ComponentIndex] = value;
                        colorData[index] = current;
                        break;
                    }
            }
        }

        public void Flush()
        {
            foreach (var channel in usedUVChannels)
            {
                Debug.Log($"Flushing {channel}");

                mesh.SetUVs((int)channel, uvData[channel]);
            }

            if (usedVertexColors)
            {
                Debug.Log($"Flushing colors");

                mesh.SetColors(colorData.Select(vec => (Color)vec).ToArray());
            }
        }
    }
}
