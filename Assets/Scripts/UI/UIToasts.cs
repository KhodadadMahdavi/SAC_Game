using System.Collections;
using TMPro;
using UnityEngine;

namespace TTT.UI
{
    /// <summary>
    /// Lightweight toast. Place on a ToastLayer GameObject with a CanvasGroup and TMP_Text.
    /// Use UIToast.Instance.Show("message", 2f).
    /// </summary>
    [AddComponentMenu("TTT/UI/Toast")]
    public class UIToast : MonoBehaviour
    {
        public static UIToast Instance { get; private set; }

        public TMP_Text text;
        public CanvasGroup canvasGroup;
        [Range(0f, 1f)] public float maxAlpha = 1f;

        private Coroutine _routine;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (canvasGroup) canvasGroup.alpha = 0f;
        }

        public void Show(string message, float seconds = 2f)
        {
            if (text) text.text = message ?? "";
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(CoShow(seconds));
        }

        private IEnumerator CoShow(float seconds)
        {
            if (!canvasGroup)
                yield break;

            // Fade in
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, maxAlpha, t / 0.2f);
                yield return null;
            }
            canvasGroup.alpha = maxAlpha;

            // Hold
            float hold = Mathf.Max(0f, seconds);
            while (hold > 0f)
            {
                hold -= Time.unscaledDeltaTime;
                yield return null;
            }

            // Fade out
            t = 0f;
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(maxAlpha, 0f, t / 0.25f);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            _routine = null;
        }
    }
}
