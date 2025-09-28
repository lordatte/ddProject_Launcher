using UnityEngine;

[CreateAssetMenu(fileName = "New Upgrade Stone", menuName = "Inventory/Upgrade Stone")]
public class UpgradesStones : Item
{
    [Header("Upgrade Stone Properties")]
    [SerializeField][Range(1, 5)] private int _stoneDegree = 1;

    public int StoneDegree => _stoneDegree;

    public override void Use()
    {
        base.Use();
        Debug.Log($"Using Upgrade Stone (Degree {_stoneDegree})");
    }

    public override string GetDescription()
    {
        return base.GetDescription() + $"\nDegree: {_stoneDegree}";
    }
}