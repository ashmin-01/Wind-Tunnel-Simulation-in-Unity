using UnityEngine;

public class SphereTriangleCollision
{
    public static bool IsSphereIntersectingTriangle(Vector3 sphereCenter, float radius, Vector3 A, Vector3 B, Vector3 C)
    {
        // Step 2: Calculate the plane normal
        Vector3 AB = B - A;
        Vector3 AC = C - A;
        Vector3 N = Vector3.Cross(AB, AC).normalized;

        // Step 3: Calculate the distance from sphere center to plane
        float distance = Mathf.Abs(Vector3.Dot(N, sphereCenter - A));

        // Step 4: Check if sphere intersects the plane
        if (distance > radius)
        {
            return false; // No intersection with the plane
        }

        // Step 5: Project sphere center onto the plane
        Vector3 P = sphereCenter - distance * N;

        // Step 6: Barycentric coordinates to check if point P is inside the triangle
        Vector3 v0 = C - A;
        Vector3 v1 = B - A;
        Vector3 v2 = P - A;

        float dot00 = Vector3.Dot(v0, v0);
        float dot01 = Vector3.Dot(v0, v1);
        float dot02 = Vector3.Dot(v0, v2);
        float dot11 = Vector3.Dot(v1, v1);
        float dot12 = Vector3.Dot(v1, v2);

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
}
