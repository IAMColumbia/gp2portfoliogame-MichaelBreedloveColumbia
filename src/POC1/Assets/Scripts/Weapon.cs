using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public enum WeaponState { IDLE, FIRING, DROPPED }

public class Weapon : MonoBehaviour
{
    //Public variables:
    public GameObject WeaponProjectile; //The projectile fired by this weapon's M1 attack.

    public WeaponState State = WeaponState.DROPPED; //Current state.

    public Vector2 BarrelLocation; //The location the bullets will be fired from.

    public Sprite DefaultSprite;    //Sprite to use by default.
    public Sprite DroppedSprite;    //Sprite to use while this weapon isn't being held.
    public Sprite FiringSprite;     //Sprite to use while firing.

    public AudioClip[] PrimaryAttackSounds;    //The sounds played when this weapon's primary attack is used.
    public AudioClip[] PrimaryAttackUnusableSounds;   //The sounds played when the player tries to use this weapon's primary attack, but cannot for any reason. Only plays when the player manually clicks the button, to prevent full-auto weapons from playing this every frame.

    public GameObject Owner;    //Weapon's owner.

    public float PrimaryAttackCooldown = 1; //Cooldown (in seconds) between M1 attacks.
    public float BarrelOffsetMultiplier = 0f;    //Used to determine the barrel's location, just tweak it until it looks good.
    public float Spread = 0;    //Projectile spread, in degrees.
    public float ShootVolume = 1.0f;    //Volume of this weapon's shoot sounds.
    public float UnusableVolume = 1.0f; //Volume of the sounds played when the player attempts to fire this weapon, but is unable to.
    public float BulletTargetDistance = 0.5f;    //The distance from the barrel of the point bullets should be fired towards. Used to prevent random spread from breaking if the mouse is too close to/behind the barrel. Works best when set slightly higher than BarrelOffsetMultiplier + Spread.
    public float RemainingPrimaryAttackCooldown = 0; //Counts down by 1 every frame. if <= 0, this weapon can fire. Otherwise, it cannot.

    public int NumProjectiles = 1;  //The number of projectiles fired when this weapon shoots.

    public bool FullAuto = false;   //Does this weapon auto-fire if the user holds the attack button?
    public bool Held = false;       //Is this weapon currently being held?

    //Private variables:
    SpriteRenderer Renderer;    //Sprite renderer.

    //int RemainingAnimationTime = 0;     //Remaining frames in the current sprite animation. Counts down per frame, and reverts the weapon's state to IDLE 

    /// <summary>
    /// 
    /// POTENTIAL FUTURE IMPLEMENTATIONS:
    /// 
    //  - Ammo requirements?
    //      -Add a "reloading" state to WeaponState and a ReloadSprite to the sprite list.
    //  - Special attacks on M2 and M3?
    //      -Add "special2" and "special3" states to WeaponState and SpecialM2 and SpecialM3 to the sprite list.
    //  - Cooldown bar(s)?
    //      -Would be a class/script all on their own.
    ///
    /// </summary>

    // Start is called before the first frame update
    void Start()
    {
        Renderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        Renderer = GetComponent<SpriteRenderer>();
        AdjustCooldown();
        UpdateSprite();
        CheckDeleteCollider();
        WeaponProjectile.transform.position = transform.position;
    }

    void CheckDeleteCollider()
    {
        if (Owner != null)
        {
            Destroy(GetComponent<BoxCollider2D>());
        }
    }

    //Sets the weapon's sprite based on given conditions.
    void UpdateSprite()
    {
        if (Renderer == null || Renderer.GetType() != typeof(SpriteRenderer))
        {
            throw new System.Exception("Weapon.Renderer was either null or not a SpriteRenderer.");
        }

        switch (State)
        {
            case WeaponState.FIRING:
                Renderer.sprite = FiringSprite;
                break;
            case WeaponState.DROPPED:
                Renderer.sprite = DroppedSprite;
                break;
            default:
                Renderer.sprite = DefaultSprite;
                break;
        }
    }

    //Reduces the weapon's current cooldown values.
    void AdjustCooldown() //TODO: Add secondary attack cooldown countdown if M2 attacks are implemented
    {
        if (RemainingPrimaryAttackCooldown > 0) { RemainingPrimaryAttackCooldown -= Time.deltaTime; }
    }

    //Launches this weapon's projectile towards the cursor, from the weapon's position.
    public void Shoot(bool Clicked)
    {
        LaunchProjectile(Clicked, BarrelLocation, GetPointInDirection(BulletTargetDistance + BarrelOffsetMultiplier));
    }

    //Launches this weapon's projectile towards the cursor, from a specified origin point.
    public void Shoot(bool Clicked, Vector2 ShootPos)
    {
        LaunchProjectile(Clicked, ShootPos, GetPointInDirection(BulletTargetDistance + BarrelOffsetMultiplier));
    }

    //Launches this weapon's projectile towards a specified target position, from a specified origin point.
    public void Shoot(bool Clicked, Vector2 ShootPos, Vector2 TargetPos)
    {
        LaunchProjectile(Clicked, ShootPos, TargetPos);
    }

    //Called by the Shoot() methods, launches projectile(s) based on given inputs.
    void LaunchProjectile(bool Clicked, Vector2 ShootPos, Vector2 TargetPos)
    {
        if (RemainingPrimaryAttackCooldown > 0)
        {
            if (Clicked && PrimaryAttackUnusableSounds.Length > 0)
            {
                PlaySound(PrimaryAttackUnusableSounds, ShootPos);
            }

            return;
        }

        for (int i = 0; i < NumProjectiles; i++)
        {
            Vector2 TargetPosAfterSpread = TargetPos;

            if (Spread != 0f)
            {
                TargetPosAfterSpread = new Vector2(TargetPos.x + Random.Range(-Spread, Spread), TargetPos.y + Random.Range(-Spread, Spread));
            }

            GameObject projectile = Instantiate(WeaponProjectile, ShootPos, Quaternion.identity);
            Projectile newProj = projectile.GetComponent<Projectile>();
            newProj.CurrentTargetPos = TargetPosAfterSpread;
            newProj.Owner = Owner;
            newProj.IsLoaded = false;
            newProj.Team = Owner.GetComponent<LivingEntity>().Team;
        }

        PlaySound(PrimaryAttackSounds, ShootPos);

        //TODO: This firing animation is permanent. Find a clean, well-optimized way to change the state back to WeaponState.IDLE when the firing animation ends.
        State = WeaponState.FIRING;
        RemainingPrimaryAttackCooldown = PrimaryAttackCooldown;
    }

    //Plays a sound. If the source of the sound is the player, the sound is global (can be heard at max volume everywhere), otherwise it is local to the sound source.
    //TODO: Create a utility class and add this to it.
    void PlaySound(AudioClip[] Sounds, Vector2 SoundPos)
    {
        if (Owner.GetType() == typeof(Player))
        {
            SoundPos = Camera.main.transform.position;
        }

        AudioSource.PlayClipAtPoint(Sounds[Random.Range(0, Sounds.Length)], SoundPos, ShootVolume);
    }

    //Allows the weapon to be visually held and rotated around the user, instead of simply copying the holder's position.
    public void HoldWeapon(Vector3 Position, Vector3 AimLoc)
    {
        transform.position = Position;
        Vector3 Direction = AimLoc - transform.position;
        float Angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
        Quaternion Rotation = Quaternion.AngleAxis(Angle, Vector3.forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, Rotation, 60f * Time.deltaTime);
        BarrelLocation = GetPointInDirection(BarrelOffsetMultiplier);

        //Rotate the gun as well so you aren't holding it upside-down if it's to your left:
        Renderer.flipY = Mathf.Abs(transform.rotation.z) > 0.7;
    }

    //Returns a Vector3 which is X units away from the gun, in the direction it is facing.
    public Vector3 GetPointInDirection(float scale)
    {
        return transform.position + transform.right * scale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Owner != null)
            return;

        Player other = collision.gameObject.GetComponent<Player>();
        if (other != null)
        {
            other.DropWeapon();
            other.GrabWeapon(gameObject);
            Owner = other.gameObject;
            Destroy(GetComponent<BoxCollider2D>());
        }
    }
}
