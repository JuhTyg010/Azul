using Azul;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

namespace Board {
    public class WallTile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
        private Vector2Int position;
        private Image image;
        private bool isFilled;
        private int id;
        private GameController gameController;
        private WallHandler wallController;

        private Color emptyColor = new Color(0, 0, 0, 0);
        private Color possibleColor = new Color(0, 1, 0, .3f);
        private Color impossibleColor = new Color(1, 0, 0, .3f);
        private Color filledColor = new Color(1, 1, 1, 1);


        public void Initialize(int row, int col, WallHandler wallControl) {
            position = new Vector2Int(row, col);
            image = GetComponent<Image>();
            image.color = emptyColor;
            gameController = FindObjectOfType<GameController>();
            wallController = wallControl;
            id = Globals.EMPTY_CELL;
        }

        public void FillTile(int typeId) {
            isFilled = true;
            id = typeId;
            image.sprite = gameController.GetTileSprite(id);
        }


        public void OnPointerEnter(PointerEventData eventData) {
            //TODO: ask wall if it's possible to choose correct color only if game in correct state
            if (!isFilled) {
               // if(wallController.IsPossiblePosition(position.x, position))
            }
        }

        public void OnPointerExit(PointerEventData eventData) {
            if (!isFilled) {
                image.color = emptyColor;
            }
        }

        public void OnPointerClick(PointerEventData eventData) {
            throw new System.NotImplementedException();
        }
    }

}