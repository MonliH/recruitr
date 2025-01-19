using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public CinemachineCamera SitCamera;
    public CinemachineCamera UICamera;

    public bool sitCameraActive = true;

    void Start()
    {
        SitCamera.Priority = 10;
        UICamera.Priority = 0;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            SwapCameras();
        }
        else if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.CompareTag("PC") && sitCameraActive)
                {
                    SwapCameras();
                }
                else if (!sitCameraActive && !hit.collider.CompareTag("PC"))
                {
                    SwapCameras();
                }
            }
            else if (!sitCameraActive)
            {
                SwapCameras();
            }
        }
    }
    private void SwapCameras()
    {
        sitCameraActive = !sitCameraActive;
        if (sitCameraActive)
        {
            SitCamera.Priority = 10;
            UICamera.Priority = 0;
        }
        else
        {
            SitCamera.Priority = 0;
            UICamera.Priority = 10;
        }
    }
}
