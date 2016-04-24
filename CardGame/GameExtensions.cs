using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CardGame
{
    public static class GameExtensions
    {
        static readonly Random Random = new Random();
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while(n-- > 1)
            {
                var k = Random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}