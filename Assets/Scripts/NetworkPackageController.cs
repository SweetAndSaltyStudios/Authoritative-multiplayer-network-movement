using UnityEngine.Networking;

public class NetworkPackageController : NetworkBehaviour
{
    [System.Serializable]
    public class Package
    {
        public float Horizontal;
        public float Vertical;
        public float TimeStamp;
    }

    [System.Serializable]
    public class ReceivePackage
    {
        public float X;
        public float Y;
        public float Z;
        public float TimeStamp;
    }

    public NetworkPackageManager<Package> PackageManager
    {
        get
        {
            if (packageManager == null)
            {
                packageManager = new NetworkPackageManager<Package>();
                if (isLocalPlayer)
                {
                    packageManager.OnRequirePackageTransmit += TransmitPackageToServer;
                }
            }
            return packageManager;
        }
    }
    public NetworkPackageManager<ReceivePackage> ServerPackageManager
    {
        get
        {
            if (serverPacketManager == null)
            {
                serverPacketManager = new NetworkPackageManager<ReceivePackage>();
                if (isServer)
                {
                    serverPacketManager.OnRequirePackageTransmit += TransmitPackageToClients; ;
                }
            }
            return serverPacketManager;
        }
    }

    private NetworkPackageManager<Package> packageManager;
    private NetworkPackageManager<ReceivePackage> serverPacketManager;

    public virtual void FixedUpdate()
    {
        PackageManager.Tick();
        ServerPackageManager.Tick();
    }

    private void TransmitPackageToServer(byte[] bytes)
    {
        CmdTransmitPackagesToServer(bytes);
    }

    private void TransmitPackageToClients(byte[] bytes)
    {
        RpcReceiveDataOnClient(bytes);
    }

    [Command]
    private void CmdTransmitPackagesToServer(byte[] data)
    {
        PackageManager.ReceiveData(data);
    }

    [ClientRpc]
    private void RpcReceiveDataOnClient(byte[] data)
    {
        ServerPackageManager.ReceiveData(data);
    }   
}
