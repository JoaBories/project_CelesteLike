using UnityEngine;

public class HairAnchor : MonoBehaviour
{
    public static HairAnchor instance;

    public float partOffsetX;
    public float partOffsetY;

    public float lerpSpeed = 10;

    public Color hairColor = Color.white;

    [SerializeField] private float scalePow = 1;

    private Transform[] hairParts;

    private void Awake()
    {
        instance = this;

        hairParts = GetComponentsInChildren<Transform>();
    }

    private void Update()
    {
        Transform pieceToFollow = transform;
        Vector2 goalPartOffset = new(partOffsetX, partOffsetY);

        foreach (Transform part in hairParts)
        {
            if (!part.Equals(transform))
            {
                Vector2 targetPos = (Vector2) pieceToFollow.position + goalPartOffset * Mathf.Pow(part.localScale.x, scalePow);
                Vector2 lerpPos = Vector2.Lerp(part.position, targetPos, Time.deltaTime * lerpSpeed);

                part.position = lerpPos;
                pieceToFollow = part;
            }
        }

        if (hairColor != GetComponent<SpriteRenderer>().color)
        {
            GetComponent<SpriteRenderer>().color = hairColor;
            foreach (Transform part in hairParts)
            {
                if (!part.Equals(transform))
                {
                    part.GetComponent<SpriteRenderer>().color = hairColor;
                }
            }
        }
    }
}
