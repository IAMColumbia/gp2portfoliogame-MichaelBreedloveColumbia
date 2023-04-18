using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ExitState { DORMANT, ACTIVE };
public class FloorExit : MonoBehaviour
{
    public ExitState State = ExitState.DORMANT;
    public Sprite DormantSprite;
    public Sprite ActiveSprite;
    public GameObject Room;

    private void Start()
    {
        if (Room.TryGetComponent<Room>(out var roomScr))
        {
            if (roomScr.Enemies.Count == 0)
            {
                State = ExitState.ACTIVE;
            }
            else
            {
                foreach (Enemy enemy in roomScr.Enemies)
                {
                    enemy.EnemyDied += Enemy_EnemyDied;
                }
            }
        }

        UpdateSprite();
    }

    private void Enemy_EnemyDied(object sender, System.EventArgs e)
    {
        if (Room.TryGetComponent<Room>(out var roomScr))
        {
            roomScr.Enemies.Remove((Enemy)sender);
            if (roomScr.Enemies.Count < 1)
            {
                Activate();
            }
        }
    }

    void Activate()
    {
        State = ExitState.ACTIVE;
        UpdateSprite();
    }

    void UpdateSprite()
    {
        if (TryGetComponent<SpriteRenderer>(out var renderer))
        {
            if (State == ExitState.DORMANT)
            {
                renderer.sprite = DormantSprite;
            }
            else
            {
                renderer.sprite = ActiveSprite;
            }
        }
    }

    // Start is called before the first frame update
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (State == ExitState.DORMANT)
            return;

        if (collision.gameObject.TryGetComponent<Player>(out Player player))
        {
            player.ClimbFloor();
            Destroy(gameObject);
        }
    }
}
