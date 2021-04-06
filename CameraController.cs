using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController instance;
    public bool initialized = false;
    public Transform target;
    Vector3 targetPos;
    [SerializeField] Vector3 offset; // camera position offset from current character
    public float followSpeed;
    Vector3 refVel = Vector3.zero;

    public bool zoomOut = false;
    [SerializeField] float zoomSpeed;
    [SerializeField] float finalOrthographicSize;

    [SerializeField] Transform introTarget;
    [SerializeField] Transform finalIntroTarget;
    [SerializeField] Transform player1;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this.gameObject);
    }

    private void Update()
    {
        if (!initialized)
        {
            if (transform.position.y > 3f) target = introTarget;
            else
            {
                target = player1.transform;
                initialized = true;
            }
        }

        if (target)
        {
            if (target.CompareTag("Player1")) // different clamp settings per player/pod
                targetPos = new Vector3(Mathf.Clamp(target.position.x, -14f, -12f), target.position.y, target.position.z) + offset;
            else if (target.CompareTag("Player2"))
                targetPos = new Vector3(0f, target.position.y, target.position.z) + offset;
            else if (target.CompareTag("Player3"))
                targetPos = new Vector3(Mathf.Clamp(target.position.x, 12f, 14f), target.position.y, target.position.z) + offset;
            else targetPos = target.position;

            FollowTarget();
        }

        if (zoomOut)
        {
            if (Camera.main.orthographicSize < finalOrthographicSize)
            {
                Camera.main.orthographicSize += zoomSpeed * Time.deltaTime;
            }
            else
            {
                Camera.main.orthographicSize = finalOrthographicSize;
                zoomOut = false;
            }
        }
    }

    void FollowTarget()
    {
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref refVel, followSpeed);
    }
}
