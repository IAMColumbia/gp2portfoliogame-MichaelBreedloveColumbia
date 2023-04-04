using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Player player;   //The player
    public WaveManager WaveManager; //Wave manager

    public Text HUD;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        DetectGameState();
    }

    void DetectGameState()
    {
        if ((player == null || WaveManager.State == GameState.VICTORY) && Input.GetKeyDown(KeyCode.Space))
        {
            SceneManager.LoadScene("MainGame");
        }

        if (player == null)
        {
            HUD.text = "You died! Press space to try again.";
        }
        else if (WaveManager.State == GameState.VICTORY)
        {
            HUD.text = "You won! Press space to play again.";
        }
        else
        {
            HUD.text = $"Wave: {WaveManager.CurrentWave}/{WaveManager.Waves.Length}";
        }
    }
}
