using UnityEngine;

namespace UI
{
    public class UIFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float followDistance = 2f;
        [SerializeField] private float heightOffset = 0f;
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float rotationSpeed = 5f;

        private void Update()
        {
            var targetPosition = cameraTransform.position + cameraTransform.forward * followDistance;
            targetPosition.y += heightOffset;

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);

            var targetRotation = Quaternion.LookRotation(transform.position - cameraTransform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
}