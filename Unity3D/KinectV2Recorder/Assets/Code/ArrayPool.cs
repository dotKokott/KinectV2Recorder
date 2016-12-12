using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;

public class PoolEntry<T> {
    public T[] Resource;
    public int LockCount;

    public bool IsFree { get { return LockCount <= 0; } }

    public PoolEntry( T[] resource, int lockCount ) {
        Resource = resource;
        LockCount = lockCount;
    }

    public void Free() {
        Interlocked.Decrement( ref LockCount );
    }    
}
public class ArrayPool<T> {

    public List<PoolEntry<T>> Resources;
    public int SizePerArray;

    public int GrowStep = 10;

    public ArrayPool( int sizePerArray, int preAllocCount ) {
        Resources = new List<PoolEntry<T>>( preAllocCount );
        SizePerArray = sizePerArray;

        for ( var i = 0; i < preAllocCount; i++ ) {
            AddEmptyItem();
        }
    }

    public PoolEntry<T> AddEmptyItem(int lockCount = 0) {
        var arr = new T[SizePerArray];
        var entry = new PoolEntry<T>( arr, lockCount );
        Resources.Add( entry );

        return entry;
    }

    public PoolEntry<T> RequestResource( int lockCount ) {
        foreach ( var res in Resources ) {
            if ( res.IsFree ) {
                Assert.IsTrue( res.LockCount == 0 );
                res.LockCount = lockCount;

                return res;
            }        
        }

        Debug.LogFormat( "Out of resources for {0}, new size {1}", this.ToString(), Resources.Count + GrowStep );

        var newEntry = AddEmptyItem( lockCount );
        for ( var i = 0; i < GrowStep - 1; i++ ) {
            var arr = new T[SizePerArray];
            var entry = new PoolEntry<T>( arr, 0 );

            Resources.Add( entry );
        }

        return newEntry;
    }
}
