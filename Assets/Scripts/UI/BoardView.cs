using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TTT.Game
{
    /// <summary>
    /// Visual layer for the 3x3 board. Exposes click event and APIs to set marks.
    /// </summary>
    [AddComponentMenu("TTT/Game/Board View")]
    public class BoardView : MonoBehaviour
    {
        [Tooltip("Assign 9 CellButton components in index order (0..8).")]
        public CellButton[] cells = new CellButton[9];

        [Tooltip("Optional: images to highlight winning cells (3).")]
        public Image[] highlightOverlays; // size 3, enable for winning line

        public event Action<int> OnCellClicked;

        void Awake()
        {
            for (int i = 0; i < cells.Length; i++)
            {
                var idx = i;
                if (cells[i] != null)
                    cells[i].Init(idx, () => OnCellClicked?.Invoke(idx));
            }
            ClearHighlights();
        }

        public void SetBoard(int[] board)
        {
            for (int i = 0; i < 9 && i < board.Length; i++)
            {
                var mark = board[i];
                if (cells[i] != null) cells[i].SetMark(mark);
            }
        }

        public void ClearAll()
        {
            for (int i = 0; i < cells.Length; i++)
                if (cells[i]) cells[i].SetMark(0);
            ClearHighlights();
        }

        public void HighlightLine(int[] line) // length 3 indices
        {
            ClearHighlights();
            if (line == null || highlightOverlays == null || highlightOverlays.Length < 3) return;

            for (int i = 0; i < 3; i++)
            {
                int cellIndex = line[i];
                if (cellIndex < 0 || cellIndex >= cells.Length) continue;
                // If you want per-cell highlight, you can place an overlay per cell instead.
                if (highlightOverlays[i])
                {
                    highlightOverlays[i].gameObject.SetActive(true);
                    // Move overlay to the cell position:
                    var t = highlightOverlays[i].rectTransform;
                    t.position = cells[cellIndex].transform.position;
                }
            }
        }

        public void ClearHighlights()
        {
            if (highlightOverlays == null) return;
            foreach (var img in highlightOverlays)
                if (img) img.gameObject.SetActive(false);
        }
    }
}
