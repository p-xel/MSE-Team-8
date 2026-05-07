using Fusion;
using UnityEngine;

public class PlayerColor : NetworkBehaviour
{
    public MeshRenderer MeshRenderer;

    [Networked, OnChangedRender(nameof(ColorChanged))]
    public Color NetworkedColor { get; set; }

    private void ColorChanged()
    {
        MeshRenderer.material.color = NetworkedColor;
    }

    private void Update()
    {
        if (HasStateAuthority == false)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            // setting material.color directly would only update on the local client; assigning the [Networked] property replicates and triggers OnChangedRender on every peer
            NetworkedColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
        }
    }
}
