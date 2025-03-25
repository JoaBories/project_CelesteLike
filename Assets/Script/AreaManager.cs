using Cinemachine;
using UnityEngine;

public class AreaManager : MonoBehaviour
{
    [SerializeField] public GameObject fixCam;
    [SerializeField] public GameObject movingCam;
    [SerializeField] public GameObject VM;
    
    private DashOrb[] dashOrbs;

    private GameObject currentArea;

    private void Awake()
    {
        dashOrbs = GameObject.FindObjectsOfType<DashOrb>();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("area") && collision.gameObject != currentArea)
        {
            currentArea = collision.gameObject;
            switchCam(collision.gameObject);
        }
    }

    private void switchCam(GameObject area)
    {
        bool isFixed = area.GetComponent<area>().isCameraFixed;

        foreach(DashOrb dashOrb in dashOrbs)
        {
            dashOrb.Reactive();
        }

        if (isFixed)
        {
            fixCam.SetActive(true);
            movingCam.SetActive(false);
            fixCam.transform.position = new Vector3(area.transform.position.x, area.transform.position.y, fixCam.transform.position.z);
        }
        else
        {
            fixCam.SetActive(false);
            movingCam.SetActive(true);
            Collider2D confiner = area.GetComponent<PolygonCollider2D>();
            CinemachineConfiner confinerComponent = VM.GetComponent<CinemachineConfiner>();
            confinerComponent.m_BoundingShape2D = confiner;
        }
    }
}
