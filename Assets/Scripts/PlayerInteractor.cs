using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera viewCamera;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField, Min(0.5f)] private float interactDistance = 2.5f;

    private PickupItem targetedItem;
    private GUIStyle promptStyle;

    private void Awake()
    {
        if (viewCamera == null)
        {
            viewCamera = GetComponent<Camera>();
        }

        if (inventory == null)
        {
            inventory = GetComponentInParent<PlayerInventory>();
        }
    }

    private void Update()
    {
        if (PlayerInventory.BagIsOpen)
        {
            targetedItem = null;
            return;
        }

        targetedItem = FindPickupInView();

        if (targetedItem != null && Input.GetKeyDown(KeyCode.E))
        {
            targetedItem.Collect(inventory);
            targetedItem = null;
        }
    }

    private PickupItem FindPickupInView()
    {
        if (viewCamera == null)
        {
            return null;
        }

        Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<PickupItem>();
        }

        return null;
    }

    private void OnGUI()
    {
        if (targetedItem == null)
        {
            return;
        }

        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = Color.white }
            };
        }

        const float promptWidth = 310f;
        Rect prompt = new Rect((Screen.width - promptWidth) * 0.5f, Screen.height * 0.68f, promptWidth, 42f);
        GUI.Box(prompt, "[E]  Pick up " + targetedItem.ItemName, promptStyle);
    }
}
