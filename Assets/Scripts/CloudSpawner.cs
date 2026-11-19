using System.Collections.Generic;
using UnityEngine;

public class CloudSpawner : MonoBehaviour
{
    [Header("Cloud prefabs (each with a SpriteRenderer)")]
    [SerializeField] private GameObject[] cloudPrefabs;

    [Header("Amount")]
    [SerializeField] private int cloudCount = 6;

    [Header("Movement")]
    [Tooltip("1 = move right, -1 = move left")]
    [SerializeField] private int direction = 1;
    [SerializeField] private float minSpeed = 0.3f;
    [SerializeField] private float maxSpeed = 1.0f;

    [Header("Gentle up/down bobbing")]
    [SerializeField] private float bobAmount = 0.1f;
    [SerializeField] private float bobSpeed = 0.5f;

    [Header("Look")]
    [SerializeField] private float minScale = 0.6f;
    [SerializeField] private float maxScale = 1.2f;
    [Tooltip("Smaller/slower clouds get more transparent, which looks like depth")]
    [SerializeField] private bool fadeDistantClouds = true;

    [Header("Vertical area (0 = bottom of screen, 1 = top)")]
    [Range(0f, 1f)][SerializeField] private float minHeight = 0.55f;
    [Range(0f, 1f)][SerializeField] private float maxHeight = 0.95f;

    [Header("Off-screen spawning")]
    [Tooltip("Extra distance outside the screen edge where clouds appear and disappear (world units)")]
    [SerializeField] private float edgePadding = 2f;
    [Tooltip("If on, clouds don't appear on screen at start - they all fly in from outside")]
    [SerializeField] private bool startOffScreen = false;

    [Header("Rendering")]
    [SerializeField] private string sortingLayer = "Default";
    [SerializeField] private int sortingOrder = -100;

    private class Cloud
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public float speed;
        public float baseY;
        public float bobOffset;
    }

    private readonly List<Cloud> clouds = new List<Cloud>();
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;

        if (cloudPrefabs == null || cloudPrefabs.Length == 0)
        {
            Debug.LogWarning("CloudSpawner: no cloud prefabs assigned.");
            enabled = false;
            return;
        }

        for (int i = 0; i < cloudCount; i++)
        {
            Cloud cloud = CreateCloud();
            float x;
            if (startOffScreen)
            {
                // Line them up outside the start side, at different distances,
                // so they fly in one after another
                float width = RightEdge() - LeftEdge();
                float offset = Random.Range(0f, width);
                x = direction > 0 ? LeftEdge() - offset : RightEdge() + offset;
            }
            else
            {
                // Spread across the whole screen so it isn't empty at start
                x = Random.Range(LeftEdge(), RightEdge());
            }
            Setup(cloud, x);
        }
    }

    private void Update()
    {
        float left = LeftEdge();
        float right = RightEdge();

        foreach (Cloud c in clouds)
        {
            Vector3 pos = c.transform.position;
            pos.x += c.speed * direction * Time.deltaTime;
            pos.y = c.baseY + Mathf.Sin(Time.time * bobSpeed + c.bobOffset) * bobAmount;
            c.transform.position = pos;

            float halfWidth = c.renderer.bounds.extents.x;

            // Left the screen? Send it back to the start side with new random settings.
            if (direction > 0 && pos.x - halfWidth > right)
                Recycle(c, left);
            else if (direction < 0 && pos.x + halfWidth < left)
                Recycle(c, right);
        }
    }

    private Cloud CreateCloud()
    {
        GameObject prefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];
        GameObject go = Instantiate(prefab, transform);

        Cloud c = new Cloud
        {
            transform = go.transform,
            renderer = go.GetComponent<SpriteRenderer>()
        };
        c.renderer.sortingLayerName = sortingLayer;
        clouds.Add(c);
        return c;
    }

    private void Recycle(Cloud c, float edgeX)
    {
        // Swap the sprite so the repetition is less obvious
        GameObject prefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];
        SpriteRenderer src = prefab.GetComponent<SpriteRenderer>();
        if (src != null) c.renderer.sprite = src.sprite;

        Setup(c, edgeX);

        // Place it fully outside the screen on the start side
        float halfWidth = c.renderer.bounds.extents.x;
        Vector3 p = c.transform.position;
        p.x = direction > 0 ? edgeX - halfWidth : edgeX + halfWidth;
        c.transform.position = p;
    }

    private void Setup(Cloud c, float x)
    {
        float t = Random.value; // 0 = far, 1 = near
        float scale = Mathf.Lerp(minScale, maxScale, t);

        c.speed = Mathf.Lerp(minSpeed, maxSpeed, t);
        c.bobOffset = Random.Range(0f, Mathf.PI * 2f);
        c.transform.localScale = Vector3.one * scale;

        float bottom = cam.ViewportToWorldPoint(new Vector3(0, minHeight, 0)).y;
        float top = cam.ViewportToWorldPoint(new Vector3(0, maxHeight, 0)).y;
        c.baseY = Random.Range(bottom, top);

        // Closer clouds draw in front of farther ones
        c.renderer.sortingOrder = sortingOrder + Mathf.RoundToInt(t * 10);

        if (fadeDistantClouds)
        {
            Color col = c.renderer.color;
            col.a = Mathf.Lerp(0.6f, 1f, t);
            c.renderer.color = col;
        }

        c.transform.position = new Vector3(x, c.baseY, transform.position.z);
    }

    private float LeftEdge() => cam.ViewportToWorldPoint(new Vector3(0, 0, 0)).x - edgePadding;
    private float RightEdge() => cam.ViewportToWorldPoint(new Vector3(1, 0, 0)).x + edgePadding;
}