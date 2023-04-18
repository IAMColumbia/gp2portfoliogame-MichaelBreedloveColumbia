using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class FloorManager : MonoBehaviour
{
    public Floor[] Floors;  //All of the floors used by the game.

    public Player player;   //The player.

    int floor = 0;  //Current floor.

    public LayerMask RoomMask;  //LayerMask used by rooms.
    public LayerMask DoorMask;  //LayerMask used by doors.
    public LayerMask CheckMask; //LayerMask used for intersection detection.

    Dictionary<Vector2, Vector2> Orientations = new Dictionary<Vector2, Vector2>();     //A list of Vector2 directions and their opposites.
    public GameObject RoomChecker;  //GameObject to be used to detect room intersection.

    void MakeMap()
    {
        for (int i = 0; i < Floors.Count(); i++)
        {
            Floors[i].DestroyFloor();
        }

        Floors[floor].player = player;
        Floors[floor].RoomMask = RoomMask;
        Floors[floor].DoorMask = DoorMask;
        Floors[floor].CheckMask =  CheckMask;
        Floors[floor].Orientations = Orientations;
        Floors[floor].RoomChecker = RoomChecker;

        Floors[floor].MakeMap();
    }

    //Advances to the next floor.
    public void ClimbFloor(object sender, EventArgs e)
    {
        if (player.CurrentFloor >= Floors.Length) //Go back to the first floor and infinitely loop the game.
        {
            player.CurrentFloor = 0;
        }

        floor = player.CurrentFloor;
        Debug.Log("Floors.length = " + Floors.Length);
        Debug.Log("Floor = " + floor);
        MakeMap();
    }

    public void Start()
    {
        player.FloorClimbed += ClimbFloor;

        Orientations.Add(Vector2.up, Vector2.down);
        Orientations.Add(Vector2.down, Vector2.up);
        Orientations.Add(Vector2.left, Vector2.right);
        Orientations.Add(Vector2.right, Vector2.left);
    }
}