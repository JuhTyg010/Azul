using Azul;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

namespace Board {
    public class BufferTile : MonoBehaviour  {

        private int id;
        private GameController gameController;
        private BufferHolder myBuffer;
        private bool isSet;
        private Image image;

        private Color emptyColor = new Color(0, 0, 0, 0);
        private Color possibleColor = new Color(0, 1, 0, .3f);
        private Color impossibleColor = new Color(1, 0, 0, .3f);
        private Color filledColor = new Color(1, 1, 1, 1);
        

        public void Init(int id, BufferHolder buffer) {
            this.id = id;
            gameController = FindObjectOfType<GameController>();
            myBuffer = buffer;
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

        public void ResetTile() {
            isSet = false;
            image.color = emptyColor;
            id = Globals.EMPTY_CELL;
            image.sprite = null;
        }

        public void SetTile(int id) {
            this.id = id;
            image.color = emptyColor;

            if (id != Globals.EMPTY_CELL) {
                isSet = true;
                image.color = filledColor;
                image.sprite = gameController.GetTileSprite(id);
            }
        }

        public void SetColor(Color color) {
            if (!isSet) {
                image.color = color;
            }
        }
    }

}
