using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    public event EventHandler RoomEntered;
    bool IsRoomAlreadyEntered = false;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player") || IsRoomAlreadyEntered)
            return;

        RoomEntered.Invoke(this, new EventArgs());
        IsRoomAlreadyEntered= true;
        //Destroy(this);
    }
}
