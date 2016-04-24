using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CardGame.Models
{
    public class Board
    {
        private List<Card> pieces = new List<Card>();
        public List<Card> Pieces
        {
            get { return pieces; }
            set { pieces = value; }
        }

        public Board()
        {
            int imgIndex = 1;
            for (int i = 1; i <= 30; i++)
            {
                if (isOdd(i))
                {
                    pieces.Add(
                        new Card()
                        {
                            Id = i,
                            Pair = i + 1,
                            Name = "Card-" + i,
                            Image = string.Format($"/Content/img/{imgIndex}.png")
                        });
                }
                else
                {
                    pieces.Add(
                        new Card()
                        {
                            Id = i,
                            Pair = i - 1,
                            Name = "Card-" + i,
                            Image = string.Format($"/Content/img/{imgIndex}.png")
                        });
                    imgIndex++;
                }
            }
            pieces.Shuffle();
        }

        private bool isOdd(int i)
        {
            return i % 2 == 0;
        }
    }
}