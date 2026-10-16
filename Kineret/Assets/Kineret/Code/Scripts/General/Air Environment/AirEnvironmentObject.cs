using System.Collections;
using UnityEngine;

public class AirEnvironmentObject : MonoBehaviour
{
    [Header("Model Direction")]
    [SerializeField, Range(-180f, 180f)] private float modelYawOffset;

    private float _movementSpeed;
    private float _lifeTime;
    private Vector3 _movementDirection;

    public void Initialize(float movementSpeed, float lifeTime, float fadeDuration, Vector2 movementDirection)
    {
        _movementSpeed = movementSpeed;
        _lifeTime = Mathf.Max(0f, lifeTime);

        _movementDirection = new Vector3(movementDirection.x, 0f, movementDirection.y).normalized;

        if (_movementDirection.sqrMagnitude > 0.001f)
        {
            Quaternion movementRotation = Quaternion.LookRotation(_movementDirection, Vector3.up);
            Quaternion modelCorrection = Quaternion.Euler(0f, modelYawOffset, 0f);
            transform.rotation = movementRotation * modelCorrection;
        }

        StartCoroutine(LifeTimeCoroutine());
    }

    private void Update()
    {
        transform.Translate(_movementDirection * _movementSpeed * Time.deltaTime, Space.World);
    }

    private IEnumerator LifeTimeCoroutine()
    {
        yield return new WaitForSeconds(_lifeTime);
        Destroy(gameObject);
    }
}