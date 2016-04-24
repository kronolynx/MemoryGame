using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CardGame.Models;
using Microsoft.AspNet.SignalR;

namespace CardGame
{
    public class GameHub : Hub
    {
        public bool Join(string userName)
        {
            // get player
            var player = GameState.Instance.GetPlayer(userName);
            // if the player exist
            if(player != null)
            {
                // notify the client that the player is an existing player
                Clients.Caller.playerExists();
                return true;
            }

            // if player doesn't exist we create a player
            player = GameState.Instance.CreatePlayer(userName);
            player.ConnectionId = Context.ConnectionId;
            Clients.Caller.name = player.Name;
            Clients.Caller.hash = player.Hash;
            Clients.Caller.id = player.Id;

            Clients.Caller.playerJoined(player);

            return StartGame(player);
        }

        private bool StartGame(Player player)
        {
            if(player != null)
            {
                // check if a game already exist for this player
                Player player2;
                var game = GameState.Instance.FindGame(player, out player2);
                if(game != null)
                {
                    // notify both clients to rebuild their boards 
                    Clients.Group(player.Group).buildBoard(game);
                }
                // search for opponent
                player2 = GameState.Instance.GetNewOpponent(player);
                if(player2 == null)
                {
                    // notify client that there's no opponents available
                    Clients.Caller.waitingList();
                    return true;
                }
                // create new game
                game = GameState.Instance.CreateGame(player, player2);
                game.WhosTurn = player.Id;

                // notify both players that is time to build their game boards
                Clients.Group(player.Group).buildBoard(game);
                return true;
            }
            return false;
        }

        public bool FLip(string cardName)
        {
            // get username of the person who flipped the card
            var userName = Clients.Caller.name;
            var player = GameState.Instance.GetPlayer(userName);
            if(player != null)
            {
                Player playerOpponent;
                var game = GameState.Instance.FindGame(player, out playerOpponent);

                if(game != null)
                {
                    // check that the right person is flipping the card if is not its turn, return
                    if(!string.IsNullOrEmpty(game.WhosTurn) && game.WhosTurn != player.Id)
                    {
                        return true;
                    }

                    var card = FindCard(game, cardName);
                    // notify both players that the card must be flipped
                    Clients.Group(player.group).flipCard(card);
                    return true;
                }
            }
            return false;
        }

        private Card FindCard(Game game, string cardName)
        {
            return game.Board.Pieces.FirstOrDefault(c => c.Name == cardName);
        }

        /// <summary>
        ///  Method to determine which card is being flipped and
        ///  check if both cards are matching pair
        /// </summary>
        /// <param name="cardName"></param>
        /// <returns></returns>
        public bool CheckCard(string cardName)
        {
            // determine usersname
            var userName = Clients.Caller.name;
            // find if the player exist within this game
            Player player = GameState.Instance.GetPlayer(userName);
            if(player != null)
            {
                // get the opponent
                Player playerOpponent;
                Game game = GameState.Instance.FindGame(player, out playerOpponent);
                if(game != null)
                {
                    // check that the right player is flipping the card
                    if (!string.IsNullOrEmpty(game.WhosTurn) && game.WhosTurn != player.Id)
                        return true;
                    // get the card
                    Card card = FindCard(game, cardName);
                    // determine if is the first card flipped
                    if(game.LastCard == null)
                    {
                        // player turn
                        game.WhosTurn = player.Id;
                        game.LastCard = card;
                        return true;
                    }

                    // second flip check if card is a match
                    bool isMatch = IsMatch(game, card);
                    if (isMatch)
                    {
                        // store the player who found the card and the card
                        StoreMatch(player, card);
                        // reset last card
                        game.LastCard = null;
                        // notify users that a match was found
                        Clients.Group(player.Group).showmatch(card, userName);
                        // determine if is the end of the game
                        if(player.Matches.Count >= 16)
                        {
                            Clients.Group(player.Group).winner(card, userName);
                            // reset game
                            GameState.Instance.ResetGame(game);
                            return true;
                        }
                        return true;
                    }
                    // if player didn't find a match change turn
                    Player opponent = GameState.Instance.GetOpponent(player, game);
                    game.WhosTurn = opponent.Id;

                    // notify players that we didn't find a match flip cards back
                    Clients.Group(player.Group).resetFlip(game.LastCard, card);
                    // reset last card
                    game.LastCard = null;
                    return true;
                }
            }
            return false;
        }

        private void StoreMatch(Player player, Card card)
        {
            // store the card
            player.Matches.Add(card.Id);
            // store the ID of the matching card
            player.Matches.Add(card.Pair);
        }

        private bool IsMatch(Game game, Card card)
        {
            if (card == null)
                return false;
            if(game.LastCard != null)
            {
                // check if cards have same ID (match)
                if(game.LastCard.Pair == card.Id)
                {
                    return true;
                }
                // they are not match
                return false;
            }
            return false;
        }
    }
}