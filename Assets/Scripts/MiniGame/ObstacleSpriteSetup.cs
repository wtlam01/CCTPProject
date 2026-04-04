// 負責幫障礙物換sprite同埋強制rebuild PolygonCollider2D去match新sprite嘅形狀
// This script applies a sprite to an obstacle and forces the polygon collider to rebuild by toggling it, since it doesnt auto update on sprite change.

using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
// unity will auto add these two components if missing
public class ObstacleSpriteSetup : MonoBehaviour
// handles applying a sprite to an obstacle and rebuilding the collider to match
{
    SpriteRenderer sr;
    PolygonCollider2D poly;
    // 兩個component，一個負責顯示sprite，一個負責碰撞

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        poly = GetComponent<PolygonCollider2D>();
        // grab both components on awake
    }

    public void ApplySprite(Sprite newSprite)
    {
        if (newSprite == null) return;

        sr.sprite = newSprite;

        poly.enabled = false;
        poly.enabled = true;
        // 強制disable再enable collider，迫佢重新generate shape去match新sprite
        // toggle trick bc PolygonCollider2D doesnt auto update when sprite changes
    }
}