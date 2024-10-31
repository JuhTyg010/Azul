using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class endGamePanel : MonoBehaviour {
    [SerializeField] private GameObject winnerText;
    [SerializeField] private GameObject tableText;

    private string winnerName;

    public void setWinner(string name) {
        winnerName = name;
        winnerText.GetComponent<TMP_Text>().text = $"Winner : {name}";
    }

    public void setTable(Dictionary<string, int> table) {
        string tableString = "";
        foreach (KeyValuePair<string, int> pair in table) {
            tableString += $"{pair.Key} : {pair.Value}\n";
        }

        tableText.GetComponent<TMP_Text>().text = tableString;
    }

    public void OnClickToMenu() {
        SceneManager.LoadScene("Menu");
    }
}
