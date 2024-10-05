
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


   
    
    private void Awake() {
        text = GetComponentInChildren<TMP_Text>();
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        notificationActive = false;
        gameObject.SetActive(false);
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
    
    void Update() {
        if (notificationActive) {
            Color color = image.color;
            color.a -= Time.deltaTime / timer;
            text.color = new Color(text.color.r, text.color.g, text.color.b, color.a);
            if (color.a <= 0) {
                notificationActive = false;
                gameObject.SetActive(false);
            }
            image.color = color;
        }
    }
}
