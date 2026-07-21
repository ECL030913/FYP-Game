using UnityEngine;

public class MapBoundary : MonoBehaviour
{
    public static MapBoundary Instance;

    BoxCollider2D boundaryCollider;
    Bounds bounds;

    void Awake()
    {
        Instance = this;
        boundaryCollider = GetComponent<BoxCollider2D>();
        bounds = boundaryCollider.bounds;
    }

    public Vector2 ClampPosition(Vector2 position, float padding = 0.5f)
    {
        // A boundary smaller than the requested padding cannot represent a
        // playable area. Returning a clamped value in that case would collapse
        // all positions to one point, so leave the caller's position alone.
        if (!CanContainPadding(padding))
        {
            return position;
        }

        float x = Mathf.Clamp(position.x, bounds.min.x + padding, bounds.max.x - padding);
        float y = Mathf.Clamp(position.y, bounds.min.y + padding, bounds.max.y - padding);

        return new Vector2(x, y);
    }

    public bool IsInside(Vector2 position, float padding = 0.5f)
    {
        if (!CanContainPadding(padding))
        {
            return true;
        }

        return position.x >= bounds.min.x + padding &&
               position.x <= bounds.max.x - padding &&
               position.y >= bounds.min.y + padding &&
               position.y <= bounds.max.y - padding;
    }

    public Vector2 GetCenter()
    {
        return new Vector2(bounds.center.x, bounds.center.y);
    }

    private bool CanContainPadding(float padding)
    {
        float safePadding = Mathf.Max(0f, padding);
        return bounds.size.x > safePadding * 2f && bounds.size.y > safePadding * 2f;
    }
}
