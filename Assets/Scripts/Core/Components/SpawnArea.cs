using UnityEngine;

/// <summary>
/// Defines a volumetric spawn region.
/// Gizmo draws the shape in the Scene view for easy placement.
/// </summary>
public class SpawnArea : MonoBehaviour
{
    public enum AreaShape { Box, Circle }

    [SerializeField] private AreaShape shape = AreaShape.Box;

    [Tooltip("Half-extents for Box, or radius for Circle (X only)")]
    [SerializeField] private Vector3 size = new Vector3(5f, 0f, 5f);

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Returns a random world-space point inside this area.</summary>
    public Vector3 GetRandomPoint()
    {
        return shape == AreaShape.Box ? RandomInBox() : RandomInCircle();
    }

    // -------------------------------------------------------------------------
    // Sampling
    // -------------------------------------------------------------------------

    private Vector3 RandomInBox()
    {
        Vector3 local = new Vector3(
            Random.Range(-size.x, size.x),
            size.y,                          // flat on the y plane of the area
            Random.Range(-size.z, size.z)
        );
        return transform.TransformPoint(local);
    }

    private Vector3 RandomInCircle()
    {
        // Uniform disk sampling
        float radius = size.x;
        Vector2 disk = Random.insideUnitCircle * radius;

        Vector3 local = new Vector3(disk.x, size.y, disk.y);
        return transform.TransformPoint(local);
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);

        if (shape == AreaShape.Box)
        {
            // DrawCube uses full size, so multiply by 2
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(new Vector3(0, size.y, 0), new Vector3(size.x * 2f, 0.1f, size.z * 2f));
        }
        else
        {
            Gizmos.DrawSphere(transform.position + Vector3.up * size.y, size.x);
        }

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Gizmos.matrix = Matrix4x4.identity;

        if (shape == AreaShape.Box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0, size.y, 0), new Vector3(size.x * 2f, 0.1f, size.z * 2f));
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position + Vector3.up * size.y, size.x);
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}
