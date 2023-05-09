using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public enum EnemyState { DORMANT, IDLE, ACTIVE };

public class Enemy : LivingEntity
{
    public float ContactDamage = 20;    //Damage dealt to the player when this enemy touches them
    public float Speed = 1; //Movement Speed

    public EnemyState State = EnemyState.DORMANT;  //Current state
    public event EventHandler EnemyDied;

    public RoomTrigger Trigger;

    Vector2 StartPos;

    public override void Start()
    {
        base.Start();
        Trigger.RoomEntered += Trigger_RoomEntered;
        StartPos = transform.position;
        transform.position = new Vector2(999999, 999999); //Way off the screen
    }

    private void Trigger_RoomEntered(object sender, EventArgs e)
    {
        if (State != EnemyState.DORMANT)
            return;

        PlaceInRoom();
    }

    public virtual void PlaceInRoom()
    {
        State = EnemyState.ACTIVE;
        transform.position = StartPos;
        //TODO: Add spawn particle + sound
    }

    void SetSprite() //TODO: Make the enemy invisible while dormant, add a spawn particle to RoomEntered
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

        MoveTowardsPlayer();
    }

    public virtual void MoveTowardsPlayer()
    {
        Player player = FindObjectOfType<Player>();
        if (player == null)
            return;

        Vector3 Direction = player.transform.position - transform.position;
        GetComponent<Rigidbody2D>().velocity = new Vector2(Direction.x, Direction.y).normalized * Speed;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        Player other = collision.gameObject.GetComponent<Player>();
        if (other != null && other.RemainingInvulnTime <= 0)
        {
            other.TakeDamage(ContactDamage, false);
        }
    }

    public override void Die()
    {
        EnemyDied.Invoke(this, new EventArgs());
        base.Die();
    }
}
