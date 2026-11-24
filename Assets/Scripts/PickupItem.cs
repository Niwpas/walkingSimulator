using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [SerializeField] private string itemName = "Flashlight";
    [SerializeField, Min(1)] private int amount = 1;

    public string ItemName => itemName;

    public void Collect(PlayerInventory inventory)
    {
        if (inventory == null || string.IsNullOrWhiteSpace(itemName) || amount <= 0)
        {
            return;
        }

        inventory.AddItem(itemName, amount);
        gameObject.SetActive(false);
    }
}
