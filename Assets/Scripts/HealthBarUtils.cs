using UnityEngine;

public static class HealthBarUtils
{
    public static bool TryGetMaxLocalY(Transform root, Renderer[] renderers, out float maxLocalY)
    {
        maxLocalY = float.MinValue;
        if (root == null || renderers == null || renderers.Length == 0)
        {
            return false;
        }

        Matrix4x4 rootWorldToLocal = root.worldToLocalMatrix;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (renderer.GetComponentInParent<EnemyHealthBar>() != null)
            {
                continue;
            }

            Bounds localBounds = renderer.localBounds;
            Vector3 center = localBounds.center;
            Vector3 extents = localBounds.extents;
            Matrix4x4 toRootLocal = rootWorldToLocal * renderer.transform.localToWorldMatrix;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 cornerLocal = center + new Vector3(extents.x * x, extents.y * y, extents.z * z);
                        Vector3 cornerInRoot = toRootLocal.MultiplyPoint3x4(cornerLocal);
                        maxLocalY = Mathf.Max(maxLocalY, cornerInRoot.y);
                    }
                }
            }
        }

        return maxLocalY > float.MinValue;
    }
}
