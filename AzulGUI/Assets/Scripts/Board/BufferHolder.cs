using Board;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Azul;

public class BufferHolder : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
    [SerializeField] private int size;
    private int type;
    
    private GameController gameController;
    private Image image;

    private void Start() {
        image = GetComponent<Image>();
        type = Globals.EMPTY_CELL;
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
            gameController.TryPlaceFromHand(size - 1);
        }
    }
}
