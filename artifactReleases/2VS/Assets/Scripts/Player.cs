using Mono.Cecil.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

enum PLAYER_STATE { IDLE, ACTIVE };

public class Player : LivingEntity
{
    //Public variables:

    //public UIDocument HUD;
    //private LoadoutScrollHUD loadoutHUD;

    public List<GameObject> Weapons = new List<GameObject>();    //Current equipped weapon.
    public int WeaponIndex = 0;

    public float MaxSpeed = 10f;    //Max movement speed.
    public float Acceleration = 0.1f;   //Amount to increase movement speed per frame while moving.
    public float Deceleration = 0.1f;   //Amount to decrease movement speed per frame while not moving.
    public float DashSpeed = 20f;       //Speed override while dashing
    public float DashTime = 0.66f;      //Duration the dash is active for
    public float DashCooldown = 1f;     //Dash cooldown
    public float WeaponSwitchCD = 0.1f;    //Cooldown between switching weapons
    float WeaponSwitchGameTime = 0f;
    public float AmmoSwitchCD = 0.1f;    //Ammo switch cooldown
    float AmmoSwitchGameTime = 0f;

    public GameObject DashTrail; //Trail used while dashing, for cosmetic effect

    public event EventHandler FloorClimbed;
    public int CurrentFloor = 0;

    //Private variables:
    PLAYER_STATE State = PLAYER_STATE.IDLE; //Current state.

    float Speed = 0f;   //Current movement speed.
    Weapon CurrentWeapon; //Weapon script of the player's current weapon
    float RemainingDashTime = 0f;
    float RemainingDashCooldown = 0f;
    GameObject CurrentTrail;

    HealthHUD healthHUD;

    public List<GameObject> AmmoList = new List<GameObject>();
    public int AmmoIndex = 0;

    float CameraShakeForce = 0;
    float CameraShakeReduction = 0;
    public float MaxCamShake = 2.0f;

    // Start is called before the first frame update
    public override void Start()
    {
        State = PLAYER_STATE.ACTIVE;
        Renderer = GetComponent<SpriteRenderer>();
        ClimbFloor();

        if (Weapons.Count > 0)
        {
            UpdateWeapon();
        }
    }

    public void ClimbFloor()
    {
        FloorClimbed.Invoke(this, new EventArgs());
        CurrentFloor++;
    }

    public override void Think()
    {
        UpdateSprite();
        UpdatePosition();
        HoldWeapon();
        DetectShoot();
        DetectDash();
        DetectMouseWheel();
        DetectReload();
        UpdateScreenShake();
    }

    private void UpdateScreenShake()
    {
        if (CameraShakeForce > 0)
        {
            Camera.main.transform.position = new Vector3(transform.position.x + UnityEngine.Random.Range(-CameraShakeForce, CameraShakeForce), transform.position.y + UnityEngine.Random.Range(-CameraShakeForce, CameraShakeForce), Camera.main.transform.position.z);
            CameraShakeForce -= CameraShakeReduction;
            if (CameraShakeForce <= 0)
            {
                CameraShakeForce = 0;
            }
        }
        else
        {

            Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, Camera.main.transform.position.z);
        }
    }

    void DetectReload()
    {
        if (CurrentWeapon != null && Input.GetKey(KeyCode.Mouse1))
        {
            CurrentWeapon.ReloadWeapon(AmmoList[AmmoIndex].GetComponent<AmmoHolder>());
        }
    }

    void DetectMouseWheel()
    {
        Vector2 delta = Input.mouseScrollDelta;
        bool WantsToSwitchAmmo = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.Mouse1);

        if (delta.y < 0) //Scrolling down
        {
            if (WantsToSwitchAmmo)
            {
                SwitchToAmmoSlot(AmmoIndex - 1);
            }
            else
            {
                SwitchToWeaponSlot(WeaponIndex - 1);
            }
        }
        else if (delta.y > 0) //Scrolling up
        {
            if(WantsToSwitchAmmo)
            {
                SwitchToAmmoSlot(AmmoIndex + 1);
            }
            else
            {
                SwitchToWeaponSlot(WeaponIndex + 1);
            }
        }
    }

    void SwitchToWeaponSlot(int slot)
    {
        if (WeaponSwitchGameTime > Time.time)
            return;

        int OldSlot = WeaponIndex;

        if (slot >= Weapons.Count)
        {
            slot = 0;
        }
        else if (slot < 0)
        {
            slot = Weapons.Count - 1;
        }

        if (slot == OldSlot)
            return;

        if (CurrentWeapon != null)
        {
            CurrentWeapon.gameObject.transform.position = new Vector2(999999, 999999);  //Put it WAY off-screen instead of just deleting it so that it doesn't lose its loaded ammo or modified stats
        }

        WeaponIndex = slot;
        UpdateWeapon();

        WeaponSwitchGameTime = Time.time + WeaponSwitchCD;
    }

    void SwitchToAmmoSlot(int slot)
    {
        if (AmmoSwitchGameTime > Time.time)
            return;

        int OldSlot = AmmoIndex;

        if (slot >= AmmoList.Count)
        {
            slot = 0;
        }
        else if (slot < 0)
        {
            slot = AmmoList.Count - 1;
        }

        if (slot == OldSlot)
            return;

        AmmoIndex = slot;
        Debug.Log("Switched to ammo type: " + AmmoList[slot].name);

        AmmoSwitchGameTime = Time.time + AmmoSwitchCD;
    }

    void DetectDash()
    {
        if (RemainingDashTime > 0f)
        {
            RemainingDashTime -= Time.deltaTime;
            if (RemainingDashTime <= 0f && CurrentTrail != null)
            {
                CurrentTrail.GetComponent<Trail>().Active = false;
                CurrentTrail.GetComponent<Trail>().CanBeDeleted = true;
            }
        }

        if (RemainingDashCooldown > 0f)
        {
            RemainingDashCooldown -= Time.deltaTime;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) && RemainingDashCooldown <= 0f)
        {
            RemainingDashTime = DashTime;

            if (CurrentTrail != null)
            {
                CurrentTrail.GetComponent<Trail>().Active = false;
                CurrentTrail.GetComponent<Trail>().CanBeDeleted = true;
            }

            CurrentTrail = Instantiate(DashTrail);
            CurrentTrail.GetComponent<Trail>().Owner = gameObject;
            CurrentTrail.GetComponent<Trail>().spriteRenderer = Renderer;
            CurrentTrail.GetComponent<Trail>().Active = true;
        }
    }

    void UpdateWeapon()
    {
        if (Weapons[WeaponIndex] == null)
            return;

        GameObject wep;
        if (!Weapons[WeaponIndex].GetComponent<Weapon>().WeaponAlreadyExists())
        {
           wep = Instantiate(Weapons[WeaponIndex]);
           Weapons[WeaponIndex] = wep;
        }
        else
        {
            wep = Weapons[WeaponIndex];
        }

        CurrentWeapon = wep.GetComponent<Weapon>();
        CurrentWeapon.Owner = gameObject;
        CurrentWeapon.PlaySound(CurrentWeapon.DeploySounds, transform.position);

        Debug.Log("Switched to weapon: " + wep.name);

        //var root = HUD.rootVisualElement;
        //loadoutHUD = root.Q<LoadoutScrollHUD>();
        //loadoutHUD.player = this;
    }

    //Sets the player's sprite based on given conditions.
    void UpdateSprite()
    {
        if (Renderer == null || Renderer.GetType() != typeof(SpriteRenderer))
        {
            throw new System.Exception("Player.Renderer was either null or not a SpriteRenderer.");
        }

        switch(State)
        {
            default:
                Renderer.sprite = DefaultSprite;
                break;
        }
    }

    //Updates the player's position, used to manage movement.
    void UpdatePosition()
    {
        float hAxis = Input.GetAxis("Horizontal");
        float vAxis = Input.GetAxis("Vertical");
        UpdateSpeed(hAxis, vAxis);

        if (gameObject.TryGetComponent<Rigidbody2D>(out var RB))
        {
            RB.AddForce(new Vector2(hAxis * Speed * Time.deltaTime, vAxis * Speed * Time.deltaTime), ForceMode2D.Force);
        }

        //transform.position = new Vector2(transform.position.x + (hAxis * Speed * Time.deltaTime),
        //    transform.position.y + (vAxis * Speed * Time.deltaTime));
    }

    //Updates the player's movement speed, used for acceleration and deceleration.
    void UpdateSpeed(float hAxis, float vAxis)
    {
        if (hAxis != 0f || vAxis != 0f) 
        { 
            Speed = Mathf.Clamp(Speed + Acceleration, 0f, MaxSpeed);

            if (RemainingDashTime > 0f)
            {
                Speed = DashSpeed;
            }
        }
        else { Speed = Mathf.Clamp(Speed - Deceleration, 0f, RemainingDashTime > 0f ? DashSpeed : MaxSpeed); }
    }

    //Used while a weapon is equipped, allows the player's sprite to visually "hold" the equipped weapon.
    void HoldWeapon()
    {
        if (CurrentWeapon == null || CurrentWeapon.GetType() != typeof(Weapon))
            return;

        CurrentWeapon.HoldWeapon(transform.position, Camera.main.ScreenToWorldPoint(Input.mousePosition));
    }

    //Detects if the player is trying to shoot, and tells their equipped weapon to fire if they are.
    void DetectShoot()
    {
        if (CurrentWeapon == null)
            return;

        if ((Input.GetMouseButton(0) && CurrentWeapon.FullAuto) || (Input.GetMouseButtonDown(0) && !CurrentWeapon.FullAuto))
        {
            CurrentWeapon.Shoot(Input.GetMouseButtonDown(0), AmmoList[AmmoIndex].GetComponent<AmmoHolder>());
        }
    }

    public override void TakeDamage(float damage, bool IgnoreIFrames)
    {
        if (RemainingDashTime > 0f)
            return;

        base.TakeDamage(damage, IgnoreIFrames);
    }

    public void DropWeapon()
    {
        if (CurrentWeapon == null)
            return;

        Weapons.Remove(CurrentWeapon.gameObject);
        if (WeaponIndex >= Weapons.Count)
            WeaponIndex = Weapons.Count - 1;

        Destroy(CurrentWeapon.gameObject);
        UpdateWeapon();
    }

    public void GrabWeapon(GameObject weapon)
    {
        Weapons.Add(weapon);
        SwitchToWeaponSlot(Weapons.IndexOf(weapon));
        weapon.GetComponent<Weapon>().State = WeaponState.IDLE;
    }

    public void ShakeScreen(float force, float reductionperframe, float maxshake)
    {
        CameraShakeForce += force;
        if (CameraShakeForce > maxshake)
            CameraShakeForce -= force;
        if (CameraShakeForce > MaxCamShake)
            CameraShakeForce = MaxCamShake;

        CameraShakeReduction = reductionperframe;
    }
}
