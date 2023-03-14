using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

//CURRENT BUGS:
//  - Doors that lead directly into walls still occur. In theory, this should not block off portions of the map, so every room should still be accessible, but it looks bad.
//  - If the minimum path distance is too low, the starting room's doors *can* lead to nowhere/out of the map.
//  - Floors generated are way smaller than what their settings should theoretically allow.
//  - Variable room width/height is not supported and will require a code restructure to work.

public class FloorManager : MonoBehaviour
{
    public int[] MinRoomDistance;   //Maximum distance between rooms. Array is used to store different settings per-floor.
    public int[] MaxRoomDistance;   //^ Minimum
    public int[] MinPathDistance;   //Maximum number of rooms spawned in any direction from the starting room. Array is used to store different settings per-floor.
    public int[] MaxPathDistance;   //^ Minimum

    public GameObject[] StartingRooms; //All starting rooms.
    public GameObject[] UpRooms;       //All rooms with a door on the top.
    public GameObject[] DownRooms;     //All rooms with a door on the bottom.
    public GameObject[] LeftRooms;     //All rooms with a door on the left.
    public GameObject[] RightRooms;    //All rooms with a door on the right.

    private List<GameObject> Rooms = new List<GameObject>();    //All current rooms.

    public Player player;   //The player.

    int floor;  //Current floor.
    public int[] MaxRooms; //Max number of rooms generated. Array is used to store different settings per-floor.
    public LayerMask RoomMask;  //LayerMask used by rooms.
    public LayerMask DoorMask;  //LayerMask used by doors.
    public LayerMask CheckMask; //LayerMask used for intersection detection.

    Dictionary<Vector2, bool> Occupied = new Dictionary<Vector2, bool>();   //A collection of room locations, and whether or not there is already a room there.
    Dictionary<Vector2, Vector2> Orientations = new Dictionary<Vector2, Vector2>();     //A list of Vector2 directions and their opposites.
    public GameObject RoomChecker;  //GameObject to be used to detect room intersection.

    //Adds a room to the floor. Utilizes recursion to add rooms to every door in every room.
    void AddRoom(GameObject LastRoom, Door LastDoor, int Distance, int ChosenDistance, bool ForceEnd = false)
    {
        if (Distance >= ChosenDistance)
        {
            Debug.Log("Reached the end of this path, chosen distance was " + ChosenDistance);
            return;
        }

        if (Rooms.Count >= MaxRooms[floor])
        {
            return;
        }

        if (LastRoom.TryGetComponent<Room>(out var roomScr))
        {
            List<GameObject> RoomList = null;

            if (LastDoor.RoomDir == Vector2.up) { RoomList = DownRooms.ToList<GameObject>(); }
            else if (LastDoor.RoomDir == Vector2.down) { RoomList = UpRooms.ToList<GameObject>(); }
            else if (LastDoor.RoomDir == Vector2.left) { RoomList = RightRooms.ToList<GameObject>(); }
            else if (LastDoor.RoomDir == Vector2.right) { RoomList = LeftRooms.ToList<GameObject>(); }

            Debug.Log("NEXT PIECE - MOVING IN THE DIRECTION OF " + LastDoor.RoomDir.x + ", " + LastDoor.RoomDir.y);

            if (RoomList == null)
                return;     //Ew, null pattern, I'll fix it later

            //NOTE: This has way too much "getcomponent", it's horribly unoptimized. Must be fixed before the final.
            //Search for a random room which has the right number of doors and the proper door positions:

            Vector3 NextRoomPos;
            int ChosenDist = UnityEngine.Random.Range(MinRoomDistance[floor], MaxRoomDistance[floor]);
            NextRoomPos = LastDoor.Room.gameObject.transform.position + (new Vector3(LastDoor.RoomDir.x, LastDoor.RoomDir.y) * ChosenDist);
            Debug.Log("THE NEXT POSITION SHOULD BE " + NextRoomPos.x + ", " + NextRoomPos.y);

            //Make sure our chosen room has a door to match every door that leads into it:
            List <Door> intersectors = GetAllIntersectors(NextRoomPos, ChosenDist);
            List<Vector2> Required = new List<Vector2>();
            List<Vector2> Forbidden = new List<Vector2>();
            foreach (Door door in intersectors)
            {
                Required.Add(Orientations[door.RoomDir]);
            }

            RaycastHit2D hit;

            //This is unoptimal but it gets the job done, I'll optimize it later.
            //Make sure our chosen room does not have any doors that lead directly into a wall:
            Vector2 pos;
            if (!Required.Contains(Vector2.up))
            {
                pos = NextRoomPos + (Vector3.up * ChosenDist);

                if (GetRoomFromPos(pos) != null)
                {
                    Forbidden.Add(Vector2.up);
                    Debug.DrawLine(NextRoomPos, pos, Color.red);
                }
            }
            if (!Required.Contains(Vector2.down))
            {
                pos = NextRoomPos + (Vector3.down * ChosenDist);

                if (GetRoomFromPos(pos) != null)
                {
                    Forbidden.Add(Vector2.down);
                    Debug.DrawLine(NextRoomPos, pos, Color.red);
                }
            }
            if (!Required.Contains(Vector2.left))
            {
                pos = NextRoomPos + (Vector3.left * ChosenDist);

                if (GetRoomFromPos(pos) != null)
                {
                    Forbidden.Add(Vector2.left);
                    Debug.DrawLine(NextRoomPos, pos, Color.red);
                }
            }
            if (!Required.Contains(Vector2.right))
            {
                pos = NextRoomPos + (Vector3.right * ChosenDist);

                if (GetRoomFromPos(pos) != null)
                {
                    Forbidden.Add(Vector2.right);
                    Debug.DrawLine(NextRoomPos, pos, Color.red);
                }
            }

            GameObject NextRoom;

            //Choose the room we need to spawn, based on context:
            bool found;
            do
            {
                found = true;

                int slot = UnityEngine.Random.Range(0, RoomList.Count);
                NextRoom = RoomList[slot];
                NextRoom.GetComponent<Room>().CreateRoomDirs();

                Debug.Log("ROOM: " + NextRoom.name);

                bool isEnd = NextRoom.GetComponent<Room>().IsEnd;
                if (ForceEnd && !isEnd)
                {
                    found = false;
                    Debug.Log("FAILED - WE HAVE REACHED THE END OF THE MAP/PATH AND THIS IS NOT AN END PIECE");
                }
                else if (!ForceEnd && isEnd && Distance < MinPathDistance[floor])
                {
                    found = false;
                    Debug.Log("FAILED - IS END PIECE WHEN NOT READY TO END");
                }

                if (Required.Count > 0 && found)
                {
                    foreach(Vector2 requirement in Required)
                    {
                        Debug.Log("REQUIRED DIRECTION: " + requirement.x + ", " + requirement.y);

                        if (!NextRoom.GetComponent<Room>().DoorDirections.Contains(requirement))
                        {
                            found = false;
                            Debug.Log("(FAILED)");
                        }
                    }
                }

                if (Forbidden.Count > 0 && found)
                {
                    foreach (Vector2 forboden in Forbidden)
                    {
                        Debug.Log("FORBIDDEN DIRECTION: " + forboden.x + ", " + forboden.y);

                        if (NextRoom.GetComponent<Room>().DoorDirections.Contains(forboden))
                        {
                            found = false;
                            Debug.Log("(FAILED)");
                        }
                    }
                }

                RoomList.RemoveAt(slot);
            }
            while (RoomList.Count > 0 && !found); //Keep cycling until we find a valid room within our contextual requirements, and there are still rooms to choose from.

            if (NextRoom == null || RoomList.Count < 0 || !found)   //None of our rooms are valid for the current position/direction, so don't spawn another one.
            {
                Debug.Log("OUT OF VALID ROOM CHOICES\n-----------------");
                Debug.Log(Required.Count + " REQUIRED DIRS, " + Forbidden.Count + " FORBIDDEN DIRS, FORCE END: " + ForceEnd);
                return;
            }
            else
            {
                Debug.Log("(PASSED)");
            }

            Rooms.Add(Instantiate(NextRoom, NextRoomPos, Quaternion.identity));
            if (!Occupied.TryGetValue(NextRoomPos, out bool alreadythere))
            {
                Occupied.Add(NextRoomPos, true);
            }

            GameObject CurrentRoom = Rooms[Rooms.Count - 1];
            if (CurrentRoom.TryGetComponent<Room>(out var nextRoomScr))
            {
                nextRoomScr.Distance = Distance;

                foreach (Door door in RandomizeList(nextRoomScr.Doors))
                {
                    Distance++;
                    bool EndOfTheLine = (Distance >= ChosenDistance || Rooms.Count >= MaxRooms[floor]);

                    if (door.RoomDir != LastDoor.RoomDir * -1) //This prevents us from messing with the door we just came from.
                    {
                        if (EndOfTheLine)
                        {
                            door.MakeDormant();
                        }
                        else
                        {
                            nextRoomScr.ToggleColliders(false);
                            hit = Physics2D.Raycast(CurrentRoom.transform.position, door.RoomDir, ChosenDist, RoomMask);
                            RaycastHit2D doorHit = Physics2D.Raycast(CurrentRoom.transform.position, door.RoomDir, ChosenDist, DoorMask);
                            Vector2 chosenPos = NextRoomPos + ((Vector3)door.RoomDir * ChosenDist);
                            bool isoccupied;
                            bool exists = Occupied.TryGetValue(chosenPos, out isoccupied);
                            if (!exists)
                            {
                                Occupied.Add(chosenPos, true);
                                isoccupied = false;
                            }

                            nextRoomScr.ToggleColliders(true);
                            if ((!hit || hit.collider == null) && (!doorHit || doorHit.collider == null) && !isoccupied)    //There is not a room in the chosen position, do not spawn a door.
                            {
                                AddRoom(CurrentRoom, door, Distance, ChosenDistance, Distance == ChosenDistance - 1 || Rooms.Count == MaxRooms[floor] - 1);
                                Debug.DrawRay(door.transform.position, door.RoomDir * ChosenDist, Color.green, 999);
                            }
                            else //We have detected that there is already a room in the next position, so do not spawn another one there.
                            {
                                if (isoccupied)
                                {
                                    Debug.Log("Space is occupied");

                                    GameObject occupiedRoom = GetRoomFromPos(chosenPos);
                                    if (occupiedRoom != null)
                                    {
                                        if (occupiedRoom.TryGetComponent<Room>(out Room theRoom))
                                        {
                                            Debug.Log("Found a room in the occupied spot");
                                            if (!theRoom.DoorDirections.Contains(Orientations[door.RoomDir]))
                                            {
                                                door.MakeDormant();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    GameObject GetRoomFromPos(Vector2 pos)
    {
        foreach(GameObject room in Rooms)
        {
            if (new Vector2(room.transform.position.x, room.transform.position.y) == pos)
            {
                Debug.Log("Found a room in the given coordinate!");
                return room;
            }
        }

        return null;
    }

    //Used to determine the number of rooms which intend to be placed on the same space.
    List<Door> GetAllIntersectors(Vector3 Pos, int ChosenDist)
    {
        GameObject Checker = Instantiate(RoomChecker, Pos, Quaternion.identity);

        List<Door> Intersectors = new List<Door>();

        foreach (Door door in GetAllDoors())
        {
            RaycastHit2D check = Physics2D.Raycast(door.transform.position, door.RoomDir, ChosenDist, CheckMask);

            if (check.collider != null)
            {
                Intersectors.Add(door);
                Debug.Log("INTERSECTION DETECTED AT " + Pos.x + ", " + Pos.y);
            }
        }

        Destroy(Checker);
        return Intersectors;
    }

    List<Door> GetAllDoors()
    {
        List<Door> doors = new List<Door>();

        foreach (GameObject room in Rooms)
        {
            if (room.TryGetComponent<Room>(out var roomScr))
            {
                foreach (Door door in roomScr.Doors)
                {
                    doors.Add(door);
                }
            }
        }

        return doors;
    }

    void MakeMap()
    {
        //Clear the current floor to make room for the next one:
        foreach (GameObject removethisroom in Rooms)
        {
            Destroy(removethisroom);
        }

        Rooms.Clear();
        Occupied.Clear();

        GameObject room = StartingRooms[UnityEngine.Random.Range(0, StartingRooms.Length)];
        Rooms.Add(Instantiate(room, new Vector3(0, 0), Quaternion.identity));
        player.gameObject.transform.position = new Vector3(0, 0);
        Occupied.Add(new Vector2(0, 0), true);

        GameObject CurrentRoom = Rooms[Rooms.Count - 1];
        if (CurrentRoom.TryGetComponent<Room>(out var roomScr))
        {
            int ChosenDist = UnityEngine.Random.Range(MinRoomDistance[floor], MaxRoomDistance[floor]);

            foreach (Door door in RandomizeList(roomScr.Doors))
            {
                if (Rooms.Count >= MaxRooms[floor])
                {
                    door.MakeDormant();
                }
                else
                {
                    roomScr.ToggleColliders(false);
                    RaycastHit2D hit = Physics2D.Raycast(CurrentRoom.transform.position, door.RoomDir, ChosenDist, RoomMask);
                    RaycastHit2D doorHit = Physics2D.Raycast(CurrentRoom.transform.position, door.RoomDir, ChosenDist, DoorMask);
                    roomScr.ToggleColliders(true);

                    if ((!hit || hit.collider == null) && (!doorHit || doorHit.collider == null))
                    {
                        AddRoom(CurrentRoom, door, 1, UnityEngine.Random.Range(MinPathDistance[floor], MaxPathDistance[floor]));
                        Debug.DrawRay(door.transform.position, door.RoomDir * ChosenDist, Color.green, 999);
                    }
                }
            }
        }
    }

    //Randomizes a list. Used to randomize the order in which doors spawn adjacent rooms, otherwise floor generation is heavily biased in favor of always spawning doors to the left.
    List<Door> RandomizeList(List<Door> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Door temp;
            temp = list[i];
            int j = UnityEngine.Random.Range(i, list.Count - 1);
            list[i] = list[j];
            list[j] = temp;
        }

        return list;
    }

    //Advances to the next floor.
    public void ClimbFloor(object sender, EventArgs e)
    {
        MakeMap();
        floor = player.CurrentFloor;
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