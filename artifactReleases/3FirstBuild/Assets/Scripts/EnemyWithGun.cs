using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWithGun : Enemy
{
    public float MaxChaseDistance = 0f; //The closest this enemy will get to the player before it stops moving.
    public float MaxShootDistance = 0f; //If the enemy is further from the player than this, it will not shoot.

    public GameObject Weapon;    //Enemy's weapon.
    public GameObject ammoBag;         //Enemy's ammunition.

    Weapon CurrentWeapon;       //Weapon script for equipped weapon
    Player Target;      //Target to aim at

    public override void Start()
    {
        base.Start();

        Renderer = GetComponent<SpriteRenderer>();
        CurrentWeapon = Instantiate(Weapon).GetComponent<Weapon>();
        CurrentWeapon.Owner = gameObject;
        ammoBag.GetComponent<AmmoHolder>().Infinite = true;
    }

    void SetSprite()
    {
        switch(State)
        {
            default:
                Renderer.sprite = DefaultSprite;
                break;
        }
    }

    //Called every frame, used to determine this enemy's behavior.
    public override void Think()
    {
        SetSprite();

        if (State != EnemyState.ACTIVE)
            return;

        DetectShoot();
        HoldWeapon();
        MoveTowardsPlayer();
    }

    public virtual void DetectShoot()
    {
        Target = FindObjectOfType<Player>();

        if (CurrentWeapon == null || Target == null)
            return;

        if (CurrentWeapon.RemainingPrimaryAttackCooldown <= 0 && Vector3.Distance(transform.position, Target.transform.position) <= MaxShootDistance)
        {
            CurrentWeapon.Shoot(false, ammoBag.GetComponent<AmmoHolder>(), CurrentWeapon.BarrelLocation, Target.transform.position);
        }
    }

    public override void MoveTowardsPlayer()
    {
        if (Target == null)
            return;

        float dist = Vector3.Distance(transform.position, Target.transform.position);
        if (dist >= MaxChaseDistance || OffScreen())
        {
            Vector3 Direction = Target.transform.position - transform.position;
            GetComponent<Rigidbody2D>().velocity = new Vector2(Direction.x, Direction.y).normalized * Speed;
        }
        else
        {
            GetComponent<Rigidbody2D>().velocity = new Vector2(0, 0);
        }
    }

    bool OffScreen()
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        bool onScreen = screenPos.x > 0f && screenPos.x < Screen.width && screenPos.y > 0f && screenPos.y < Screen.height;

        return !onScreen && !Renderer.isVisible;
    }

    public virtual void HoldWeapon()
    {
        if (CurrentWeapon == null || CurrentWeapon.GetType() != typeof(Weapon) || Target == null)
            return;

        //TODO: This throws a NullReferenceException 3 times at the start of any wave with an enemy with a gun and I have no clue why:
        CurrentWeapon.HoldWeapon(transform.position, Target.transform.position);
    }

    public override void Die()
    {
        base.Die();
        Destroy(CurrentWeapon.gameObject);
    }
}
