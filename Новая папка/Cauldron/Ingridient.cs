using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Ingridient", menuName = "Alchemy/Ingridient")]
public class Ingridient : ScriptableObject
{
    public string nameIngridient;
    public GameObject ingredientPrefab;
    public float cookingTime;
    public float overcookingTime;
}
