using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState { ACTIVE, VICTORY, DEFEAT }

public class GameManager : MonoBehaviour
{
    public Player player;   //The player
    GameState State = GameState.ACTIVE;
    [SerializeField] AudioClip WinSound;
    [SerializeField] AudioClip LoseSound;

    // Start is called before the first frame update
    void Start()
    {
        player.PlayerKilled += OnPlayerKilled;
        player.PlayerWon += OnPlayerWon;
    }

    // Update is called once per frame
    void Update()
    {
        DetectGameState();
    }

    void DetectGameState()
    {
        if ((State != GameState.ACTIVE) && Input.GetKeyDown(KeyCode.Space))
        {
            SceneManager.LoadScene("MainGame");
        }
    }

    public void OnPlayerKilled(object sender, EventArgs e)
    {
        State = GameState.DEFEAT;
        AudioSource.PlayClipAtPoint(LoseSound, Camera.main.transform.position, 0.5f);
    }

    public void OnPlayerWon(object sender, EventArgs e)
    {
        State = GameState.VICTORY;
        AudioSource.PlayClipAtPoint(WinSound, Camera.main.transform.position);
    }
}
