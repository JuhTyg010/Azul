using Board;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Azul;
using Unity.Collections;
using UnityEngine.PlayerLoop;

public class BufferHolder : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
    [SerializeField] private int size;
    [SerializeField] private Vector2 leftPosition;
    [SerializeField] private Vector2 offset;
    [SerializeField] private GameObject bufferTile;
    
    private int type;
    private int count;
    
    private GameController gameController;
    private Image image;
    private BufferTile[] bufferTiles;
    private void Awake() {
        image = GetComponent<Image>();
        gameController = FindObjectOfType<GameController>();
        type = Globals.EMPTY_CELL;

        bufferTiles = new BufferTile[size];
        for (int i = 0; i < size; i++) {
            var nextTile = Instantiate(bufferTile, transform);
            Vector2 realPosition = leftPosition;
            realPosition.x += offset.x * i;
            realPosition.y += offset.y * i;
            nextTile.GetComponent<RectTransform>().anchoredPosition = realPosition;
            bufferTiles[i] = nextTile.GetComponent<BufferTile>();
            bufferTiles[i].Initialize(Globals.EMPTY_CELL, this);
        }
        
    }

    public void LoadData(int typeId, int count) {
        //TODO: use some math to determine positions and generate tiles
        for (int i = 0; i < count; i++) {
            bufferTiles[i].SetTile(typeId);
        }
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
        if (gameController.isHolding && type == Globals.EMPTY_CELL) {
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
        if(type == Globals.EMPTY_CELL) image.color = new Color(1, 1, 1, .3f);
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        if (gameController.isHolding) {
            var data = gameController.GetHoldingData();
            gameController.TryPlaceFromHand(size - 1);
        }
    }
}
