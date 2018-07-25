using System;
using UnityEngine;

public class MatrixUtils
{
    internal static Vector3 GetPosition(Matrix4x4 m)
    {
        return m.GetColumn(3);
    }

    internal static Quaternion GetRotation(Matrix4x4 m)
    {
        var f = m.GetColumn(2);
        if (f == Vector4.zero)
        {
            return Quaternion.identity;
        }
        return Quaternion.LookRotation(f, m.GetColumn(1));
    }
}