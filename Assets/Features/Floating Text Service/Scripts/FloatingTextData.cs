using UnityEngine;

public struct FloatingTextData
{
    public string text;
    public Color? color;
    public float durationOverride;
    public float fontSizeOverride;
    public Vector3 worldPosition;

    // Icon support
    public Sprite iconOverride;         
    public Vector2? iconSizeOverride;

    public static FloatingTextData FromWorld(string text, Vector3 pos)
    {
        return new FloatingTextData
        {
            text = text,
            worldPosition = pos,
            durationOverride = -1f,
            fontSizeOverride = -1f,
            iconOverride = null,
            iconSizeOverride = null
        };
    }
}