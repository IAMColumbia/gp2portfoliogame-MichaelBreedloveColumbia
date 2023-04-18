using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RoomType    //What type of room is this?
{
    NORMAL,
    ITEM,
    SHOP,
    BOSS
};

public class Room : MonoBehaviour //Would've been better to do an interface for this (IRoom) and have the types inherit from it, but I'm way too deep into this by now and don't have that kind of time for the POC.
{
    public RoomType RoomType;

    public List<Vector2> DoorDirections;

    public List<Enemy> Enemies;     //Enemies contained in this room. TODO: Rework the LivingEntity system to have a root "Entity"
        
    public int Floor = 0;       //Floor associated with this room. TODO: Convert this to an enum once I figure out the floor names
    public int Distance = 0;    //The distance between this room and the starting room on the same floor.

    public int Width;   //How wide is this room, from the center?
    public int Height;  //How tall is this room, from the center?
    public int GridHeight;  //From its center, how tall is this room on the grid?
    public int GridWidth;   //From its center, how wide is this room on the grid?

    public bool IsEnd = false;  //Is this room an end piece?
    public bool IsBoss = false; //Is this room a boss room?

    public List<Door> Doors = new List<Door>(); //All of this room's doors.

    //public RoomTrigger roomTrigger;

    private void Start()
    {
        CreateRoomDirs();
    }

    //Stores all of the directions of all of this room's doors in a list.
    public void CreateRoomDirs()
    {
        DoorDirections.Clear();

        foreach (Door door in Doors)
        {
            DoorDirections.Add(door.RoomDir);
        }
    }

    //Turns the doors' colliders on or off.
    public void ToggleColliders(bool enabled)
    {
        foreach(Door door in Doors)
        {
            door.GetComponent<BoxCollider2D>().enabled = enabled;
        }
    }
}
