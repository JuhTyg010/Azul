using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NextPlayerPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text textField;
    
    private string text;


    public void SetText(string text) {
        this.text = text;
        textField.text = text;
    }
}
