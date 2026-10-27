using System;
using UnityEngine;

namespace MageTower.Shop
{
    /// <summary>
    /// Элемент оформленного или выбранного заказа.
    /// Хранит ссылку на ScriptableObject ингредиента и выбранное количество.
    /// </summary>
    [Serializable]
    public class OrderItem
    {
        [Tooltip("Ссылка на ScriptableObject ингредиента")]
        public Ingridient ingredient;

        [Tooltip("Количество заказанных единиц")]
        public int count;

        public OrderItem() { }

        public OrderItem(Ingridient ingredient, int count)
        {
            this.ingredient = ingredient;
            this.count = count;
        }

        /// <summary>
        /// Суммарная стоимость данной позиции
        /// </summary>
        public int TotalPrice => (ingredient != null) ? ingredient.price * count : 0;
    }
}
