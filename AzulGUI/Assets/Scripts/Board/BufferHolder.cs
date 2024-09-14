using System;
using System.Collections;
using System.Collections.Generic;
using Board;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BufferHolder : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
    [SerializeField] private int size;
    private int type;
    
    private GameController gameController;
    private Image image;

    private void Start() {
        image = GetComponent<Image>();
        type = Azul.Globals.EMPTY_CELL;
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
        if (gameController.isHolding) {
            var data = gameController.GetHoldingData();
            if (type == (int) data.x || type == Azul.Globals.EMPTY_CELL) {
                image.color = Color.black;
            }
            else {
                image.color = Color.red;
            }

        }
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
        image.color = Color.white;
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        if (gameController.isHolding) {
            var data = gameController.GetHoldingData();
            if (type == (int)data.x || type == Azul.Globals.EMPTY_CELL) {
                //TODO: place stuff
            }
            else {
                Debug.Log("What the fuck");
                //TODO: return to plate
            }
        }
    }
}
