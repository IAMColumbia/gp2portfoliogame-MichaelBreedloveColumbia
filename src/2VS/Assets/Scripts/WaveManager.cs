using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { ACTIVE, VICTORY }
public class WaveManager : MonoBehaviour
{
    public Wave[] Waves;    //List of waves

    public int CurrentWave = 0;    //Current active wave, can be changed in the Unity editor to skip to a specific wave.

    public GameState State = GameState.ACTIVE;

    //TODO: Small pauses between waves, automatic wave advances if the player takes too long?

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0);
        StartNewWave();
    }

    // Update is called once per frame
    void Update()
    {
        if (Waves[CurrentWave - 1].AllEnemiesAreDead() && Waves[CurrentWave - 1].State == WaveState.ACTIVE)
        {
            StartNewWave();
        }
    }

    void StartNewWave()
    {
        if (CurrentWave >= Waves.Length)
        {
            State = GameState.VICTORY;
            return;
        }

        Waves[CurrentWave].SpawnEnemies();
        CurrentWave++;
    }
}
