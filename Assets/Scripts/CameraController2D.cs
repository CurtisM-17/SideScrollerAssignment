using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraController2D : MonoBehaviour
{
    Vector2 viewportHalfSize;
    float leftBoundLimit, rightBoundLimit, bottomBoundLimit;

    Camera cam;
    Tilemap map;
    Transform plr;

    public Vector2 offset;
    public float smoothing = 5f;
    public Vector3 shakeOffset;
    public float shakeIntensity, shakeDuration;

    private void Start()
    {
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        map = GameObject.FindGameObjectWithTag("Ground").GetComponent<Tilemap>();
        plr = GameObject.FindGameObjectWithTag("Player").transform;

        map.CompressBounds();
        CalculateCamBounds();
    }

    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.T)) Shake(shakeIntensity, shakeDuration);

        Vector3 smoothedPos = Vector3.Lerp(
            transform.position,
            plr.position + new Vector3(offset.x, offset.y, transform.position.z) + shakeOffset, // Desired position
            1 - Mathf.Exp(-smoothing * Time.deltaTime)
        );

        smoothedPos.x = Mathf.Clamp(smoothedPos.x, leftBoundLimit, rightBoundLimit);
        smoothedPos.y = Mathf.Clamp(smoothedPos.y, bottomBoundLimit, smoothedPos.y);

        transform.position = smoothedPos;
    }

    void CalculateCamBounds()
    {
        viewportHalfSize = new(cam.orthographicSize * cam.aspect, cam.orthographicSize);

        leftBoundLimit = map.transform.position.x + (map.cellBounds.min.x * 0.75f) + viewportHalfSize.x;
        rightBoundLimit = map.transform.position.x + (map.cellBounds.max.x * 0.75f) - viewportHalfSize.x;
        bottomBoundLimit = map.transform.position.y + (map.cellBounds.min.y * 0.75f) + viewportHalfSize.y;
    }

    public void Shake(float intensity, float duration)
    {
        StartCoroutine(ShakeScreen(intensity, duration));
    }

    IEnumerator ShakeScreen(float intensity, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            shakeOffset = Random.insideUnitCircle * intensity;
            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeOffset = Vector3.zero;
    }
}
