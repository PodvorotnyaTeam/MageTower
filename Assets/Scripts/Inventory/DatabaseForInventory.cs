using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DatabaseForInventory", menuName = "Game/DatabaseForInventory")]
public class DatabaseForInventory : ScriptableObject
{
    public List<Item> items = new List<Item>();
}

[System.Serializable]
public class Item
{
    public int id;
    public string name;
    public Sprite img;
}