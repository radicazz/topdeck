using UnityEngine;

public static class RendererUtils
{
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();

    public static void SetColor(Renderer renderer, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.GetPropertyBlock(Block);
        Block.SetColor(ColorId, color);
        Block.SetColor(BaseColorId, color);
        renderer.SetPropertyBlock(Block);
    }
}
