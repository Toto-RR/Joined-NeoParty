using UnityEngine;

public class FrustumVolumeSpawner : MonoBehaviour
{
    [Header("Cámara / Frustum")]
    public Camera cam;
    public float minDistance = 6f;
    public float maxDistance = 22f;
    [Range(0f, 0.45f)] public float screenMargin = 0.08f;

    [Header("Restricciones opcionales")]
    public BoxCollider allowedArea;            // isTrigger
    public LayerMask visibilityBlockers = ~0;  // paredes, columnas, etc.
    public LayerMask collisionMask = ~0;       // para evitar solapes
    public float clearanceRadius = 0.6f;

    public bool TryGetVisiblePoint(out Vector3 pos, int maxTries = 20)
    {
        for (int i = 0; i < maxTries; i++)
        {
            float u = Random.Range(screenMargin, 1f - screenMargin);
            float v = Random.Range(screenMargin, 1f - screenMargin);
            float d = Random.Range(minDistance, maxDistance);

            Vector3 nearP = cam.ViewportToWorldPoint(new Vector3(u, v, minDistance));
            Vector3 farP = cam.ViewportToWorldPoint(new Vector3(u, v, maxDistance));
            Vector3 candidate = Vector3.Lerp(nearP, farP, (d - minDistance) / (maxDistance - minDistance));

            if (allowedArea && !allowedArea.bounds.Contains(candidate)) continue;

            Vector3 dir = candidate - cam.transform.position;
            float dist = dir.magnitude;
            if (Physics.Raycast(cam.transform.position, dir.normalized, out _, dist, visibilityBlockers))
                continue;

            if (Physics.CheckSphere(candidate, clearanceRadius, collisionMask, QueryTriggerInteraction.Ignore))
                continue;

            pos = candidate;
            return true;
        }

        pos = default;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!cam) return;
        Vector3[] uv = { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };
        Vector3[] near = new Vector3[4];
        Vector3[] far = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            var u = Mathf.Lerp(screenMargin, 1f - screenMargin, uv[i].x);
            var v = Mathf.Lerp(screenMargin, 1f - screenMargin, uv[i].y);
            near[i] = cam.ViewportToWorldPoint(new Vector3(u, v, minDistance));
            far[i] = cam.ViewportToWorldPoint(new Vector3(u, v, maxDistance));
        }
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.25f);
        DrawQuad(near); DrawQuad(far);
        for (int i = 0; i < 4; i++) Gizmos.DrawLine(near[i], far[i]);

        if (allowedArea)
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.15f);
            Gizmos.matrix = allowedArea.transform.localToWorldMatrix;
            Gizmos.DrawCube(allowedArea.center, allowedArea.size);
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.9f);
            Gizmos.DrawWireCube(allowedArea.center, allowedArea.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

    private static void DrawQuad(Vector3[] p)
    {
        Gizmos.DrawLine(p[0], p[1]);
        Gizmos.DrawLine(p[1], p[2]);
        Gizmos.DrawLine(p[2], p[3]);
        Gizmos.DrawLine(p[3], p[0]);
    }
}
