using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LivingEntity : MonoBehaviour
{
    public float HP = 100;  //Remaining HP
    public float MaxHP = 100;   //Max HP

    public float InvulnTime = 0;  //Duration (in seconds) to grant invulnerability when hit

    public Sprite DefaultSprite;    //Default sprite

    public float HurtShadeReduction = 0;     //Used to make the sprite red temporarily when hit.
    public SpriteRenderer Renderer;

    public float RemainingInvulnTime = 0;

    public int Team = 0;    //Entity's team, used to prevent friendly fire

    // Start is called before the first frame update
    public virtual void Start()
    {
        Renderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    public virtual void Update()
    {
        Think();
        ReduceInvulnTime();
        SetShade();
    }

    public virtual void Think()
    {
        return;
    }

    public virtual void ReduceInvulnTime()
    {
        if (RemainingInvulnTime > 0)
        {
            RemainingInvulnTime -= Time.deltaTime;
        }
    }

    public virtual void SetShade()
    {
        if (HurtShadeReduction > 0)
        {
            Renderer.color = new Color(1, 1 - HurtShadeReduction, 1 - HurtShadeReduction);
            HurtShadeReduction -= Time.deltaTime * 5;
        }
    }

    public virtual void TakeDamage(float damage, bool IgnoreIFrames)
    {
        if (RemainingInvulnTime > 0 && !IgnoreIFrames)
            return;

        HurtShadeReduction = InvulnTime + 1;
        RemainingInvulnTime = InvulnTime;
        HP -= damage;

        if (HP <= 0)
        {
            Die();
        }
    }

    public virtual void Die()
    {
        Destroy(gameObject);
    }
}
