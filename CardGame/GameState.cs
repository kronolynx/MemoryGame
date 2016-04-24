using CardGame.Models;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace CardGame
{
    public class GameState
    {
        private static readonly Lazy<GameState> instance = new Lazy<GameState>(()=> new GameState(GlobalHost.ConnectionManager.GetHubContext<GameHub>()));

        private readonly ConcurrentDictionary<string, Player> players = new ConcurrentDictionary<string, Player>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, Game> games = new ConcurrentDictionary<string, Game>(StringComparer.OrdinalIgnoreCase);

        public GameState(IHubContext context)
        {
            Clients = context.Clients;
            Groups = context.Groups;
        }

        public static GameState Instance
        {
            get { return instance.Value; }
        }

        public IHubConnectionContext<dynamic> Clients { get; set; }
        public IGroupManager Groups { get; set; }

        public Player CreatePlayer(string userName)
        {
            var player = new Player(userName, GetMD5Hash(userName));
            players[userName] = player;
            return player;
        }

        private string GetMD5Hash(string userName)
        {
            return String.Join("", MD5.Create()
                                      .ComputeHash(System.Text.Encoding.Default.GetBytes(userName))
                                      .Select(b => b.ToString("x2")));
        }

        public Player GetPlayer(string userName)
        {
            return players.Values.FirstOrDefault(u => u.Name == userName);
        }

        public Player GetNewOpponent(Player player)
        {
            return players.Values.FirstOrDefault(u => !u.IsPlaying && u.Id != player.Id);
        }

        public Player GetOpponent(Player player, Game game)
        {
            if (game.Player1.Id == player.Id)
                return game.Player2;
            return game.Player1;
        }

        public Game CreateGame(Player player1, Player player2)
        {
            var game = new Game()
            {
                Player1 = player1,
                Player2 = player2,
                Board = new Board()
            };

            var group = Guid.NewGuid().ToString("d");
            games[group] = game;

            player1.IsPlaying = true;
            player1.Group = group;

            player2.IsPlaying = true;
            player2.Group = group;

            // Add players to group, if group doesn't exist Signalr enginee creates automatically one
            Groups.Add(player1.ConnectionId, group);
            Groups.Add(player2.ConnectionId, group);

            return game;
        }

        public Game FindGame(Player player, out Player opponent)
        {
            opponent = null;
            if (player.Group == null)
                return null;

            Game game;
            games.TryGetValue(player.Group, out game);

            if(game != null)
            {
                if(player.Id == game.Player1.Id)
                {
                    opponent = game.Player2;
                    return game;
                }
                opponent = game.Player1;
                return game;
            }
            return null;
        } 

        public void ResetGame(Game game)
        {
            // get the group and names of the players
            var groupName = game.Player1.Group;
            var player1Name = game.Player1.Name;
            var player2Name = game.Player2.Name;

            // remove the players from groups
            Groups.Remove(game.Player1.ConnectionId, groupName);
            Groups.Remove(game.Player2.ConnectionId, groupName);

            //remove the players from game players
            Player p1;  // try remove returns a single player
            players.TryRemove(player1Name, out p1);

            Player p2;
            players.TryRemove(player2Name, out p2);

            // now we can remove the game form the games collection
            Game g;
            games.TryRemove(groupName, out g);
        }
    }
}

