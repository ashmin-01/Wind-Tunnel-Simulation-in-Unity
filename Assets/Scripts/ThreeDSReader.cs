using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine;

public  class ThreeDSReader
{
    private const ushort MAIN_CHUNK = 0x4D4D;
    private const ushort EDITOR_CHUNK = 0x3D3D;
    private const ushort OBJECT_BLOCK = 0x4000;
    private const ushort TRIANGULAR_MESH = 0x4100;
    private const ushort VERTICES_LIST = 0x4110;

    public static List<Vector3> ReadVertices(string filePath)
    {
        List<Vector3> vertices = new List<Vector3>();

        try
        {
            using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                // Read the first chunk to check byte order
                ushort chunkID = reader.ReadUInt16();
                if (chunkID != MAIN_CHUNK)
                {
                    chunkID = SwapBytes(chunkID);
                    if (chunkID != MAIN_CHUNK)
                    {
                        throw new Exception("Unable to determine correct byte order.");
                    }
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                }

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    ReadChunk(reader, reader.BaseStream.Length, vertices);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error reading 3DS file: " + e.Message);
        }

        return vertices;
    }

    private static void ReadChunk(BinaryReader reader, long parentLength, List<Vector3> vertices)
    {
        long startPosition = reader.BaseStream.Position;

        while (reader.BaseStream.Position < startPosition + parentLength)
        {
            ushort chunkID = reader.ReadUInt16();
            int chunkLength = reader.ReadInt32();

            long chunkDataPosition = reader.BaseStream.Position;

            switch (chunkID)
            {
                case MAIN_CHUNK:
                case EDITOR_CHUNK:
                case OBJECT_BLOCK:
                case TRIANGULAR_MESH:
                    // Recursively read sub-chunks
                    ReadChunk(reader, chunkLength - 6, vertices);
                    break;
                case VERTICES_LIST:
                    int numVertices = reader.ReadUInt16();
                    Debug.Log("Found VERTICES_LIST chunk with " + numVertices + " vertices.");
                    for (int i = 0; i < numVertices; i++)
                    {
                        float x = reader.ReadSingle();
                        float y = reader.ReadSingle();
                        float z = reader.ReadSingle();
                        vertices.Add(new Vector3(x, y, z));
                    }
                    break;
                default:
                    // Skip unknown chunks
                    reader.BaseStream.Seek(chunkLength - 6, SeekOrigin.Current);
                    break;
            }
        }
    }

    private static ushort SwapBytes(ushort value)
    {
        return (ushort)((value >> 8) | (value << 8));
    }

    private static int SwapBytes(int value)
    {
        return (int)((SwapBytes((ushort)(value & 0xFFFF)) << 16) | SwapBytes((ushort)(value >> 16)));
    }

    private static float SwapBytes(float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Array.Reverse(bytes);
        return BitConverter.ToSingle(bytes, 0);
    }
}
