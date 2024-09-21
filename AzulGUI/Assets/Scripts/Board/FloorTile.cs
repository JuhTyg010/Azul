using System.Collections;
using System.Collections.Generic;
using Azul;
using Board;
using UnityEngine;
using UnityEngine.UI;

public class FloorTile : MonoBehaviour {


    [SerializeField] private Sprite firstSprite;
    private int id;
    private FloorHandler handler;
    private GameController gameController;
    private Image image;
    private bool isSet;

    private Color emptyColor = new Color(0, 0, 0, 0);
    private Color filledColor = new Color(1,1,1,1);

    public void Init(int id_, FloorHandler handler_) {
        id = id_;
        handler = handler_;
        gameController = GameObject.FindObjectOfType<GameController>();
        image = GetComponent<Image>();

        if (id == Globals.EMPTY_CELL) {
            isSet = false;
            image.color = emptyColor;
        }
        else {
            isSet = true;
            image.color = filledColor;
            image.sprite = gameController.GetTileSprite(id);
        }
    } 
    
    public void SetTile(int id) {
        this.id = id;

        if (id == Globals.EMPTY_CELL) {
            image.color = emptyColor;
        }
        else {
            isSet = true;
            image.color = filledColor;
            if (id == Globals.FIRST) image.sprite = firstSprite;
            else image.sprite = gameController.GetTileSprite(id);
        }
    }
}
