using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WaveState { INACTIVE, ACTIVE}
public class Wave : MonoBehaviour
{
    public GameObject[] Enemies; //An array containing prefabs for all enemies spawned by this wave.
    public int[] NumEnemies;    //An array detailing the number of each enemy contained in Enemies to spawn during this wave. In other words: value X in NumEnemies[Y] will spawn X of the Enemy type contained in Enemies[Y].

    public WaveState State = WaveState.INACTIVE;

    List<GameObject> LivingEnemies = new List<GameObject>();  //Enemies spawned by this wave which are still alive. If none are alive, WaveManager automatically begins the next wave.

    //TODO: Timed enemy spawns, random spawn orders?

    private void Start()
    {
        GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnEnemies()
    {
        for (int i = 0; i < Enemies.Length; i++)
        {
            for (int j = 0; j < NumEnemies[i]; j++)
            {
                GameObject enemy = Enemies[i];
                LivingEnemies.Add(Instantiate(enemy, GetRandomPositionOffScreen(), Quaternion.identity));
            }
        }

        State = WaveState.ACTIVE;
    }

    public bool AllEnemiesAreDead()
    {
        for (int i = 0; i < LivingEnemies.Count; i++)
        {
            if (LivingEnemies[i] != null)
            {
                return false;
            }
        }

        return true;
    }

    Vector2 GetRandomPositionOffScreen()
    {
        Vector3 WorldPoint = Camera.main.ScreenToWorldPoint(new Vector2(0, 0));

        return Camera.main.ScreenToWorldPoint(new Vector2(WorldPoint.x + GetRandomValue(Screen.width, Screen.width * 1.01f), WorldPoint.y + GetRandomValue(Screen.height, Screen.height * 1.01f)));
    }

    float GetRandomValue(float min, float max)
    {
        float val = Random.Range(min, max);
        if (FlipACoin()) { val *= -1; }
        return val;
    }

    bool FlipACoin()
    {
        return Random.Range(0, 2) == 1;
    }
}
