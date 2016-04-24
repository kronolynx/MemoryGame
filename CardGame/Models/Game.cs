using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CardGame.Models
{
    public class Game
    {
        public Player Player1 { get; set; }
        public Player Player2 { get; set; }

        public Board Board { get; set; }

        public Card LastCard { get; set; }
        public string WhosTurn { get; set; }
    }
}