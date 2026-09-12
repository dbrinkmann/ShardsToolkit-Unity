using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


static public class ShardsExtensionMethods
{
    public static string CombinePaths(params string[] paths)
    {
        if (paths == null)
        {
            throw new ArgumentNullException("paths");
        }
        return paths.Aggregate(Path.Combine);
    }

    static public T Clamp<T>(this T _val, T _bound1, T _bound2)
        where T : IComparable
    {
        bool boundsOrder = _bound1.CompareTo(_bound2) < 0;
        return _val.CompareTo(_bound1) < 0 == boundsOrder ? _bound1
            : _val.CompareTo(_bound2) >= 0 == boundsOrder ? _bound2
            : _val;
    }

    public const float DegreesPerRadian = (float)(180 / Math.PI);

    static public float NormalDegrees(this float _value)
    {
        float result = _value % 360.0f;
        if (result < 0) result += 360.0f;
        return result;
    }

    static public float ToDegrees(this double _value)
    {
        float result = (float)_value * DegreesPerRadian;
        return result.NormalDegrees();
    }

    static public double ToRadians(this float _value)
    {
        return _value / DegreesPerRadian;
    }

    // To help avoid problems due to floating-point rounding errors
    static public float LimitPrecision(this float _value)
    {
        //return (float)((int)(_value * 1000.0f)) / 1000.0f;
        return _value;
    }

    // For calculating xor-based hash codes for arrays of verts
    static int XorHashCode(this float[] _vertArray)
    {
        int result = _vertArray.Length;
        foreach (float value in _vertArray)
            result ^= result.GetHashCode();
        return result;
    }

    static bool VertArrayEquals(this float[] _vertArray, float[] _otherVertArray)
    {
        if (_otherVertArray == null) return false;
        if (_vertArray.Length != _otherVertArray.Length) return false;
        for (int i = 0; i < _vertArray.Length; i++)
            if (_vertArray[i] != _otherVertArray[i])
                return false;
        return true;
    }
}

/// <summary>
/// Comparer for comparing two keys, handling equality as beeing greater
/// Use this Comparer e.g. with SortedLists or SortedDictionaries, that don't allow duplicate keys
/// </summary>
/// <typeparam name="TKey"></typeparam>
public class DuplicateKeyComparer<TKey>
                :
                IComparer<TKey> where TKey : IComparable
{
    #region IComparer<TKey> Members

    public int Compare(TKey x, TKey y)
    {
        int result = x.CompareTo(y);

        if (result == 0)
            return 1;   // Handle equality as beeing greater
        else
            return result;
    }

    #endregion
}
