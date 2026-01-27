using UnityEngine;

public static class LayerUtils
{
    public static void SetLayerRecursive(GameObject gameObject, int layer)
    {
        if (gameObject == null || layer < 0)
        {
            return;
        }

        gameObject.layer = layer;
        Transform transform = gameObject.transform;
        for (int i = 0; i < transform.childCount; i++)
        {
            SetLayerRecursive(transform.GetChild(i).gameObject, layer);
        }
    }
}
