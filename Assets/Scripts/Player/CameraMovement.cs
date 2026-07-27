using UnityEngine;
using UnityEngine.Tilemaps; 

public class CameraMovement : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 0f, -10f);
    public float smoothTime = 0.08f;

    [Header("Edge Visibility")]
    public float edgePadding = 1.5f;

    [Header("Map Reference")]
    public Tilemap backgroundTilemap; 

    Camera cam;
    Vector3 velocity;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        desiredPosition = ClampCameraInsideMap(desiredPosition);

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPosition, ref velocity, smoothTime
        );
    }

    Vector3 ClampCameraInsideMap(Vector3 cameraPosition)
    {
        if (cam == null || !cam.orthographic) return cameraPosition;

        
        Bounds mapBounds;
        if (backgroundTilemap != null)
        {
            mapBounds = backgroundTilemap.GetComponent<TilemapRenderer>().bounds;
        }
        else if (MapBoundary.Instance != null)
        {
            mapBounds = MapBoundary.Instance.GetBounds(); 
        }
        else
        {
            return cameraPosition;
        }

        float cameraHalfHeight = cam.orthographicSize;
        float cameraHalfWidth = cameraHalfHeight * cam.aspect;

        if (mapBounds.size.x <= cameraHalfWidth * 2f ||
            mapBounds.size.y <= cameraHalfHeight * 2f)
        {
            cameraPosition.x = mapBounds.center.x;
            cameraPosition.y = mapBounds.center.y;
            return cameraPosition;
        }

        cameraPosition.x = Mathf.Clamp(
            cameraPosition.x,
            mapBounds.min.x + cameraHalfWidth - edgePadding,
            mapBounds.max.x - cameraHalfWidth + edgePadding
        );

        cameraPosition.y = Mathf.Clamp(
            cameraPosition.y,
            mapBounds.min.y + cameraHalfHeight - edgePadding,
            mapBounds.max.y - cameraHalfHeight + edgePadding
        );

        return cameraPosition;
    }
}