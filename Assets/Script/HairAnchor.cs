using UnityEngine;

public class HairAnchor : MonoBehaviour
{
    public static HairAnchor instance;

    public Vector2 partOffset = Vector2.zero;
    public int flipX = 1;

    [SerializeField] private float lerpSpeed = 10;

    private Transform[] hairParts;

    private void Awake()
    {
        instance = this;

        hairParts = GetComponentsInChildren<Transform>();
    }

    private void Update()
    {
        Transform pieceToFollow = transform;
        Vector2 goalPartOffset = partOffset * new Vector2(flipX, 1);

        Debug.Log(goalPartOffset);

        foreach (Transform part in hairParts)
        {
            if (!part.Equals(transform))
            {
                Vector2 targetPos = (Vector2) pieceToFollow.position + goalPartOffset;
                Vector2 lerpPos = Vector2.Lerp(part.position, targetPos, Time.deltaTime * lerpSpeed);

                part.position = lerpPos;
                pieceToFollow = part;
            }
        }
    }
}
