using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

public class Pool<T> where T : MonoBehaviour
{
    private Func<T> _prefabSpawner;
    private Stack<T> availableObjects = new Stack<T>();

    public Pool(Func<T> prefabSpawner)
    {
        this._prefabSpawner = prefabSpawner;
    }

    public T Get()
    {
        if (availableObjects.Count > 0)
        {
            T obj = availableObjects.Pop();
            obj.gameObject.SetActive(true);
            return obj;
        }
        else
        {
            T newObj = _prefabSpawner.Invoke();
            return newObj;
        }
    }

    public void Release(T obj)
    {
        obj.gameObject.SetActive(false); 
        availableObjects.Push(obj);
    }
}
