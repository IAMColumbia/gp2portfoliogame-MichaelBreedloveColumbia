using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Trail : MonoBehaviour
{
    List<SpriteRenderer> currentSprites = new List<SpriteRenderer>();
    public SpriteRenderer spriteRenderer;
    public Sprite trailSprite;
    public float baseOpacity;
    public float opacityDecay;
    public float baseR;
    public float baseG;
    public float baseB;
    public float baseScale;
    public float scaleDecay;
    public int SpawnRate;
    public bool Active = false;
    public bool CanBeDeleted = false;
    public GameObject Owner;
    int remainingTime;

    // Start is called before the first frame update
    void Start()
    {
        //spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        remainingTime = SpawnRate;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSprites();

        if (!Active)
            return;

        if (remainingTime > 0)
        {
            remainingTime--;
        }
        else
        {
            GameObject clone = new GameObject("trail_sprite");
            clone.transform.position = Owner.transform.position;
            clone.transform.localScale = Owner.transform.localScale * baseScale;

            SpriteRenderer cloneRenderer = clone.AddComponent<SpriteRenderer>();
            cloneRenderer.sprite = trailSprite;
            cloneRenderer.sortingOrder = spriteRenderer.sortingOrder;
            cloneRenderer.color = new Color(baseR, baseG, baseB, baseOpacity);
            currentSprites.Add(cloneRenderer);
        }
    }
    
    void UpdateSprites()
    {
        for (int i = 0; i < currentSprites.Count; i++)
        {
            if (currentSprites[i] == null)
            {
                currentSprites.RemoveAt(i);
                i--;
            }
            else
            {
                float opacity = currentSprites[i].color.a;
                currentSprites[i].gameObject.transform.localScale *= scaleDecay;

                if (opacity - opacityDecay <= 0f)
                {
                    Destroy(currentSprites[i].gameObject);
                    currentSprites.RemoveAt(i);
                    i--;
                }
                else
                {
                    currentSprites[i].color = new Color(1, 1, 1, opacity - opacityDecay);
                }
            }
        }

        if (CanBeDeleted && currentSprites.Count < 1)
        {
            Destroy(gameObject);
        }
    }
}
