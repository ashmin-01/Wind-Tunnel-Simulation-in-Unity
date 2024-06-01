using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class SphereTriangleCollision
{
    public static bool IsSphereIntersectingTriangle(float3 sphereCenter, float radius, float3 A, float3 B, float3 C)
    {
        // Step 2: Calculate the plane normal
        float3 AB = B - A;
        float3 AC = C - A;
        float3 N = math.normalize(math.cross(AB, AC));

        // Step 3: Calculate the distance from sphere center to plane
        float distance = math.abs(math.dot(N, sphereCenter - A));

        // Step 4: Check if sphere intersects the plane
        if (distance > radius)
        {
            return false; // No intersection with the plane
        }

        // Step 5: Project sphere center onto the plane
        float3 P = sphereCenter - distance * N;

        // Step 6: Barycentric coordinates to check if point P is inside the triangle
        float3 v0 = C - A;
        float3 v1 = B - A;
        float3 v2 = P - A;

        float dot00 = math.dot(v0, v0);
        float dot01 = math.dot(v0, v1);
        float dot02 = math.dot(v0, v2);
        float dot11 = math.dot(v1, v1);
        float dot12 = math.dot(v1, v2);

        float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        bool isInTriangle = (u >= 0) && (v >= 0) && (u + v < 1);

        if (isInTriangle)
        {
            return true; // The sphere intersects the triangle
        }

        // Additional checks for edges and vertices can be added here

        return false; // No collision detected
    }
    public static bool IsSphereIntersecting (float3 sphereCenter, float sphereRadius , float3[] triangles)
    {
        bool isCollsion = false;
        for(int i = 0; i < triangles.Length; i= i+3)
        {
           isCollsion= IsSphereIntersectingTriangle(sphereCenter, sphereRadius, triangles[i] , triangles[i+1] , triangles[i+2]);
        }
        return isCollsion;
    }


}
