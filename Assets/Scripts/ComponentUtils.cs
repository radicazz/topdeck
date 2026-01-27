using UnityEngine;

public static class ComponentUtils
{
    public static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        if (gameObject.TryGetComponent(out T existing))
        {
            return existing;
        }

        return gameObject.AddComponent<T>();
    }
}
