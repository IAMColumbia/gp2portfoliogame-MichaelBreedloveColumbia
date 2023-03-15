using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

enum PLAYER_STATE { IDLE, ACTIVE };

public class Player : LivingEntity
{
    //Public variables:

    public GameObject Weapon;    //Current equipped weapon.

    public float MaxSpeed = 10f;    //Max movement speed.
    public float Acceleration = 0.1f;   //Amount to increase movement speed per frame while moving.
    public float Deceleration = 0.1f;   //Amount to decrease movement speed per frame while not moving.
    public float DashSpeed = 20f;       //Speed override while dashing
    public float DashTime = 0.66f;      //Duration the dash is active for
    public float DashCooldown = 1f;     //Dash cooldown

    public Text HealthText; //Text used to indicate health

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

    // Start is called before the first frame update
    public override void Start()
    {
        State = PLAYER_STATE.ACTIVE;
        Renderer = GetComponent<SpriteRenderer>();
        ClimbFloor(CurrentFloor);
    }

    public void ClimbFloor(int floor)
    {
        FloorClimbed.Invoke(this, new EventArgs());
        CurrentFloor++;
    }

    public override void Think()
    {
        UpdateWeapon();
        UpdateSprite();
        UpdatePosition();
        HoldWeapon();
        DetectShoot();
        DetectDash();
        UpdateHP();
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

    void UpdateHP()
    {
        HealthText.text = $"Health: {HP}/{MaxHP}";
        HealthText.transform.position = new Vector2(transform.position.x, transform.position.y + 0.5f);
    }

    void UpdateWeapon()
    {
        if (Weapon == null)
            return;

        CurrentWeapon = Weapon.GetComponent<Weapon>();
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
            CurrentWeapon.Shoot(Input.GetMouseButtonDown(0));
        }
    }

    public override void TakeDamage(float damage, bool IgnoreIFrames)
    {
        if (RemainingDashTime > 0f)
            return;

        base.TakeDamage(damage, IgnoreIFrames);
        UpdateHP();
    }

    public void DropWeapon()
    {
        if (CurrentWeapon == null)
            return;

        Destroy(CurrentWeapon.gameObject);
    }

    public void GrabWeapon(GameObject weapon)
    {
        Weapon = weapon;
        CurrentWeapon = weapon.GetComponent<Weapon>();
    }
}
