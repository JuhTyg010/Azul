using System.IO;
using UnityEngine;
using Azul;
using TMPro;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TMP_Text infoText;
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

    public void OnQuitButtonClicked() {
        Application.Quit();
    }

    public void OnAboutButtonClicked() {
        infoText.text = Resources.Load<TextAsset>("About").text;
        infoPanel.SetActive(true);
    }


    public void OnCreditsButtonClicked() {
        infoText.text = Resources.Load<TextAsset>("Credits").text;
        infoPanel.SetActive(true);
    }

}
