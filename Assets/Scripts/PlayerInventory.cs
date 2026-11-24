using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private readonly Dictionary<string, int> items = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    private string lastPickedUpItem;
    private float lastPickupTime;
    private GUIStyle packTitleStyle;
    private GUIStyle packItemStyle;
    private GUIStyle bagTitleStyle;
    private GUIStyle bagSubtitleStyle;
    private GUIStyle bagItemStyle;
    private GUIStyle hintStyle;
    private Texture2D hudTexture;
    private Texture2D overlayTexture;
    private Texture2D panelTexture;
    private Texture2D headerTexture;
    private Texture2D slotTexture;
    private Texture2D accentTexture;

    public static bool BagIsOpen { get; private set; }

    private void Awake()
    {
        BagIsOpen = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            SetBagOpen(!BagIsOpen);
        }
        else if (BagIsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            SetBagOpen(false);
        }
    }

    private void OnDisable()
    {
        if (BagIsOpen)
        {
            SetBagOpen(false);
        }
    }

    public void AddItem(string itemName, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemName) || amount <= 0)
        {
            return;
        }

        items.TryGetValue(itemName, out int currentAmount);
        items[itemName] = currentAmount + amount;
        lastPickedUpItem = itemName;
        lastPickupTime = Time.time;
    }

    private static void SetBagOpen(bool open)
    {
        BagIsOpen = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    private void OnGUI()
    {
        EnsureStyles();
        DrawPackHud();

        if (BagIsOpen)
        {
            DrawBag();
        }

        if (!string.IsNullOrEmpty(lastPickedUpItem) && Time.time - lastPickupTime < 2.5f)
        {
            const float toastWidth = 300f;
            Rect toast = new Rect((Screen.width - toastWidth) * 0.5f, Screen.height - 76f, toastWidth, 42f);
            GUI.DrawTexture(toast, headerTexture);
            GUI.Label(toast, "Added to pack  ·  " + lastPickedUpItem, bagItemStyle);
        }
    }

    private void DrawPackHud()
    {
        float panelHeight = 58f + items.Count * 23f;
        Rect panel = new Rect(18f, 18f, 225f, panelHeight);
        GUI.DrawTexture(panel, hudTexture);
        GUI.DrawTexture(new Rect(panel.x, panel.y, 4f, panel.height), accentTexture);
        GUI.Label(new Rect(panel.x + 14f, panel.y + 8f, 200f, 22f), "FIELD PACK  [B]", packTitleStyle);

        if (items.Count == 0)
        {
            GUI.Label(new Rect(panel.x + 14f, panel.y + 31f, 200f, 20f), "Nothing collected yet", packItemStyle);
            return;
        }

        int row = 0;
        foreach (KeyValuePair<string, int> item in items)
        {
            Rect rowRect = new Rect(panel.x + 14f, panel.y + 31f + row * 23f, 200f, 21f);
            GUI.Label(rowRect, item.Key + "  ×" + item.Value, packItemStyle);
            row++;
        }
    }

    private void DrawBag()
    {
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), overlayTexture);

        float panelWidth = Mathf.Min(540f, Screen.width - 32f);
        float panelHeight = Mathf.Min(430f, Screen.height - 32f);
        Rect panel = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);
        GUI.DrawTexture(panel, panelTexture);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 7f), accentTexture);
        GUI.DrawTexture(new Rect(panel.x + 22f, panel.y + 92f, panel.width - 44f, 1f), headerTexture);

        GUI.Label(new Rect(panel.x + 28f, panel.y + 24f, panel.width - 56f, 38f), "FIELD PACK", bagTitleStyle);
        GUI.Label(new Rect(panel.x + 30f, panel.y + 64f, panel.width - 60f, 22f), "SUPPLIES FOR THE NIGHT", bagSubtitleStyle);

        if (items.Count == 0)
        {
            Rect emptySlot = new Rect(panel.x + 24f, panel.y + 112f, panel.width - 48f, 76f);
            GUI.DrawTexture(emptySlot, slotTexture);
            GUI.Label(new Rect(emptySlot.x + 18f, emptySlot.y + 12f, emptySlot.width - 36f, 48f), "Your pack is empty. Search the forest and abandoned buildings for supplies.", packItemStyle);
        }
        else
        {
            int row = 0;
            foreach (KeyValuePair<string, int> item in items)
            {
                Rect itemSlot = new Rect(panel.x + 24f, panel.y + 112f + row * 68f, panel.width - 48f, 58f);
                GUI.DrawTexture(itemSlot, slotTexture);
                GUI.DrawTexture(new Rect(itemSlot.x + 12f, itemSlot.y + 12f, 34f, 34f), accentTexture);
                GUI.Label(new Rect(itemSlot.x + 60f, itemSlot.y + 9f, itemSlot.width - 150f, 40f), item.Key.ToUpperInvariant(), bagItemStyle);
                GUI.Label(new Rect(itemSlot.xMax - 82f, itemSlot.y + 9f, 64f, 40f), "×" + item.Value, bagItemStyle);
                row++;
            }
        }

        GUI.Label(new Rect(panel.x + 26f, panel.yMax - 48f, panel.width - 52f, 24f), "B or ESC  ·  CLOSE PACK", hintStyle);
    }

    private void EnsureStyles()
    {
        if (packTitleStyle != null)
        {
            return;
        }

        hudTexture = MakeTexture(new Color(0.035f, 0.055f, 0.048f, 0.9f));
        overlayTexture = MakeTexture(new Color(0.008f, 0.014f, 0.012f, 0.78f));
        panelTexture = MakeTexture(new Color(0.055f, 0.075f, 0.064f, 0.98f));
        headerTexture = MakeTexture(new Color(0.09f, 0.12f, 0.095f, 1f));
        slotTexture = MakeTexture(new Color(0.075f, 0.095f, 0.08f, 1f));
        accentTexture = MakeTexture(new Color(0.47f, 0.53f, 0.32f, 1f));

        packTitleStyle = MakeStyle(14, FontStyle.Bold, new Color(0.91f, 0.93f, 0.86f), TextAnchor.MiddleLeft);
        packItemStyle = MakeStyle(14, FontStyle.Normal, new Color(0.82f, 0.86f, 0.78f), TextAnchor.MiddleLeft);
        bagTitleStyle = MakeStyle(26, FontStyle.Bold, new Color(0.93f, 0.94f, 0.87f), TextAnchor.MiddleLeft);
        bagSubtitleStyle = MakeStyle(12, FontStyle.Bold, new Color(0.63f, 0.69f, 0.57f), TextAnchor.MiddleLeft);
        bagItemStyle = MakeStyle(16, FontStyle.Bold, new Color(0.91f, 0.93f, 0.86f), TextAnchor.MiddleLeft);
        hintStyle = MakeStyle(12, FontStyle.Bold, new Color(0.63f, 0.69f, 0.57f), TextAnchor.MiddleCenter);
    }

    private static GUIStyle MakeStyle(int fontSize, FontStyle fontStyle, Color textColor, TextAnchor alignment)
    {
        return new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = fontStyle,
            alignment = alignment,
            normal = { textColor = textColor }
        };
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void OnDestroy()
    {
        DestroyTexture(hudTexture);
        DestroyTexture(overlayTexture);
        DestroyTexture(panelTexture);
        DestroyTexture(headerTexture);
        DestroyTexture(slotTexture);
        DestroyTexture(accentTexture);
    }

    private static void DestroyTexture(Texture2D texture)
    {
        if (texture != null)
        {
            Destroy(texture);
        }
    }
}
