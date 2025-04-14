using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ToastManager : MonoBehaviour
{
    public static ToastManager Instance;

    [Header("Main Toast Panel")]
    public GameObject toastPanel;  // Parent panel

    [Header("Text Panel")]
    public GameObject textPanel;   // Child panel containing text
    public Text messageText;      // Text component inside textPanel

    private CanvasGroup toastCanvasGroup;
    private Coroutine currentToastRoutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        InitializeToastSystem();
    }

    void InitializeToastSystem()
    {
        // Ensure panels start hidden
        if (toastPanel != null)
        {
            toastCanvasGroup = toastPanel.GetComponent<CanvasGroup>();
            if (toastCanvasGroup == null)
            {
                toastCanvasGroup = toastPanel.AddComponent<CanvasGroup>();
            }
            toastPanel.SetActive(false);
        }

        if (textPanel != null)
        {
            textPanel.SetActive(false);
        }
    }

    public void ShowToast(string message, float duration = 2f)
    {
        if (toastPanel == null || textPanel == null || messageText == null)
        {
            Debug.LogError("ToastManager references not set in inspector!");
            return;
        }

        // Cancel existing toast if any
        if (currentToastRoutine != null)
        {
            StopCoroutine(currentToastRoutine);
        }

        // Set up panels
        messageText.text = message;
        toastPanel.SetActive(true);
        textPanel.SetActive(true);
        toastCanvasGroup.alpha = 1;

        currentToastRoutine = StartCoroutine(HideToastAfterDelay(duration));
    }

    private IEnumerator HideToastAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Fade out both panels together
        float fadeDuration = 0.5f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            toastCanvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Disable both panels
        toastPanel.SetActive(false);
        textPanel.SetActive(false);
        currentToastRoutine = null;
    }
}