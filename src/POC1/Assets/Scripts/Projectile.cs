using System.Collections;
using System.Collections.Generic;
using System.Net.Cache;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Device;
using UnityEngine.Scripting.APIUpdating;
using Screen = UnityEngine.Device.Screen;

public class Projectile : MonoBehaviour
{
    //Public variables:
    public Vector2 CurrentTargetPos;    //The projectile's target position. Call Move() after modifying to change the projectile's direction, or just use ChangeTargetPos.
    public AudioClip[] HitSounds;        //The sound(s) played when this projectile hits a target.

    public GameObject Owner;    //Projectile's owner.

    public float Speed = 1f;    //The projectile's movement speed. Call Move() after modifying to change the projectile's speed, or use ChangeSpeed.
    public float Damage = 10f;  //Damage dealt when this projectile hits a target.
    public float HitVolume = 1; //The volume of the sound(s) played when this projectile hits a target.

    public bool IsLoaded = false;   //If false, this projectile deletes itself when off-screen.
    public bool IgnoreIFrames = false;  //Should this projectile ignore immunity frames?

    public int Team = 0;   //Projectile's team index

    //Private variables:
    Rigidbody2D ProjRB; //The projectile's rigidbody.

    // Start is called before the first frame update
    void Start()
    {
        ProjRB = GetComponent<Rigidbody2D>();
        Move();
    }

    // Update is called once per frame
    void Update()
    {
        ProjectileThink();
    }

    //Called every frame, can be used to alter the projectile's behavior.
    public virtual void ProjectileThink()
    {
    }

    //Changes the projectile's direction to move towards its current target position. Called automatically by ChangeTargetPos and ChangeSpeed.
    public virtual void Move()
    {
        MoveToPoint(CurrentTargetPos);
    }

    //Moves the projectile in the direction of a given target location, but does not change its current target position (if you must change the target position, use ChangeTargetPosition).
    public virtual void MoveToPoint(Vector2 TargetPos)
    {
        Vector2 Direction = GetDirectionToPoint(TargetPos);
        ProjRB.velocity = new Vector2(Direction.x, Direction.y).normalized * Speed;
    }

    //Same as MoveToPoint, but allows the user to change the projectile's current speed as well. Does not change the Speed variable, use ChangeSpeed to do so.
    public virtual void MoveToPoint(Vector2 TargetPos, float SpeedOverride)
    {
        Vector2 Direction = GetDirectionToPoint(TargetPos);
        ProjRB.velocity = new Vector2(Direction.x, Direction.y).normalized * SpeedOverride;
    }

    //Returns the Vector2 direction from this projectile's current location to a given point.
    //TODO: Create a utility class and add this to it.
    Vector2 GetDirectionToPoint(Vector2 TargetPos)
    {
        return TargetPos - new Vector2(transform.position.x, transform.position.y);
    }

    //Changes the projectile's current speed, then updates its velocity accordingly.
    public void ChangeSpeed(float NewSpeed) { Speed = NewSpeed; Move(); }

    //Changes the projectile's current target position, then updates its velocity accordingly.
    public void ChangeTargetPos(Vector2 NewTargetPos) { CurrentTargetPos = NewTargetPos; Move(); }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsLoaded)
            return;

        LivingEntity other = collision.gameObject.GetComponent<LivingEntity>();
        if (other != null)
        {
            if (other.Team != Team)
            {
                if ((IgnoreIFrames || other.RemainingInvulnTime <= 0))
                {
                    AudioSource.PlayClipAtPoint(HitSounds[Random.Range(0, HitSounds.Length)], transform.position, HitVolume);
                    other.TakeDamage(Damage, IgnoreIFrames);
                }
            }
            else
            {
                Physics2D.IgnoreCollision(GetComponent<Collider2D>(), other.GetComponent<Collider2D>(), true);
            }
        }

        Destroy(gameObject);
        //TODO: Particle
    }

    //TODO: Create a utility class and add all of these to it:
    /*Vector2 GetDirectionToPoint(Vector2 StartPos, Vector2 TargetPos)
    {
        return TargetPos - StartPos;
    }

    Vector2 GetDirectionToMouse()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return mousePos - transform.position;
    }

    Vector2 GetDirectionToMouse(Vector2 StartPos)
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return new Vector2(mousePos.x, mousePos.y) - StartPos;
    }*/
}
