using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Ingridient", menuName = "Alchemy/Ingridient")]
public class Ingridient : ScriptableObject
{
    public string nameIngridient;
    public GameObject ingredientPrefab;

    public int tocisity;
    public int acidity;
    public int volatility;
    public int viscosity;
    public int thermoactivity;
    public int vitality;
    public int conductivity;
    public int psychoactivity;
    public int entropy;

    public List<int> attributes;

    public string i = "ПИДАРАС";
    public float cookingTime;
    public float overcookingTime;

    [Header("Shop Settings")]
    [Tooltip("Стоимость ингредиента в магазине")]
    public int price = 10;
    [Tooltip("Иконка ингредиента для отображения на страницах книги")]
    public Sprite icon;

    public void Awake()
    {
        attributes = new List<int> { tocisity, acidity, volatility, viscosity, thermoactivity, vitality, conductivity, psychoactivity, entropy };
    }
}
