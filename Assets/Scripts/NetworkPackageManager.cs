using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class NetworkPackageManager<T> where T : class
{
    #region VARIABLES

    public event Action<Byte[]> OnRequirePackageTransmit;
    public List<T> Packages
    {
        get
        {
            if (packages == null)
            {
                packages = new List<T>();
            }

            return packages;
        }
    }
    public float SendSpeed
    {
        get
        {
            if (sendSpeed < 0.1f)
            {
                return sendSpeed = 0.1f;
            }
            return sendSpeed;
        }
        set
        {
            sendSpeed = value;
        }
    }

    private Queue<T> receivedPackages;
    private List<T> packages;
    private float sendSpeed = 0.2f;
    private float nextTick;

    #endregion VARIBLES

    private List<T> ReadBytes(byte[] bytes)
    {
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        using (MemoryStream memoryStream = new MemoryStream())
        {
            memoryStream.Write(bytes, 0, bytes.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return (List<T>)binaryFormatter.Deserialize(memoryStream);
        }
    }

    private byte[] CreateBytes(List<T> packages)
    {
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        using (MemoryStream memoryStream = new MemoryStream())
        {
            binaryFormatter.Serialize(memoryStream, packages);
            return memoryStream.ToArray();
        }
    }

    public T GetNextDataReceived()
    {
        if(receivedPackages == null || receivedPackages.Count <= 0)
        {
            return default(T);
        }

        return receivedPackages.Dequeue();
    }

    public void AddPackage(T package)
    {
        Packages.Add(package);
    }

    public void ReceiveData(byte[] bytes)
    {
        if (receivedPackages == null)
        {
            receivedPackages = new Queue<T>();
        }

        T[] packages = ReadBytes(bytes).ToArray();

        for (int i = 0; i < packages.Length; i++)
        {
            receivedPackages.Enqueue(packages[i]);
        }
    }

    public void Tick()
    {
        nextTick += 1 / SendSpeed * Time.fixedDeltaTime;
        if (nextTick > 1 && Packages.Count > 0)
        {
            nextTick = 0;

            if(OnRequirePackageTransmit != null)
            {
                byte[] bytes = CreateBytes(Packages);
                Packages.Clear();
                OnRequirePackageTransmit(bytes);
            }
        }
    } 
}
