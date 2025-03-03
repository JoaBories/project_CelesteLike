using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashOrb: MonoBehaviour
{
    [SerializeField] private float cooldown = 10f;

    private float lastUsed;
    public bool active;

    [SerializeField] private SpriteRenderer spriteRenderer;

    private void FixedUpdate()
    {
        lastUsed -= Time.fixedDeltaTime;

        if (lastUsed <= 0 && !active)
        {
            Reactive();
        }
    }

    public void Reactive()
    {
        active = true;
        spriteRenderer.enabled = true;
    }

    public void Use()
    {
        lastUsed = cooldown;
        active = false;
        spriteRenderer.enabled = false;
    }
}
