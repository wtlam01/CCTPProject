using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class ObstacleSpriteSetup : MonoBehaviour
{
    SpriteRenderer sr;
    PolygonCollider2D poly;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        poly = GetComponent<PolygonCollider2D>();
    }

    public void ApplySprite(Sprite newSprite)
    {
        if (newSprite == null) return;

        sr.sprite = newSprite;

        // 強制重建 collider
        poly.enabled = false;
        poly.enabled = true;
    }
}