using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TTT.UI
{
    /// <summary>
    /// Countdown readout for current turn. Supports TMP_Text and/or Image (radial fill).
    /// Call Set(secondsRemaining) whenever a new state arrives.
    /// </summary>
    [AddComponentMenu("TTT/UI/Countdown View")]
    public class CountdownView : MonoBehaviour
    {
        public TMP_Text text;       // optional, "00:29"
        public Image radialFill;    // optional, Image with filled radial
        public float maxSeconds = 30f;

        private float _remaining;

        public void Set(float secondsRemaining)
        {
            _remaining = Mathf.Max(0f, secondsRemaining);
            UpdateUI();
        }

        void Update()
        {
            if (_remaining > 0f)
            {
                _remaining -= Time.deltaTime;
                if (_remaining < 0f) _remaining = 0f;
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            if (text)
            {
                int s = Mathf.CeilToInt(_remaining);
                text.text = $"00:{s:00}";
            }
            if (radialFill)
            {
                float denom = Mathf.Max(0.01f, maxSeconds);
                radialFill.fillAmount = Mathf.Clamp01(_remaining / denom);
            }
        }
    }
}
