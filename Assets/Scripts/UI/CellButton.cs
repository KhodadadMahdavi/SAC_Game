using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TTT.Game
{
    /// <summary>
    /// A single board cell. Displays mark and raises click.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("TTT/Game/Cell Button")]
    public class CellButton : MonoBehaviour
    {
        public TMP_Text label; // shows "X","O",""
        public Image background; // optional

        private Button _btn;
        private int _index;

        public void Init(int index, Action onClicked)
        {
            _index = index;
            _btn = GetComponent<Button>();
            _btn.onClick.RemoveAllListeners();
            _btn.onClick.AddListener(() => onClicked?.Invoke());
        }

        public void SetInteractable(bool on)
        {
            if (!_btn) _btn = GetComponent<Button>();
            _btn.interactable = on;
        }

        public void SetMark(int mark) // 0 empty, 1 X, 2 O
        {
            if (label)
            {
                label.text = mark == 1 ? "<color=#FE4C40>X</color>" : mark == 2 ? "<color=#02A4D3>O</color>" : "";
            }
        }
    }
}
