using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class endGamePanel : MonoBehaviour {
    [SerializeField] private GameObject winnerText;
    [SerializeField] private GameObject tableText;

    private string winnerName;

    public void setWinner(string name) {
        winnerName = name;
        winnerText.name = $"Winner : {name}";
    }

    public void setTable(Dictionary<string, int> table) {
        string tableString = "";
        foreach (KeyValuePair<string, int> pair in table) {
            tableString += $"{pair.Key} : {pair.Value}\n";
        }

        tableText.name = tableString;
    }

    public void OnClickToMenu() {
        SceneManager.LoadScene("Menu");
    }
}
