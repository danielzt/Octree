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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using NuHash.UsefulUtilities;
public class Octree : INtree
{
	//public OctreeData data;
    public delegate bool SplitPolicy(Vector3 coord, float length, OctreeLeaf trace, int threadNum = 1);
    SplitPolicy _splitPolicy;
    public delegate bool LeafFunction(OctreeLeaf leaf, Vector3 pos, float len, int threadNum);
    LeafFunction _leafFunction;
	public OctreeLeaf baseTrace;
	public Vector3 position;
	public float boxLength;
	List<OctreeLeaf> leaves = new List<OctreeLeaf>();
	Int64 queuedItems=8;
	Stack<ThreadObject> queuedLeaves = new Stack<ThreadObject>();
	readonly object _lock = new object();

    static readonly Vector3[] midPointVectors = { new Vector3(0, -1, -1), new Vector3(1, -1, 0), new Vector3(0, -1, 1), new Vector3(-1, -1, 0), new Vector3(0, -1, 0), new Vector3(-1, 0, -1), new Vector3(0, 0, -1), new Vector3(1, 0, -1), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1), new Vector3(-1, 0, 1), new Vector3(-1, 0, 0), Vector3.zero, new Vector3(0, 1, -1), new Vector3(1, 0, 0), new Vector3(0, 1, 1), new Vector3(-1, 1, 0), new Vector3(0, 1, 0) };
    static readonly Vector3[] cornerPointVectors = { new Vector3(-1, -1, -1), new Vector3(1, -1, -1), new Vector3(1, -1, 1), new Vector3(-1, -1, 1), new Vector3(-1, 1, -1), new Vector3(1, 1, -1), Vector3.one, new Vector3(-1, 1, 1) };
	private struct ThreadObject{
		public OctreeLeaf trace;
		public float length;
		public Vector3 pos;
	}
	
	public Octree (SplitPolicy splitPolicy,Vector3 position,float length,OctreeLeaf trace)
	{
		this.baseTrace = trace;
		this.position = position;
        _splitPolicy = splitPolicy;
		this.boxLength = length;
	}

	public void Split()
	{

		float newLength = boxLength/2;
		for (byte i = 0; i < 8; i++) {
			ThreadObject tO = new ThreadObject(){
				trace = i,
				length = newLength,
				pos = GetChildPos(position,boxLength,i),
			};
			queuedLeaves.Push(tO);
			//ThreadPool.QueueUserWorkItem(new WaitCallback(SplitThread));
		}
		
        /*Thread thread = new Thread(new ThreadStart(()=>SplitThread(0)));
        thread.Name = "Octree Thread 0";
		thread.Start();
		Thread thread1 = new Thread(new ThreadStart(()=>SplitThread(1)));
        thread1.Name = "Octree Thread 1";
		thread1.Start();
		Thread thread2 = new Thread(new ThreadStart(()=>SplitThread(2)));
        thread2.Name = "Octree Thread 2";
		thread2.Start();
		Thread thread3 = new Thread(new ThreadStart(()=>SplitThread(3)));
        thread3.Name = "Octree Thread 3";
        thread3.Start();

		Thread thread4 = new Thread(new ThreadStart(()=>SplitThread(4)));
		thread4.Start();
		Thread thread5 = new Thread(new ThreadStart(()=>SplitThread(5)));
		thread5.Start();
		Thread thread6 = new Thread(new ThreadStart(()=>SplitThread(6)));
		thread6.Start();
		Thread thread7 = new Thread(new ThreadStart(()=>SplitThread(7)));
		thread7.Start();*/
		SplitThread(0);

	}

	private void SplitThread(int tNum=1)//byte[] trace,ref float length)
	{
		int threadNum = tNum;
		ThreadObject a;
		lock(_lock){
			a = queuedLeaves.Pop();
		}
		while(true){
			var trace = a.trace;
			float length = a.length;
			Vector3 cellPos = a.pos;
			if(_splitPolicy(cellPos,length,trace, threadNum))
			{
				//List<byte> newTrace = new List<byte>(trace);
				//newTrace.Add(0);
				float newLength = length/2;
                lock (_lock)
                {
                    queuedItems+=8;
                    for (byte i = 0; i < 8; i++)
                    {

                        ThreadObject tO = new ThreadObject()
                        {
                            trace = trace.GetChild(i),
                            length = newLength,
                            pos = GetChildPos(cellPos, length, i)
                        };

                        queuedLeaves.Push(tO);

                    }
                }
                
				
			}else{
				lock(_lock){
					leaves.Add(trace);
				}
			}
			lock(_lock){
                queuedItems--;
                if (queuedItems == 0)
                {
                    if (queuedLeaves.Count == 0)
                    {
                        Debug.Log("Split complete");
                        Debug.Log(leaves.Count + " leaves.");
                        queuedLeaves.TrimExcess();
                        leaves.Sort();
                    }
                    break;
                }
				a = queuedLeaves.Pop();
			}
		}
	}

    public List<OctreeLeaf> GetLeaves()
    {
        return leaves;
    }

	public static UInt64 ChildTrace(UInt64 trace){
		byte level = TraceToLevel(trace);
		return LevelToTrace((byte)(level+1))+8*(trace-LevelToTrace(level));
	}

	public static UInt64 ChildTrace(UInt64 trace,byte level){
		return LevelToTrace((byte)(level+1))+8*(trace-LevelToTrace(level));
	}
	
	public static byte TraceToLevel(UInt64 trace)
	{
		return (byte)(NMath.Log2_64((7*trace/8)+1)/3);
	}

	public static UInt64 LevelToTrace(byte level)
	{
		return (UInt64)(1<<(3*level) - 1)*8/7;
	}

	public Vector3 TraceToPosition(byte[] trace)
	{
		Vector3 pos = position;
		float len = boxLength;
		for (int i = 1; i < trace.Length; i++) {
			pos = GetChildPos(pos,len,trace[i]);
			len = len/2;
		}
		return pos;
	}

	public Vector3 TraceToPosition(UInt64 trace)
	{
		if(trace==0)
			return position;
		UInt64 level = NMath.Log2_64(7*trace/8+1)/3ul -1ul;
		Vector3 pos = position;
		float len = boxLength;
		UInt64 nCells = NMath.IntPow(8,level);
		while(level>0){
			UInt64 rPos = trace - nCells/8;
			byte childNum = (byte)(8*rPos/nCells);
			pos = GetChildPos(pos,len,childNum);
			len = len/2;
			nCells = nCells/8;
			level--;
		}
		return pos;
	}

	public static Vector3 GetChildPos(Vector3 center, float length, byte childNum)
	{
        return center + 0.25f * length * cornerPointVectors[childNum];
	}

	public static Vector3 GetCornerPos(Vector3 center, float length, byte cornerNum)
	{
		return center + 0.5f*length*cornerPointVectors[cornerNum];
	}

	public static Vector3[] GetCornersPos(Vector3 center, float length)
	{
		Vector3[] results = new Vector3[8];
		for (byte i = 0; i < 8; i++) {
			results[i] = GetCornerPos(center,length,i);
		}
		return results;
	}

    public static void GetChildrenPos(Vector3 center, float length, ref Vector3[] results)
    {
        if (!(results.Length >= 8))
        {
            throw new IndexOutOfRangeException("Number of elements in results array is not enough to accomodate positions");
        }
        for (byte i = 0; i < 8; i++)
        {
            results[i] = GetCornerPos(center, length, i);
        }
    }

	public static void GetCornersPos(Vector3 center, float length, ref Vector3[] results)
	{
		if(!(results.Length >= 8)){
			throw new IndexOutOfRangeException("Number of elements in results array is not enough to accomodate positions");
		}
		for (byte i = 0; i < 8; i++) {
            results[i] = center + 0.5f * length * cornerPointVectors[i];
		}
	}

	
	public static Vector3 GetMidPointPos(Vector3 center, float length, byte posNum)
	{
		return center + 0.5f*length * midPointVectors[posNum];
	}

	public static Vector3[] GetMidPointsPos(Vector3 center, float length)
	{
		Vector3[] results = new Vector3[19];
		for (byte i = 0; i < 19; i++) {
			results[i] = GetMidPointPos(center,length,i);
		}
		return results;
	}

	public static void GetMidPointsPos(Vector3 center, float length,ref Vector3[] results)
	{
		if(!(results.Length >= 19)){
			throw new IndexOutOfRangeException("Number of elements in results array is not enough to accomodate positions");
		}
		for (byte i = 0; i < 19; i++) {
            results[i] = center + 0.5f * length * midPointVectors[i];
		}
		return;
	}
	
}