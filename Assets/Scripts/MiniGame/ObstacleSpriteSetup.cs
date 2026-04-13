// script 控制障礙物 sprite 設定同 collider 更新：
// 一開始：
// 1. 取得 SpriteRenderer 同 PolygonCollider2D

// 當設定新 sprite 時：
// 2. 將新 sprite 套用到 SpriteRenderer
// 3. 將 PolygonCollider2D disable 再 enable
// 4. 強制重新生成 collider 形狀去 match 新 sprite

// This script applies a sprite to an obstacle and updates its collider.
// At start:
// 1. Gets the SpriteRenderer and PolygonCollider2D components

// When applying a new sprite:
// 2. Sets the new sprite on the SpriteRenderer
// 3. Disables and re-enables the PolygonCollider2D
// 4. Forces the collider to rebuild to match the new sprite

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

//Reference: Mini game (Dani, 2020)