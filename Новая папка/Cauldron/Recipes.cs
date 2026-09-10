using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Recipes", menuName = "Alchemy/Recipes")]
public class Recipes : ScriptableObject
{
    public string nameRecipe;
    public List<Ingridient> ingridients;
    public GameObject result;
}
