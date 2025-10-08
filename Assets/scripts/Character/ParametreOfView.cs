using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ParametreOfView : MonoBehaviour
{
    [System.Serializable]
    public class Item
    {
        public Sprite _sprite;
        public Color ParametredColor = Color.white;
        public Vector3 ParametredPos = Vector3.zero;     // local position
        public Vector3 ParametredScale = Vector3.one;    // local scale
    }

    // Returns sorting order for each part (lower is behind)
    int orderInLayer(string type)
    {
        switch (type) 
        {
            case "body":    return 0;
            case "eyes":    return 2;
            case "hair":    return 3;
            case "acce":    return 4;
            case "glasses": return 5;
            default:        return 0;   // fallback
        }
    }

    public void set_View(string type, Item it)
    {
        if (it == null)
        {
            Debug.LogWarning($"{name}.set_View: Item is null"); 
            return;
        }

        // Normalize casing once
        string key = (type ?? "").Trim().ToLowerInvariant();

        if (key == "hair" || key == "eyes" || key == "acce" || key == "glasses")
            key = "head/" + key;

        Transform partT = transform.Find(key);
        if (partT == null)
        {
            Debug.LogWarning($"{name}.set_View: Could not find child path '{key}' under '{transform.name}'");
            return;
        }

        var sr = partT.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogWarning($"{name}.set_View: '{partT.name}' has no SpriteRenderer");
            return;
        }

        sr.sprite = it._sprite;
        sr.color  = it.ParametredColor;
        partT.localPosition = it.ParametredPos;
        partT.localScale    = it.ParametredScale;

        string last = partT.name.ToLowerInvariant();
        sr.sortingOrder = orderInLayer(last);
    }
}
