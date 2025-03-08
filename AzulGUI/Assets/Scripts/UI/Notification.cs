
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Notification : MonoBehaviour {
    
    [SerializeField] private float notificationDuration;
    
    private TMP_Text text;
    private Image image;
    private bool notificationActive;
    private float timer;
    private CanvasGroup canvasGroup;
    private int iters;
    private bool isVibrating;

   
    
    private void Awake() {
        text = GetComponentInChildren<TMP_Text>();
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        notificationActive = false;
        gameObject.SetActive(false);
    }

    public void ShowStableMessage(string message) {
        notificationActive = true;
        text.text = message;
        gameObject.SetActive(true);
    }
    
    public void ShowMessage(string message) {
        notificationActive = true;
        text.text = message;
        Color color = image.color;
        color.a = 1;
        image.color = color;
        timer = notificationDuration;
        gameObject.SetActive(true);
    }

    public void ShowLongMessage(string message) {
        notificationActive = true;
        text.text = message;
        Color color = image.color;
        color.a = 1;
        image.color = color;
        timer = notificationDuration * 2;
        gameObject.SetActive(true);
    }

    public void StartVibratingMessage(string message) {
        notificationActive = true;
        isVibrating = true;
        text.text = message;
        gameObject.SetActive(true);
        
    }

    public void StopVibratingMessage() {
        isVibrating = false;
        notificationActive = false;
        iters = 0;
        gameObject.SetActive(false);
    }
    
    void Update() {
        /*if (notificationActive) {
            if (isVibrating) {
                if (iters < 40) {
                    Vector3 temp = gameObject.GetComponent<RectTransform>().localScale;
                    temp.x += .01f;
                    temp.y += .01f;
                    temp.z += .01f;
                    gameObject.GetComponent<RectTransform>().localScale = temp;

                }
                else if(iters > 40) {
                    Vector3 temp = gameObject.GetComponent<RectTransform>().localScale;
                    temp.x -= .01f;
                    temp.y -= .01f;
                    temp.z -= .01f;
                    gameObject.GetComponent<RectTransform>().localScale = temp;
                    if(iters > 80) iters = -1;
                }
                iters++;
            }
            else {
                Color color = image.color;
                color.a -= Time.deltaTime / timer;
                text.color = new Color(text.color.r, text.color.g, text.color.b, color.a);
                if (color.a <= 0) {
                    notificationActive = false;
                    gameObject.SetActive(false);
                }

                image.color = color;
            }
        }*/
    }
}
