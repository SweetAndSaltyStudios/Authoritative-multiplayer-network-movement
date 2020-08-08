using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerNetwork : NetworkPackageController
{
    [SerializeField]
    [Range(0.1f, 1)]
    private float networkSendRate = 0.5f;

    [SerializeField]
    private float moveSpeed = 4f;
    [SerializeField]
    private float correctionTreshold;
    [SerializeField]
    private bool isPredictionEnabled;

    private Vector3 lastPosition;

    private CharacterController characterController;
    private MeshRenderer meshRenderer;

    private List<ReceivePackage> predictedPackages;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Start()
    {
        PackageManager.SendSpeed = networkSendRate;
        ServerPackageManager.SendSpeed = networkSendRate;
        predictedPackages = new List<ReceivePackage>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        meshRenderer.material.color = Color.red;
    }

    private void Update()
    {
        LocalClientMove();
        ServeUpdate();
        RemoteClientUpdate();
    }

    private void LocalClientMove()
    {
        if (!isLocalPlayer)
            return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (horizontal != 0 || vertical != 0) 
        {
            float timeStamp = Time.time;

            PackageManager.AddPackage(new Package
            {
                Horizontal = horizontal,
                Vertical = vertical,
                TimeStamp = timeStamp
            }
            );

            if (isPredictionEnabled)
            {
                Move(Input.GetAxis("Horizontal") * moveSpeed, Input.GetAxis("Vertical") * moveSpeed);
                predictedPackages.Add(new ReceivePackage
                {
                    X = transform.position.x,
                    Y = transform.position.y,
                    Z = transform.position.z,
                    TimeStamp = timeStamp
                });
            }
        }   
    }

    private void Move(float horizontal, float vertical)
    {
        characterController.Move(new Vector3(horizontal, 0, vertical));
    }

    private void ServeUpdate()
    {
        if (!isServer || isLocalPlayer)
            return;

        Package packageData = PackageManager.GetNextDataReceived();

        if (packageData == null)
            return;

        Move(packageData.Horizontal * moveSpeed, packageData.Vertical * moveSpeed);

        if (transform.position == lastPosition)
            return;

        lastPosition = transform.position;

        ServerPackageManager.AddPackage(new ReceivePackage
        {
            X = transform.position.x,
            Y = transform.position.y,
            Z = transform.position.z,
            TimeStamp = packageData.TimeStamp       
        });
    }

    private void RemoteClientUpdate()
    {
        if (isServer)
            return;

        var data = ServerPackageManager.GetNextDataReceived();

        if (data == null)
            return;

        if(isLocalPlayer && isPredictionEnabled)
        {
            var transmittedPackage = predictedPackages.Where(x => x.TimeStamp == data.TimeStamp).FirstOrDefault();
            if (transmittedPackage == null)
                return;

            if(Vector3.Distance(new Vector3(transmittedPackage.X, transmittedPackage.Y,transmittedPackage.Z),new Vector3(data.X,data.Y,data.Z))> correctionTreshold)
            {
                transform.position = new Vector3(data.X, data.Y, data.Z);
            }

            predictedPackages.RemoveAll(x => x.TimeStamp <= data.TimeStamp);
        }
        else
        {
            transform.position = new Vector3(data.X, data.Y, data.Z);
        }
    }
}
