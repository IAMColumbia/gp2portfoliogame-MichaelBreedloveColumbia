using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LockState { LOCKED, UNLOCKED, LOCKED_REQUIRES_KEY };

public class Door : MonoBehaviour
{
    public GameObject Room; //The room associated with this door.
    public LockState LockStatus;    //Door lock state.
    public Sprite LockedSprite;     //Sprite to use while locked
    public Sprite KeyLockedSprite;  //Sprite to use while locked, if a key is required
    public Sprite UnlockedSprite;   //Sprite to use when unlocked
    public RoomTrigger Trigger;     //RoomTrigger entity which opens/closes this door
    public float XDist;             //Distance in unity units from the center of this door's room to the door itself.
    public float YDist;             //^ but for Y axis
    public Vector2 RoomDir;         //The direction this door is facing

    SpriteRenderer Renderer;
    BoxCollider2D Collider;

    private void Start()
    {
        Collider = GetComponent<BoxCollider2D>();
        Renderer = GetComponent<SpriteRenderer>();

        if (Room.TryGetComponent<Room>(out var roomScr))
        {
            foreach (Enemy enemy in roomScr.Enemies)
            {
                enemy.EnemyDied += Enemy_EnemyDied;
            }
        }

        Trigger.RoomEntered += Trigger_RoomEntered;

        UpdateLockStatus();
    }

    private void Trigger_RoomEntered(object sender, System.EventArgs e)
    {
        if (LockStatus == LockState.LOCKED || LockStatus == LockState.LOCKED_REQUIRES_KEY)
            return;

        if (Room.TryGetComponent<Room>(out var roomScr))
        {
            if (roomScr.Enemies.Count > 0)
            {
                Lock();
            }
        }
    }

    //Disables a door entirely and turns it into a regular wall. Used mainly for map generation.
    public void MakeDormant()
    {
        if (TryGetComponent<Tile>(out var tile))
        {
            tile.SetSprite();
        }

        Room.GetComponent<Room>().DoorDirections.Remove(RoomDir);

        Destroy(GetComponent<Door>());
    }

    //Uses observer pattern to detect when enemies inside of this door's room are killed. If it was the last enemy in the room, this door is unlocked.
    private void Enemy_EnemyDied(object sender, System.EventArgs e)
    {
        if (LockStatus == LockState.LOCKED_REQUIRES_KEY)
            return;

        if (Room.TryGetComponent<Room>(out var roomScr))
        {
            roomScr.Enemies.Remove((Enemy)sender);
            if (roomScr.Enemies.Count < 1)
            {
                Unlock();
            }
        }
    }

    //Locks the door.
    public void Lock()
    {
        LockStatus = LockState.LOCKED;
        UpdateLockStatus();
    }

    //Unlocks the door.
    public void Unlock()
    {
        LockStatus = LockState.UNLOCKED;
        UpdateLockStatus();
    }

    //Based on lock status, activates/deactivates the door's collider and updates its sprite.
    void UpdateLockStatus()
    {
        switch(LockStatus)
        {
            case LockState.LOCKED:
                Renderer.sprite = LockedSprite;
                Collider.enabled = true;
                break;
            case LockState.LOCKED_REQUIRES_KEY:
                Renderer.sprite = KeyLockedSprite;
                Collider.enabled = true;
                break;
            case LockState.UNLOCKED:
                Renderer.sprite = UnlockedSprite;
                Collider.enabled = false;
                break;
        }
    }
}
