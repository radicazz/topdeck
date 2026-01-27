using UnityEngine;

public static class TargetingUtils
{
    public static T FindClosestTarget<T>(Vector3 position, float range, LayerMask mask, Collider[] buffer) where T : Component
    {
        if (buffer == null || buffer.Length == 0)
        {
            return null;
        }

        int count = Physics.OverlapSphereNonAlloc(position, range, buffer, mask);
        T closest = null;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            Collider hit = buffer[i];
            if (hit == null)
            {
                continue;
            }

            if (!hit.TryGetComponent(out T candidate))
            {
                continue;
            }

            float distance = (candidate.transform.position - position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                closest = candidate;
            }
        }

        return closest;
    }
}
