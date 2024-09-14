using System.IO;
using UnityEngine;
using Azul;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public void OnPlayButtonClicked() {
        var players = GameObject.FindObjectsOfType<PlayerConfig>();
        int playerCount = players.Length;
        string[] playerNames = new string[playerCount];
        for (int i = 0; i < playerCount; i++) {
            playerNames[i] = players[i].playerName;
        }
        
        using StreamWriter sw =  new StreamWriter(Path.Combine(Application.dataPath, "game_config.txt"));
        sw.Write($"{playerCount} ");
        for (int i = 0; i < playerCount; i++) {
            string fullName = players[i].isHuman ? "H_" : "B_";
            fullName += playerNames[i];
            sw.Write($"{fullName} ");
        }
        sw.Close();
        SceneManager.LoadScene("Game");
    } 
    
    public void OnAboutButtonClicked(){} //TODO: here will be the rules 
    
    public void OnCreditsButtonClicked(){} //TODO: this will open message with all the credits
    
}
