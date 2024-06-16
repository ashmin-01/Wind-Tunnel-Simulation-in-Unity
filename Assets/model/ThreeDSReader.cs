using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine;

public  class ThreeDSReader
{
    public static List<Vector3> ReadVertices(string assetPath)
    {
        try
        {
            byte[] fileData = File.ReadAllBytes(assetPath);

            // Assuming a simple 3ds structure with a chunk for vertices
            if (fileData.Length > 4 && fileData[0] == '4' && fileData[1] == 'D' && fileData[2] == '0' && fileData[3] == '0')
            {
                // Potentially a .3ds file, try to find the vertex chunk
                int vertexChunkStart = FindChunk(fileData, "VERT");
                if (vertexChunkStart > 0)
                {
                    // Extract vertex data based on chunk size
                    int vertexCount = GetChunkSize(fileData, vertexChunkStart);
                    int vertexDataSize = vertexCount * 3 * sizeof(float); // Assuming 3 floats per vertex (x, y, z)

                    if (vertexDataSize <= fileData.Length - vertexChunkStart - 6) // Account for chunk header
                    {
                        // Read vertex data (assuming floats)
                        List<Vector3> vertices = new List<Vector3>(vertexCount); // Use List constructor with capacity

                        for (int i = 0; i < vertexCount; i++)
                        {
                            int vertexOffset = vertexChunkStart + 6 + i * 3 * sizeof(float);
                            vertices.Add(new Vector3(
                                BitConverter.ToSingle(fileData, vertexOffset),
                                BitConverter.ToSingle(fileData, vertexOffset + sizeof(float)),
                                BitConverter.ToSingle(fileData, vertexOffset + 2 * sizeof(float))
                            ));
                        }

                        return vertices;
                    }
                    else
                    {
                        Debug.LogError("Incomplete vertex data in .3ds file");
                        return null;
                    }
                }
                else
                {
                    Debug.LogError("Could not find VERT chunk in .3ds file");
                    return null;
                }
            }
            else
            {
                Debug.LogWarning("Not a recognized .3ds file format");
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error reading .3ds file: " + ex.Message);
            return null;
        }
    }

    // Helper function to find a chunk by its ID (4 characters)
    private static int FindChunk(byte[] data, string chunkId)
    {
        for (int i = 0; i < data.Length - 4; i++)
        {
            if (data[i] == chunkId[0] && data[i + 1] == chunkId[1] &&
                data[i + 2] == chunkId[2] && data[i + 3] == chunkId[3])
            {
                return i;
            }
        }
        return -1;
    }

    // Helper function to get the size of a chunk
    private static int GetChunkSize(byte[] data, int chunkStart)
    {
        return (data[chunkStart + 4] << 24) | (data[chunkStart + 5] << 16) |
               (data[chunkStart + 6] << 8) | data[chunkStart + 7];
    }
}
