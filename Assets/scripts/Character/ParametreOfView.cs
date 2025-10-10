using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   // ⬅️ UI
                       // Make sure these objects live under a Canvas

[DisallowMultipleComponent]
public class ParametreOfView : MonoBehaviour
{
    [System.Serializable]
    public class Item
    {
        public Sprite  _sprite;
        public Color   ParametredColor = Color.white;

        // For UI: use x,y as anchoredPosition (z is ignored for UI layout)
        public Vector3 ParametredPos   = Vector3.zero;   // anchoredPosition (x,y)
        public Vector3 ParametredScale = Vector3.one;    // localScale

        /// <summary>
        /// UI draw order override as a sibling index.
        /// Leave null to use default from part type.
        /// </summary>
        public int? orderLayer = null;
    }

    // Draw order map (lower = behind, higher = on top)
    int orderInLayer(string type)
    {
        switch (type)
        {
            case "body":    return 0;
            case "eyes":    return 2;
            case "hair":    return 3;
            case "acce":    return 4;
            case "glasses": return 5;
            default:        return 0;
        }
    }

    public void set_View(string type, Item it)
    {
        if (it == null)
        {
            Debug.LogWarning($"{name}.set_View: Item is null");
            return;
        }

        string key = (type ?? string.Empty).Trim().ToLowerInvariant();

        if (key != "body" && key != "head")
        {
            if (key == "hair" || key == "eyes" || key == "acce" || key == "glasses")
                key = "head/" + key;
            else if (key == "outfit" || key == "aura" || key == "wings")
                key = "body/" + key;
        }

        Transform partT = transform.Find(key);
        if (partT == null)
        {
            Debug.LogWarning($"{name}.set_View: Could not find child path '{key}' under '{transform.name}'");
            return;
        }

        var rt  = partT as RectTransform ?? partT.GetComponent<RectTransform>();
        var img = partT.GetComponent<Image>();
        if (rt == null)
        {
            Debug.LogWarning($"{name}.set_View: '{partT.name}' has no RectTransform (UI requires it)");
            return;
        }
        if (img == null)
        {
            Debug.LogWarning($"{name}.set_View: '{partT.name}' has no UI Image component");
            return;
        }

        // Apply visuals
        img.sprite = it._sprite;
        img.color  = it.ParametredColor;

        rt.anchoredPosition = new Vector2(it.ParametredPos.x, it.ParametredPos.y);
        rt.localScale       = it.ParametredScale;

        // ✅ Layering: sibling index inside the same Canvas hierarchy
        int desiredIndex = it.orderLayer.HasValue
            ? it.orderLayer.Value
            : orderInLayer(partT.name.ToLowerInvariant());


    }
}
