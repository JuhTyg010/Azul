using System.Collections;
using System.Collections.Generic;
using Board;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(SpriteRenderer))]
public class CursorSprite : MonoBehaviour {
    [SerializeField] private Vector2 size;
    [SerializeField] private TMP_Text text;
    private bool isVisible = false;
    private SpriteRenderer image;
    private GameController gameController;
    private GameObject canvas;
    // Start is called before the first frame update
    void Start() {
        image = GetComponent<SpriteRenderer>();
        gameController = FindObjectOfType<GameController>();
        text.gameObject.SetActive(false);
    }
    
    void Update() {
        if (isVisible) {
            image.enabled = true;
            text.gameObject.SetActive(true);
            Vector2 cursorPos;  
            cursorPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = cursorPos;
        }
        else if (image.enabled) {
            image.enabled = false;
        }
    }

    public void SetVisible(bool visible, Sprite sprite, string message = "") {
        isVisible = visible;
        image.sprite = sprite;
        text.text = message;
    }
}
