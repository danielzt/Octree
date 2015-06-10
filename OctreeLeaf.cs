//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using UnityEngine;
public struct OctreeLeaf : IEquatable<OctreeLeaf>, IComparable<OctreeLeaf>
{
	Int64 _trace;

	public OctreeLeaf(byte trace)
	{
        _trace = (trace & 0xF);
        level = 1;
	}

    private OctreeLeaf(Int64 trace, byte level)
    {
        _trace = trace;
        this.level = level;
    }

	public Vector3 Position(Vector3 orig, float origLen)
	{
		Vector3 result = orig;
		float len = origLen/(1<<Level);
        for (int i = 0; i < Level; i++)
        {
            byte childNum = (byte)((_trace >> (i * 3)) & 7);
            result = Octree.GetChildPos(result, len, childNum);
            len *= 2;
        }
		return result;
	}

    byte level;
	public byte Level
	{
		get
		{
			return level;
		}
	}

	public OctreeLeaf GetChild(byte childNum)
	{
		Int64 ranged = ((Int64)(childNum & 7));
		//keeps childNum valid without throwing
        //Int64 appendix = (ranged | 0x8) << (4 * Level);
        //Int64 mask = -1 << ((4 * Level) - 1);
        Int64 newTrace = (_trace << 3) | ranged;
        return new OctreeLeaf(newTrace,(byte)(Level+1));
	}

    /*public static implicit operator OctreeLeaf(Int64 t)
    {
        return new OctreeLeaf(t);
    }*/

    public static implicit operator OctreeLeaf(byte t)
    {
        return new OctreeLeaf(t);
    }

    public bool Equals(OctreeLeaf other)
    {
        return (this.Level == other.Level && this._trace == other._trace);
    }

    public int CompareTo(OctreeLeaf other)
    {
        int levelCheck = (this.Level - other.Level);
        int absLevelCheck = (levelCheck + (levelCheck >> 31)) ^ (levelCheck >> 31);
        int rangedLevelCheck = (levelCheck+(1-(levelCheck&int.MinValue)>>30))/(1+((levelCheck + (levelCheck >> 31)) ^ (levelCheck >> 31))); //Brings to -1/0/+1
        Int64 traceCheck = (this._trace - other._trace);
        int rangedTraceCheck = (int)(((rangedLevelCheck & 1) ^ 1) * (1 + traceCheck) / (1 + ((traceCheck + (traceCheck >> 63)) ^ (traceCheck >> 63))));//Brings to -1/0/1 then times by 1 if zero and 0 if not zero
        return rangedTraceCheck;
    }
    
    public static bool operator >(OctreeLeaf l1, OctreeLeaf l2)
    {
        return l1.Level>l2.Level && l1._trace > l2._trace;
    }
    public static bool operator <(OctreeLeaf l1, OctreeLeaf l2)
    {
        return l1.Level < l2.Level && l1._trace < l2._trace;
    }

    public static bool operator ==(OctreeLeaf l1, OctreeLeaf l2)
    {
        return (l1.Level == l2.Level && l1._trace == l2._trace);
    }

    public static bool operator !=(OctreeLeaf l1, OctreeLeaf l2)
    {
        return (l1.Level != l2.Level && l1._trace != l2._trace);
    }
}
