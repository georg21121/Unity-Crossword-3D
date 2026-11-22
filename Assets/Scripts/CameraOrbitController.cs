using UnityEngine;

namespace Crossword3D
{
    public class CameraOrbitController : MonoBehaviour
    {
        public static CameraOrbitController Instance { get; private set; }

        [Header("Target & Distances")]
        public Transform target;
        public Vector3 targetOffset = Vector3.zero;
        public Vector3 defaultTargetPos = Vector3.zero;
        public float defaultDistance = 21.0f;
        public float distance = 21.0f;
        public float minDistance = 6.0f;
        public float maxDistance = 35.0f;

        [Header("Rotation Limits")]
        public float xSpeed = 120.0f;
        public float ySpeed = 80.0f;
        public float yMinLimit = 15.0f;
        public float yMaxLimit = 85.0f;

        [Header("Zoom & Smoothing")]
        public float zoomRate = 40.0f;
        public float smoothTime = 0.12f;

        private float x = 0.0f;
        private float y = 50.0f;
        private float targetX;
        private float targetY;
        private float targetDistance;

        private float xVelocity = 0.0f;
        private float yVelocity = 0.0f;
        private float distanceVelocity = 0.0f;

        private bool isInteractingWithUI = false;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;
            x = angles.y;
            y = angles.x;

            targetX = x;
            targetY = y;
            targetDistance = distance;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Check if pointer is over UI
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                isInteractingWithUI = true;
            }
            else
            {
                isInteractingWithUI = false;
            }

            // Mouse orbit (Right click or Alt + Left click)
            if (Input.GetMouseButton(1) || (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0)))
            {
                targetX += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                targetY -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
                targetY = ClampAngle(targetY, yMinLimit, yMaxLimit);
            }

            // Mouse zoom (only when not interacting with UI)
            if (!isInteractingWithUI)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    targetDistance -= scroll * zoomRate;
                    targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
                }
            }

            // Touch controls for mobile / tablet cross-platform
            if (Input.touchCount == 1 && !isInteractingWithUI)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved)
                {
                    targetX += touch.deltaPosition.x * xSpeed * 0.005f;
                    targetY -= touch.deltaPosition.y * ySpeed * 0.005f;
                    targetY = ClampAngle(targetY, yMinLimit, yMaxLimit);
                }
            }
            else if (Input.touchCount == 2)
            {
                Touch touchZero = Input.GetTouch(0);
                Touch touchOne = Input.GetTouch(1);

                Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
                targetDistance += deltaMagnitudeDiff * 0.02f;
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            }

            // Smooth interpolation
            x = Mathf.SmoothDamp(x, targetX, ref xVelocity, smoothTime);
            y = Mathf.SmoothDamp(y, targetY, ref yVelocity, smoothTime);
            distance = Mathf.SmoothDamp(distance, targetDistance, ref distanceVelocity, smoothTime);

            Quaternion rotation = Quaternion.Euler(y, x, 0);
            Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.position + targetOffset;

            transform.rotation = rotation;
            transform.position = position;
        }

        public void ResetViewPerspective()
        {
            targetX = 0f;
            targetY = 54f;
            targetDistance = defaultDistance;

            if (target != null)
            {
                target.position = defaultTargetPos;
            }
        }

        public void ResetViewTopDown()
        {
            targetX = 0f;
            targetY = 85f;
            targetDistance = defaultDistance * 0.9f;

            if (target != null)
            {
                target.position = defaultTargetPos;
            }
        }

        public void FocusOnPosition(Vector3 worldPos)
        {
            if (target != null)
            {
                target.position = worldPos;
            }
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F) angle += 360F;
            if (angle > 360F) angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }
    }
}
