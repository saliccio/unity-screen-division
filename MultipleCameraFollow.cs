using UnityEngine;

public class MultipleCameraFollow : MonoBehaviour
{
    // This type of structures will be responsible for holding the layout of the cameras.
    private struct CameraLayout
    {
        public int regularX, regularY; // Holds how many regular (symmetric) cameras will be there for X and Y axes. 
        public int irregularX, irregularY; // Holds the axis information about the irregular camera (if exists).
        public float xSize, ySize; // X and Y sizes for regular cameras.
        public CameraLayout(int rx, int ry, int irx, int iry) // Constructor, in which variable assignments and size calculations happen.
        {
            regularX = rx;
            regularY = ry;
            irregularX = irx;
            irregularY = iry;
            xSize = 1f / (regularX + irregularX);
            ySize = 1f / (regularY + irregularY);
        }
    }
    public enum SliceMode { Vertical, Horizontal };

    [Tooltip("The camera prefab to instantiate.")]
    public Camera cameraPrefab;

    [Tooltip("The axis you want to stretch regular cameras along.")]
    public SliceMode stretchAxis;

    [Tooltip("The axis you want to stretch the irregular camera (if exists) along.")]
    public SliceMode irregularAxis;

    [Tooltip("The targets you want to follow.")]
    public GameObject[] targets;

    private Camera[] cameras; // Array to hold cameras.
    private CameraLayout layout; // Our layout.

    private bool isNumberPrime(int number)
    {
        bool prime = true;
        for (int i = 2; i * i <= number; i++)
        {
            if (number % i == 0)
            {
                prime = false;
                break;
            }
        }
        return prime;
    }

    private void getTwoProductsWithLowestSum(int number, out int number1, out int number2)
    {
        int numberX = 100000, numberY = 100000; // Initial numbers should be large since our aim is to find the minimum sum.
        for (int i = 2; i <= number; i++)
        {
            if (number % i == 0)
            {
                int divisor1 = number / i;
                int sum = divisor1 + i;
                if (sum < numberX + numberY)
                {
                    numberX = divisor1;
                    numberY = i;
                }
            }
        }
        number1 = numberX;
        number2 = numberY;
    }

    private void arrangeLayoutAxes(ref int numberX, ref int numberY)
    {
        if (stretchAxis == SliceMode.Horizontal) // These 2 if statements control which axis will have more camera, depending on the enum selection.
        {
            int temp = numberX;
            numberX = Mathf.Max(numberX, numberY);
            numberY = Mathf.Min(temp, numberY);
        }
        else
        {
            int temp = numberX;
            numberX = Mathf.Min(numberX, numberY);
            numberY = Mathf.Max(temp, numberY);
        }
    }

    private void setLayout()
    {
        int arrayLength = targets.Length; // Target count.
        // If the number of the targets is 1 or 2, there is no need to make a complex computation.
        if (arrayLength == 1)
        {
            switch (stretchAxis)
            {
                case SliceMode.Horizontal:
                    layout = new CameraLayout(1, 1, 0, 0);
                    break;
                case SliceMode.Vertical:
                    layout = new CameraLayout(1, 1, 0, 0);
                    break;
            }
            return;
        }
        else if (arrayLength == 2)
        {
            switch (stretchAxis)
            {
                case SliceMode.Horizontal:
                    layout = new CameraLayout(1, 1, 1, 0);
                    break;
                case SliceMode.Vertical:
                    layout = new CameraLayout(1, 1, 0, 1);
                    break;
            }
            return;
        }
        int orderedCameraCount = arrayLength; // This variable will hold the number of regular cameras.
        bool isPrime = isNumberPrime(arrayLength); // Is our target count a prime number ?
        int numberX, numberY, irrX = 0, irrY = 0; // Variables we will use to create the layout object.
        if (isPrime)
        {
            orderedCameraCount--; // Guaranteeing that our regular camera count is non-prime.
            irrX = irregularAxis == SliceMode.Horizontal ? 1 : 0; // If the user selected the irregular camera to be horizontal, then set our X value to 1, otherwise 0. 
            irrY = irregularAxis == SliceMode.Vertical ? 1 : 0; // If the user selected the irregular camera to be vertical, then set our Y value to 1, otherwise 0.
        }
        getTwoProductsWithLowestSum(orderedCameraCount, out numberX, out numberY); // Getting 2 divisors that achieves the minimum sum is optimal for view.
        arrangeLayoutAxes(ref numberX, ref numberY); // Arranging numbers in axes, so the selected axis in enum will have more cameras.
        layout = new CameraLayout(numberX, numberY, irrX, irrY); // Create our layout object.
    }


    // The purpose of this function is to initialize a follow camera independently from whether it is regular or irregular.
    private void initFollowCamera(Camera camera, int index)
    {
        camera.transform.parent = targets[index].transform;
        camera.transform.localPosition = new Vector3(0, 4.5f, -6);
        cameras[index] = camera;
    }

    // The purpose of this function is to set the viewport rect of the irregular camera.
    private void setIrregularCamera(Camera camera, int index)
    {
        initFollowCamera(camera, index);
        Rect cameraRect = camera.rect;
        if (irregularAxis == SliceMode.Horizontal)
        {
            cameraRect.width = layout.xSize;
            cameraRect.height = 1;
            cameraRect.x = 1 - layout.xSize;
            cameraRect.y = 0;
        }
        else
        {
            cameraRect.width = 1;
            cameraRect.height = layout.ySize;
            cameraRect.x = 0;
            cameraRect.y = 0;
        }
        camera.rect = cameraRect;
    }

    // The purpose of this function is to set the viewport rect of a regular camera.
    private void setRegularCamera(Camera camera, int index)
    {
        initFollowCamera(camera, index);
        Rect cameraRect = camera.rect;
        cameraRect.width = layout.xSize;
        cameraRect.height = layout.ySize;
        cameraRect.x = (index % (layout.regularX)) * layout.xSize;
        cameraRect.y = 1 - ((index / (layout.regularX)) * layout.ySize) - layout.ySize;
        camera.rect = cameraRect;
    }
    void Start()
    {
        setLayout(); // Set our layout to be the optimal view.
        int targetCount = targets.Length;
        cameras = new Camera[targetCount]; // Create the camera array.
        int iterationIndex = 0; // Using iteration index, because we do not want to skip if there is a main camera, so we need to know where to start our loop.
        if (Camera.main != null)
        {
            cameras[0] = Camera.main;
            iterationIndex = 1;
            setRegularCamera(Camera.main, 0); // If exists, take our main camera into consideration.
        }
        for (; iterationIndex <= targetCount - 1; iterationIndex++) // Loop to instantiate cameras.
        {
            Camera iteratedCam = Instantiate(cameraPrefab).GetComponent<Camera>();
            setRegularCamera(iteratedCam, iterationIndex);
        }
        if (layout.irregularX != 0 || layout.irregularY != 0) // If the irregular camera exists, create it as well.
        {
            Camera irregularCamera = Instantiate(cameraPrefab).GetComponent<Camera>();
            setIrregularCamera(irregularCamera, targetCount - 1);
        }
    }
}
