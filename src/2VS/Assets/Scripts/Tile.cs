using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public List<Sprite> Textures = new List<Sprite>();

    // Start is called before the first frame update
    void Start()
    {
        SetSprite();
    }

    public void SetSprite()
    {
        if (gameObject.TryGetComponent<SpriteRenderer>(out var Renderer))
        {
            Renderer.sprite = Textures[Random.Range(0, Textures.Count)];
        }
    }
}
