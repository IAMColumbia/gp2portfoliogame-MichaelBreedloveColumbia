using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    public event EventHandler RoomEntered;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        RoomEntered.Invoke(this, new EventArgs());
        Destroy(this);
    }
}
