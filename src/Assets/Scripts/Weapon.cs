using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public enum WeaponState { IDLE, FIRING, DROPPED, HOLSTERED }

public class Weapon : MonoBehaviour
{
    public WeaponState State = WeaponState.DROPPED; //Current state.

    public Vector2 BarrelLocation; //The location the bullets will be fired from.

    public Sprite DefaultSprite;    //Sprite to use by default.
    public Sprite DroppedSprite;    //Sprite to use while this weapon isn't being held.
    public Sprite FiringSprite;     //Sprite to use while firing.

    public AudioClip[] PrimaryAttackSounds;    //The sounds played when this weapon's primary attack is used.
    public AudioClip[] PrimaryAttackUnusableSounds;   //The sounds played when the player tries to use this weapon's primary attack, but cannot for any reason. Only plays when the player manually clicks the button, to prevent full-auto weapons from playing this every frame.
    public AudioClip[] DeploySounds;            //The sounds to play when this weapon is equipped.

    Queue<GameObject> LoadedProjectiles= new Queue<GameObject>();

    public GameObject StartingAmmo; //Type of ammo loaded into this weapon when it is picked up for the first time
    public GameObject Owner;    //Weapon's owner.

    public float PrimaryAttackCooldown = 1; //Cooldown (in seconds) between M1 attacks.
    public float BarrelOffsetMultiplier = 0f;    //Used to determine the barrel's location, just tweak it until it looks good.
    public float Spread = 0;    //Projectile spread, in degrees.
    public float ShootVolume = 1.0f;    //Volume of this weapon's shoot sounds.
    public float UnusableVolume = 1.0f; //Volume of the sounds played when the player attempts to fire this weapon, but is unable to.
    public float BulletTargetDistance = 0.5f;    //The distance from the barrel of the point bullets should be fired towards. Used to prevent random spread from breaking if the mouse is too close to/behind the barrel. Works best when set slightly higher than BarrelOffsetMultiplier + Spread.
    public float RemainingPrimaryAttackCooldown = 0; //Counts down by 1 every frame. if <= 0, this weapon can fire. Otherwise, it cannot.

    public int NumProjectiles = 1;  //The number of projectiles fired when this weapon shoots.
    public float ClipSize = 0;        //Max ammo loaded
    public float AmountLoaded = 0;   //Current ammo loaded

    private float ReloadCDGameTime = 0;
    public float ReloadCD;  //The time, in seconds, between weapon reloads.

    public bool FullAuto = false;   //Does this weapon auto-fire if the user holds the attack button?
    public bool Held = false;       //Is this weapon currently being held?

    public float DamageMultiplier = 1.0f;
    public float VelocityMultiplier = 1.0f;
    public bool IgnoreIFrames = false;
    public float FreshProjectileBonusPerProjectile;
    public float MaxFreshProjectileBonus;
    public float StaleProjectilePenalty;
    private float FreshProjectileBonus = 0f;
    GameObject LastProjectile = null;

    private bool Exists = false;

    public float ScreenShakeForce = 0;
    public float ScreenShakeDecay = 0;
    public float MaxScreenShake = 0;

    //Private variables:
    public SpriteRenderer Renderer;    //Sprite renderer.

    // Start is called before the first frame update
    void Start()
    {
        Renderer = GetComponent<SpriteRenderer>();
        RemainingPrimaryAttackCooldown = 0;

        bool StillLoading = false;
        do
        {
            StillLoading = ReloadWeapon();
        }
        while (StillLoading);

        Exists = true;
    }

    public bool WeaponAlreadyExists() { return Exists; }

    // Update is called once per frame
    void Update()
    {
        Renderer = GetComponent<SpriteRenderer>();
        UpdateSprite();
        CheckDeleteCollider();
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

    //Launches this weapon's projectile towards the cursor, from the weapon's position.
    public virtual void Shoot(bool Clicked, AmmoHolder holder)
    {
        LaunchProjectile(Clicked, holder, BarrelLocation, GetPointInDirection(BulletTargetDistance + BarrelOffsetMultiplier));
    }

    //Launches this weapon's projectile towards the cursor, from a specified origin point.
    public virtual void Shoot(bool Clicked, AmmoHolder holder, Vector2 ShootPos)
    {
        LaunchProjectile(Clicked, holder, ShootPos, GetPointInDirection(BulletTargetDistance + BarrelOffsetMultiplier));
    }

    //Launches this weapon's projectile towards a specified target position, from a specified origin point.
    public virtual void Shoot(bool Clicked, AmmoHolder holder, Vector2 ShootPos, Vector2 TargetPos)
    {
        LaunchProjectile(Clicked, holder, ShootPos, TargetPos);
    }

    //Called by the Shoot() methods, launches projectile(s) based on given inputs.
    public virtual void LaunchProjectile(bool Clicked, AmmoHolder holder, Vector2 ShootPos, Vector2 TargetPos)
    {
        if (Time.time < RemainingPrimaryAttackCooldown || Time.time < ReloadCDGameTime)
        {
            return;
        }

        float ROFMult = 1.0f;

        if (AmountLoaded < 1)
        {
            //TODO: Start a reload sequence if the player has enough ammo on reserve for their currently-chosen ammo type
            if ((holder.AmmoCount > 0 || holder.Infinite) && holder.AmmoType.GetComponent<Projectile>().ClipWeight <= ClipSize)
            {
                ReloadWeapon(holder);
            }
            else if (PrimaryAttackUnusableSounds.Length > 0)
            {
                PlaySound(PrimaryAttackUnusableSounds, ShootPos);
            }
        }
        else
        {
            GameObject projOriginal = LoadedProjectiles.Peek();

            //TODO: Sound effects and particles for this, possibly a HUD notification as well.
            if (LastProjectile != null)
            {
                if (projOriginal != LastProjectile)
                {
                    FreshProjectileBonus += FreshProjectileBonusPerProjectile;
                    if (FreshProjectileBonus > MaxFreshProjectileBonus)
                    {
                        FreshProjectileBonus = MaxFreshProjectileBonus;
                    }
                }
                else
                {
                    FreshProjectileBonus *= StaleProjectilePenalty;
                }
            }

            for (int i = 0; i < NumProjectiles; i++)
            {
                Vector2 TargetPosAfterSpread = TargetPos;

                if (Spread != 0f)
                {
                    TargetPosAfterSpread = new Vector2(TargetPos.x + Random.Range(-Spread, Spread), TargetPos.y + Random.Range(-Spread, Spread));
                }

                GameObject projectile = Instantiate(projOriginal, ShootPos, Quaternion.identity);
                Projectile newProj = projectile.GetComponent<Projectile>();
                newProj.CurrentTargetPos = TargetPosAfterSpread;
                newProj.Owner = Owner;
                newProj.IsLoaded = false;
                newProj.Team = Owner.GetComponent<LivingEntity>().Team;
                newProj.Speed *= VelocityMultiplier;
                newProj.Damage *= DamageMultiplier + FreshProjectileBonus;
                newProj.IgnoreIFrames = IgnoreIFrames;
            }

            Projectile og = projOriginal.GetComponent<Projectile>();
            ROFMult = og.RateOfFireMultiplier;
            PlaySound(PrimaryAttackSounds, ShootPos);
            AmountLoaded -= og.ClipWeight;
            LoadedProjectiles.Dequeue();
            LastProjectile = projOriginal;
            if (Owner.TryGetComponent<Player>(out Player player))
            {
                player.ShakeScreen(ScreenShakeForce * og.ScreenShakeForceMultiplier, ScreenShakeDecay * og.ScreenShakeDecayMultiplier, MaxScreenShake * og.ScreenShakeMaxMultiplier);
            }
        }

        RemainingPrimaryAttackCooldown = Time.time + (PrimaryAttackCooldown * ROFMult);
        Debug.Log("ROFMult is " + ROFMult);
    }

    //Loads a SINGLE round of the currently-equipped ammo type. This allows the player to load individual clips with all sorts of different ammo types.
    public virtual bool ReloadWeapon(AmmoHolder holder)
    {
        if (ReloadCDGameTime > Time.time || RemainingPrimaryAttackCooldown > Time.time)
        {
            Debug.Log("This weapon's reload is on cooldown.");
            return false;
        }

        if (holder.AmmoCount < 1 && !holder.Infinite)
        {
            Debug.Log("You're out of that ammo type!"); //TODO: Turn this into a HUD notification with a sound effect. Optimally, the game should automatically change the equipped ammo type if you run out.
            return false;
        }

        //TODO: Start a reload sequence which performs this process after a delay. For now, it will just insta-reload.
        float ClipSpace = ClipSize - AmountLoaded;

        if (holder.AmmoType.TryGetComponent<Projectile>(out Projectile AmmoType))
        {
            if (ClipSpace < AmmoType.ClipWeight)
            {
                Debug.Log("There's not enough room in the clip for that ammo type!"); //TODO: Turn this into a HUD notification with a sound effect
                return false;
            }
            else
            {
                LoadedProjectiles.Enqueue(holder.AmmoType);
                AmountLoaded += AmmoType.ClipWeight;
                if (!holder.Infinite)
                {
                    holder.AmmoCount--;
                }

                PlaySound(AmmoType.ReloadSounds, transform.position);
            }
        }

        ReloadCDGameTime = Time.time + ReloadCD;
        FreshProjectileBonus = 0f;
        return true;
    }

    //Loads a SINGLE round of this weapon's default ammo type. Used primarily to fill the weapon with ammo when it initially spawns, so the player can test it out without wasting the ammo they already have.
    public virtual bool ReloadWeapon()
    {
        float ClipSpace = ClipSize - AmountLoaded;

        if (StartingAmmo.TryGetComponent<Projectile>(out Projectile AmmoType))
        {
            if (ClipSpace < AmmoType.ClipWeight)
            {
                Debug.Log($"There was not enough room, clipspace is {ClipSpace} and weight is {AmmoType.ClipWeight}.");
                return false;
            }
            else
            {
                LoadedProjectiles.Enqueue(StartingAmmo);
                AmountLoaded += AmmoType.ClipWeight;
            }
        }

        return true;
    }

    //Plays a sound. If the source of the sound is the player, the sound is global (can be heard at max volume everywhere), otherwise it is local to the sound source.
    //TODO: Create a utility class and add this to it.
    public void PlaySound(AudioClip[] Sounds, Vector2 SoundPos)
    {
        if (Sounds.Length == 0)
            return;

        if (Owner.GetType() == typeof(Player))
        {
            SoundPos = Camera.main.transform.position;
        }

        AudioSource.PlayClipAtPoint(Sounds[Random.Range(0, Sounds.Length)], SoundPos, ShootVolume);
    }

    //Allows the weapon to be visually held and rotated around the user, instead of simply copying the holder's position.
    public virtual void HoldWeapon(Vector3 Position, Vector3 AimLoc)
    {
        if (Renderer == null)
            return;

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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (State != WeaponState.DROPPED)
            return;

        Player other = collision.gameObject.GetComponent<Player>();
        if (other != null)
        {
            //other.DropWeapon();
            other.GrabWeapon(gameObject);
            Owner = other.gameObject;
            Destroy(GetComponent<BoxCollider2D>());
        }
    }
}
