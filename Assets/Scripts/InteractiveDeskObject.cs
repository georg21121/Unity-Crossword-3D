using System.Collections;
using UnityEngine;

namespace Crossword3D
{
    public enum DeskObjectType
    {
        DeskLamp,
        MagnifyingGlass,
        VintageRadio,
        Notebook
    }

    public class InteractiveDeskObject : MonoBehaviour
    {
        [Header("Object Type")]
        public DeskObjectType objectType;

        [Header("Lamp References (if DeskLamp)")]
        public Light lampLight;
        public MeshRenderer lampBulbRenderer;
        public Color warmLightColor = new Color(1.0f, 0.88f, 0.65f);
        public Color coolLightColor = new Color(0.85f, 0.92f, 1.0f);
        private bool isLampOn = true;

        [Header("Animation")]
        private Vector3 originalPos;
        private Quaternion originalRot;
        private bool isInteracting = false;

        private void Start()
        {
            originalPos = transform.localPosition;
            originalRot = transform.localRotation;
        }

        private void OnMouseEnter()
        {
            if (!isInteracting)
            {
                transform.localScale = Vector3.one * 1.05f;
            }
        }

        private void OnMouseExit()
        {
            if (!isInteracting)
            {
                transform.localScale = Vector3.one;
            }
        }

        private void OnMouseDown()
        {
            Interact();
        }

        public void Interact()
        {
            if (isInteracting) return;
            StartCoroutine(InteractRoutine());
        }

        private IEnumerator InteractRoutine()
        {
            isInteracting = true;

            switch (objectType)
            {
                case DeskObjectType.DeskLamp:
                    yield return ToggleLamp();
                    break;

                case DeskObjectType.MagnifyingGlass:
                    yield return TriggerHintAnimation();
                    break;

                case DeskObjectType.VintageRadio:
                    yield return TriggerRadioAnimation();
                    break;

                case DeskObjectType.Notebook:
                    yield return TriggerNotebookAnimation();
                    break;
            }

            transform.localScale = Vector3.one;
            isInteracting = false;
        }

        private IEnumerator ToggleLamp()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayLampSwitch();

            isLampOn = !isLampOn;

            // Small tactile click rotation
            transform.localRotation = originalRot * Quaternion.Euler(0, 0, isLampOn ? 5f : -5f);
            yield return new WaitForSeconds(0.1f);
            transform.localRotation = originalRot;

            if (lampLight != null)
            {
                lampLight.enabled = isLampOn;
                lampLight.color = isLampOn ? warmLightColor : coolLightColor;
            }

            if (lampBulbRenderer != null && lampBulbRenderer.material != null)
            {
                lampBulbRenderer.material.color = isLampOn ? warmLightColor * 1.5f : Color.gray * 0.4f;
            }
        }

        private IEnumerator TriggerHintAnimation()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayHint();

            // Magnifying glass lifts up and inspects the board
            float elapsed = 0f;
            float duration = 0.6f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float yOffset = Mathf.Sin(progress * Mathf.PI) * 0.4f;
                transform.localPosition = originalPos + new Vector3(0, yOffset, 0);
                transform.localRotation = originalRot * Quaternion.Euler(0, progress * 45f, 0);
                yield return null;
            }

            transform.localPosition = originalPos;
            transform.localRotation = originalRot;

            // Trigger crossword hint action
            if (CrosswordManager.Instance != null)
            {
                CrosswordManager.Instance.UseHint();
            }
        }

        private IEnumerator TriggerRadioAnimation()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayClick();
                AudioManager.Instance.ToggleMute();
            }

            // Dial rotation effect
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                transform.localRotation = originalRot * Quaternion.Euler(0, Mathf.Sin(elapsed * 25f) * 8f, 0);
                yield return null;
            }
            transform.localRotation = originalRot;
        }

        private IEnumerator TriggerNotebookAnimation()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayClick();

            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                float y = Mathf.Sin(elapsed / 0.3f * Mathf.PI) * 0.15f;
                transform.localPosition = originalPos + new Vector3(0, y, 0);
                yield return null;
            }
            transform.localPosition = originalPos;
        }
    }
}
