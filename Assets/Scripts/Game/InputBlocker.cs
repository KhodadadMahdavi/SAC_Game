using UnityEngine;
using UnityEngine.UI;

namespace TTT.UI
{
    /// <summary>
    /// Attach to a full-screen Image with Raycast Target enabled.
    /// Toggle .enabled to block/unblock input during waits / not-your-turn.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("TTT/UI/Input Blocker")]
    public class InputBlocker : MonoBehaviour
    {
        private Image _img;
        void Awake()
        {
            _img = GetComponent<Image>();
            _img.raycastTarget = true;
            _img.enabled = false; // start disabled (invisible)
        }

        public void Block(bool on)
        {
            if (_img) _img.enabled = on;
        }
    }
}
