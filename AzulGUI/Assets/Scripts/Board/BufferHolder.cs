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

    private void Awake() {
        image = GetComponent<Image>();
        gameController = FindObjectOfType<GameController>();
        type = Globals.EMPTY_CELL;
    }

    public void LoadData(int typeId, int count) {
        //TODO: use some math to determine positions and generate tiles
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
        if (gameController.isHolding) {
            var data = gameController.GetHoldingData();
            if (type == (int) data.x || type == Azul.Globals.EMPTY_CELL) {
                image.color = new Color(0, 0, 0, .3f);
            }
            else {
                image.color = new Color(1, 0, 0, .3f);
            }
        }
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
        image.color = new Color(1, 1, 1, .3f);
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        if (gameController.isHolding) {
            var data = gameController.GetHoldingData();
            gameController.TryPlaceFromHand(size - 1);
        }
    }
}
