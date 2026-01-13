using UnityEngine;

public class HoverDebugger : MonoBehaviour
{
    public Camera cam;                // assign your main camera
    public LayerMask placeableMask;   // set to the "Placeable" layer

    void Update()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, placeableMask))
        {
            var placeable = hit.collider.GetComponentInParent<PlaceableObject>();
            if (placeable)
            {
                Debug.Log("Hovering over: " + placeable.name);
            }
        }
    }
}