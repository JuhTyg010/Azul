using System.IO;
using UnityEngine;
using Azul;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public void OnPlayButtonClicked() {
        var players = GameObject.FindObjectsOfType<PlayerConfig>();
        int playerCount = players.Length;
        string playerNames = "";
        string playerTypes = "";

        for (int i = 0; i < playerCount; i++) {
            playerNames += $"{players[i].playerName}\n";
            playerTypes += players[i].isHuman ? "Human" : "AI";
            playerTypes += "\n";
        }
        PlayerPrefs.SetInt("PlayerCount", playerCount);
        PlayerPrefs.SetString("PlayerNames", playerNames);
        PlayerPrefs.SetString("PlayerTypes", playerTypes);
        PlayerPrefs.Save();
        
        SceneManager.LoadScene("Game");
    } 
    
    public void OnAboutButtonClicked(){} //TODO: here will be the rules 
    
    public void OnCreditsButtonClicked(){} //TODO: this will open message with all the credits
    
}
