using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ventEnergy.Extensions
{
    /// <summary>
    /// Предоставляет статический класс, для расширенной работы с коллекциями
    /// </summary>
    public static class CollectionExtension
    {
        /// <summary>
        /// Делит исходный список на несколько составных по заданному размеру и собирает из них нвоый список
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="initialList">Разделяемый список</param>
        /// <param name="chunkLenght">Длина составных частей</param>
        /// <returns>Список, содержащий в себе части разбитого начального списка</returns>
        public static List<List<T>> DivideByLenght<T>(this List<T> initialList, int chunkLenght)
        {
            int valuesLenght = initialList.Count();
            int chunks = (int)Math.Ceiling(valuesLenght / (double)chunkLenght);
            var dividedList = Enumerable.Range(0, chunks).Select(i => initialList.Skip(i * chunkLenght).Take(chunkLenght).ToList()).ToList();
            return dividedList;
        }

        /// <summary>
        /// Делит исходный список на несколько составных на определенное колличество частей 
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="initialList">Разделяемый список</param>
        /// <param name="chunks">Колличество частей в новом списке</param>
        /// <returns>Список, содержащий в себе части разбитого начального списка</returns>
        public static List<List<T>> DivideByChunks<T>(this List<T> initialList, int chunks)
        {
            int valuesLenght = initialList.Count();
            int chunkLenght = (int)Math.Ceiling(valuesLenght / (double)chunks);
            var dividedList = Enumerable.Range(0, chunks).Select(i => initialList.Skip(i * chunkLenght).Take(chunkLenght).ToList()).ToList();
            return dividedList;
        }
    }
}
