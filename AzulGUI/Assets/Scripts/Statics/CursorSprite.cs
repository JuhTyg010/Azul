using System.Collections;
using System.Collections.Generic;
using Board;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(SpriteRenderer))]
public class CursorSprite : MonoBehaviour {
    [SerializeField] private Vector2 size;
    private bool isVisible = false;
    private SpriteRenderer image;
    private GameController gameController;
    private GameObject canvas;
    // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<SpriteRenderer>();
        gameController = FindObjectOfType<GameController>();
        GetComponent<RectTransform>().sizeDelta = size;
        canvas = FindObjectOfType<Canvas>().gameObject;
    }
    
    void Update()
    {
        if (isVisible) {
            image.enabled = true;
            Vector2 cursorPos;
            /*RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform,
                Input.mousePosition,Camera.main,out cursorPos);
            /*cursorPos.x = Input.mousePosition.x / canvas.transform.localScale.x;
            cursorPos.y = Input.mousePosition.y / canvas.transform.localScale.y;  */  
            cursorPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = cursorPos;
        }
        else if (image.enabled) {
            image.enabled = false;
        }
    }

    public void SetVisible(bool visible, Sprite sprite) {
        isVisible = visible;
        image.sprite = sprite;
    }
}
