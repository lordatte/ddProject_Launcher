using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Items/Weapon")]
public class Weapon : Item
{
    [Header("Weapon Display")]
    [SerializeField] private string displayName;
    [SerializeField] private Sprite equippedImage;
    [SerializeField] private Sprite useImage;

    [Header("Weapon Stats")]
    [SerializeField] private int damage;
    [SerializeField] private int defense;
    [SerializeField] private int magicDamage;
    [SerializeField] private int magicDefense;
    [SerializeField] private int agility;
    [SerializeField] private int aim;

    // Public methods for access
    public string GetDisplayName() => displayName;
    public Sprite GetEquippedImage() => equippedImage;
    public Sprite GetUseImage() => useImage;
    public int GetDamage() => damage;
    public int GetDefense() => defense;
    public int GetMagicDamage() => magicDamage;
    public int GetMagicDefense() => magicDefense;
    public int GetAgility() => agility;
    public int GetAim() => aim;


}
