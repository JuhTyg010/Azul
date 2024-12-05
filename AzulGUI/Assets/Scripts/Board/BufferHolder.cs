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
            bufferTiles[i].Init(Globals.EMPTY_CELL, this);
        }
        
    }

    public void UpdateData(int typeId, int count) {
        type = typeId;
        for (int i = 0; i < size; i++) {
            bufferTiles[i].SetTile(i < count ? typeId : Globals.EMPTY_CELL);
        }
    }
    

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
        if (gameController.holding.isHolding) {
            if (gameController.CanPlaceFromHand(size - 1)) {
                foreach (var tile in bufferTiles) {
                    tile.SetColor(new Color(0, 0, 0, .3f));
                }
            }
            else {
                foreach (var tile in bufferTiles) {
                    tile.SetColor(new Color(1, 0, 0, .3f));
                }
            }
        }
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
        foreach (var tile in bufferTiles) {
            tile.SetColor(new Color(0, 0, 0, 0));
        }
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        if (gameController.holding.isHolding) {
            gameController.TryPlaceFromHand(size - 1);
        }
    }
}
