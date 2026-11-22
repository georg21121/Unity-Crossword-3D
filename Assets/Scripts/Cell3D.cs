using System.Collections;
using UnityEngine;

namespace Crossword3D
{
    public class Cell3D : MonoBehaviour
    {
        [Header("Grid Position")]
        public int gridX;
        public int gridY;
        public char correctChar = ' ';
        public char currentChar = ' ';
        public bool isSolved = false;

        [Header("Visual Components")]
        public MeshRenderer tileRenderer;
        public TextMesh letterTextMesh;
        public TextMesh numberTextMesh;

        [Header("Colors")]
        public Color normalColor = new Color(0.95f, 0.94f, 0.90f);       // Soft warm ivory
        public Color hoverColor = new Color(1.0f, 0.98f, 0.85f);         // Warm glow
        public Color selectedColor = new Color(1.0f, 0.85f, 0.35f);      // Vibrant amber/gold
        public Color wordHighlightColor = new Color(0.80f, 0.90f, 1.0f); // Soft sky blue highlight
        public Color solvedColor = new Color(0.65f, 0.92f, 0.70f);        // Soft emerald green
        public Color errorColor = new Color(1.0f, 0.55f, 0.55f);         // Soft crimson red

        private Material tileMaterial;
        private Vector3 initialLocalPos;
        private Vector3 targetLocalPos;
        private Coroutine animCoroutine;

        public bool IsSelected { get; private set; }
        public bool IsHovered { get; private set; }
        public bool IsWordHighlighted { get; private set; }

        public void Initialize(int x, int y, char targetChar, int clueNumber = -1)
        {
            gridX = x;
            gridY = y;
            correctChar = char.ToUpperInvariant(targetChar);
            currentChar = ' ';
            isSolved = false;

            initialLocalPos = transform.localPosition;
            targetLocalPos = initialLocalPos;

            // Setup internal Material copy to change colors individually
            if (tileRenderer != null)
            {
                if (tileRenderer.sharedMaterial != null)
                    tileMaterial = new Material(tileRenderer.sharedMaterial);
                else
                    tileMaterial = new Material(Shader.Find("Standard"));
                tileRenderer.material = tileMaterial;
            }

            UpdateLetterDisplay();
            SetClueNumber(clueNumber);
            UpdateVisualState();
        }

        public void SetClueNumber(int num)
        {
            if (numberTextMesh != null)
            {
                numberTextMesh.text = num > 0 ? num.ToString() : "";
            }
        }

        public void SetChar(char c)
        {
            currentChar = char.ToUpperInvariant(c);
            UpdateLetterDisplay();
        }

        public void ClearChar()
        {
            if (!isSolved)
            {
                currentChar = ' ';
                UpdateLetterDisplay();
            }
        }

        public void MarkSolved()
        {
            isSolved = true;
            currentChar = correctChar;
            UpdateLetterDisplay();
            UpdateVisualState();
            AnimateBounce();
        }

        private void UpdateLetterDisplay()
        {
            if (letterTextMesh != null)
            {
                letterTextMesh.text = (currentChar != ' ') ? currentChar.ToString() : "";
            }
        }

        public void SetHovered(bool hovered)
        {
            IsHovered = hovered;
            UpdateVisualState();
            UpdateElevation();
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            UpdateVisualState();
            UpdateElevation();
        }

        public void SetWordHighlight(bool highlight)
        {
            IsWordHighlighted = highlight;
            UpdateVisualState();
        }

        private void UpdateElevation()
        {
            float elevation = 0f;
            if (IsSelected) elevation = 0.15f;
            else if (IsHovered) elevation = 0.08f;

            targetLocalPos = initialLocalPos + new Vector3(0f, elevation, 0f);

            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(AnimatePosition(targetLocalPos));
        }

        private void UpdateVisualState()
        {
            if (tileMaterial == null) return;

            Color targetColor = normalColor;
            if (isSolved)
            {
                targetColor = solvedColor;
                if (IsSelected) targetColor = Color.Lerp(solvedColor, selectedColor, 0.5f);
                else if (IsWordHighlighted) targetColor = Color.Lerp(solvedColor, wordHighlightColor, 0.35f);
            }
            else if (IsSelected)
            {
                targetColor = selectedColor;
            }
            else if (IsHovered)
            {
                targetColor = hoverColor;
            }
            else if (IsWordHighlighted)
            {
                targetColor = wordHighlightColor;
            }

            tileMaterial.color = targetColor;
        }

        public void FlashError()
        {
            StartCoroutine(FlashColorRoutine(errorColor, 0.4f));
        }

        private IEnumerator FlashColorRoutine(Color flashColor, float duration)
        {
            if (tileMaterial == null) yield break;
            Color original = tileMaterial.color;
            tileMaterial.color = flashColor;
            yield return new WaitForSeconds(duration);
            UpdateVisualState();
        }

        public void AnimateBounce()
        {
            StartCoroutine(BounceRoutine());
        }

        private IEnumerator BounceRoutine()
        {
            float elapsed = 0f;
            float duration = 0.35f;
            Vector3 startPos = transform.localPosition;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float yOffset = Mathf.Sin(progress * Mathf.PI) * 0.25f;
                transform.localPosition = startPos + new Vector3(0f, yOffset, 0f);
                yield return null;
            }
            transform.localPosition = targetLocalPos;
        }

        private IEnumerator AnimatePosition(Vector3 target)
        {
            while (Vector3.Distance(transform.localPosition, target) > 0.002f)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, target, Time.deltaTime * 18f);
                yield return null;
            }
            transform.localPosition = target;
        }

        private void OnMouseEnter()
        {
            SetHovered(true);
        }

        private void OnMouseExit()
        {
            SetHovered(false);
        }

        private void OnMouseDown()
        {
            if (CrosswordManager.Instance != null)
            {
                CrosswordManager.Instance.OnCellClicked(gridX, gridY);
            }
        }
    }
}
