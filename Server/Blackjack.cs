//  Blackjack.cs
//  Author: Timothy Dexter
//  Release: 0.0.1
//  Date: 04/27/2019
//  
//   
//  Known Issues
//   
//   
//  Please send any edits/improvements/bugs to this script back to the author. 
//   
//  Usage 
//   
//   
//  History:
//  Revision 0.0.1 2019/05/02 10:26 PM EDT TimothyDexter 
//  - Initial release
//   

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CitizenFX.Core;
using Roleplay.Server.Classes.Institutions.GamblingGames.Blackjack.SharedModels;
using Roleplay.SharedClasses;
using Roleplay.SharedModels;
using Newtonsoft.Json;

namespace Roleplay.Server.Classes.Institutions.GamblingGames.Blackjack
{
	internal static class Blackjack
	{
		private const int MaxPlayers = 8;

		private static readonly Dictionary<int, BjGameShared> CreatedGames = new Dictionary<int, BjGameShared>();

		/// <summary>
		///     Initializes this instance.
		/// </summary>
		public static void Init() {
			Server.ActiveInstance.RegisterEventHandler( "Blackjack.CreateGame",
				new Action<Player, string, string, int, int, float>( HandleCreateGame ) );

			Server.ActiveInstance.RegisterEventHandler( "Blackjack.EndGame",
				new Action<Player>( HandleEndGame ) );

			Server.ActiveInstance.RegisterEventHandler( "Blackjack.PlayerQuit",
				new Action<Player>( HandlePlayerQuit ) );

			Server.ActiveInstance.RegisterEventHandler( "Blackjack.PlayerKicked",
				new Action<Player, string>( HandlePlayerKicked ) );

			Server.ActiveInstance.RegisterEventHandler( "Blackjack.GetNearbyGames",
				new Action<Player, string>( HandleGetNearbyGames ) );

			Server.ActiveInstance.RegisterEventHandler( "Blackjack.JoinNearbyGame",
				new Action<Player, int, string>( HandleJoinNearbyGame ) );

			Server.ActiveInstance.RegisterEventHandler( "Blackjack.StartGame",
				new Action<Player>( HandleStartGame ) );

			Server.ActiveInstance.RegisterEventHandler( "Blackjack.RequestRoundBets",
				new Action<Player>( HandleRequestRoundBets ) );

			Server.ActiveInstance.RegisterEventHandler( "Blackjack.SetPlayerBet",
				new Action<Player, int>( HandleSetPlayerBet ) );

			Server.ActiveInstance.RegisterEventHandler( "Blackjack.InsureHand",
				new Action<Player, int>( HandleInsureHand ) );

			Server.ActiveInstance.RegisterEventHandler( "Blackjack.ShowPlayerListInfo",
				new Action<Player>( HandleShowPlayerListInfo ) );

			Server.ActiveInstance.RegisterEventHandler( "Blackjack.SetChipCount",
				new Action<Player, int, int>( HandleSetChipCount ) );

			Server.ActiveInstance.RegisterEventHandler( "Blackjack.UpdateGame",
				new Action<Player, string>( HandleUpdateGame ) );

			Server.ActiveInstance.RegisterEventHandler( "Chat.BlackjackAction",
				new Action<Player, string>( HandleBlackjackAction ) );
		}

		private static void HandlePlayerKicked( [FromSource] Player source, string message ) {
			// HasDealerAccess
			if( !SessionManager.SessionList.ContainsKey( source.Handle ) ||
			    !int.TryParse( source.Handle, out int netId ) || !CreatedGames.ContainsKey( netId ) || CreatedGames[netId].Players == null ) return;

			foreach( var player in CreatedGames[netId].Players ) {
				if( !SessionManager.SessionList.TryGetValue( player.NetId.ToString(), out var session ) ) continue;

				session.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
					message );
			}
		}

		private static void HandlePlayerQuit( [FromSource] Player source ) {
			if( !SessionManager.SessionList.ContainsKey( source.Handle ) ||
			    !int.TryParse( source.Handle, out int netId ) ||
			    !CreatedGames.Values.Any( g => g.Players.Any( p => p.NetId == netId ) ) ) return;

			var gameToRemovePlayerFrom =
				CreatedGames.Values.FirstOrDefault( g => g.Players.Any( p => p.NetId == netId ) );
			var playerToRemove = gameToRemovePlayerFrom?.Players.FirstOrDefault( p => p.NetId == netId );

			if(gameToRemovePlayerFrom == null || playerToRemove == null ) return;

			string message =
				$"{playerToRemove.Name} in seat {playerToRemove.Position} left the game with ${playerToRemove.Chips} remaining.";

			Log.Verbose(
				$"{playerToRemove.Name} in seat {playerToRemove.Position} left {gameToRemovePlayerFrom.Name} with ${playerToRemove.Chips} remaining." );
			gameToRemovePlayerFrom.Players.Remove( playerToRemove );


			foreach( var player in gameToRemovePlayerFrom.Players ) {
				if( !SessionManager.SessionList.TryGetValue( player.NetId.ToString(), out var session ) ) continue;

				session.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
					message );
				session.TriggerEvent( "Blackjack.UpdateClientGame", JsonConvert.SerializeObject( gameToRemovePlayerFrom ) );
			}
		}

		/// <summary>
		///     Handles the end game.
		/// </summary>
		/// <param name="source">The source.</param>
		private static void HandleEndGame( [FromSource] Player source ) {
			try {
				// HasDealerAccess
				if( !SessionManager.SessionList.ContainsKey( source.Handle ) ||
				    !int.TryParse( source.Handle, out int netId ) || !CreatedGames.ContainsKey( netId ) ) return;

				HandleShowPlayerListInfo( source );

				var playerList = CreatedGames[netId].Players.Select( p => p.NetId.ToString() ).ToArray();
				SessionManager.SessionList.Where( p => p.Value.IsPlaying && playerList.Contains( p.Value.NetID ) )
					.ToList()
					.ForEach( p =>
						p.Value.TriggerEvent( "Blackjack.TableTerminated" ) );

				Log.Verbose( $"Ending blackjack game:{netId}" );
				CreatedGames.Remove( netId );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		/// <summary>
		///     Handles the update game.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="gameSharedModelData">The game shared model data.</param>
		private static void HandleUpdateGame( [FromSource] Player source, string gameSharedModelData ) {
			try {
				// Check client is player or dealer
				if( !SessionManager.SessionList.ContainsKey( source.Handle ) ||
				    !int.TryParse( source.Handle, out int netId ) ||
				    !CreatedGames.ContainsKey( netId ) &&
				    !CreatedGames.Values.Any( g => g.Players.Any( p => p.NetId == netId ) ) ||
				    string.IsNullOrEmpty( gameSharedModelData ) ) return;

				var gameSharedModel = JsonConvert.DeserializeObject<BjGameShared>( gameSharedModelData );
				if( gameSharedModel == null ) {
					Log.Info( "HandleUpdateGame: gameSharedModel null." );
					return;
				}

				int gameId = 0;
				if( CreatedGames.ContainsKey( netId ) ) {
					gameId = netId;
				}
				else {
					var playerGame = CreatedGames?.Values.FirstOrDefault( g => g.Players.Any( p => p.NetId == netId ) );
					if( playerGame?.Dealer !=
					    null )
						gameId = playerGame.Dealer.NetId;
				}

				if( !CreatedGames.ContainsKey( gameId ) ) {
					Log.Info( $"HandleUpdateGame: No game with ID:{gameId} exists to update." );
					return;
				}

				CreatedGames[gameId] = gameSharedModel;
				var netIds = GetGamePlayerNetIds( gameId );
				SessionManager.SessionList.Where( p => p.Value.IsPlaying && netIds.Contains( p.Value.NetID ) ).ToList()
					.ForEach( p =>
						p.Value.TriggerEvent( "Blackjack.UpdateClientGame", gameSharedModelData ) );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		/// <summary>
		///     Handles the create game.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="dealerName">Name of the dealer.</param>
		/// <param name="vector3Data">The vector3 data.</param>
		/// <param name="minimumBet">The minimum bet.</param>
		/// <param name="numberOfDecks">The number of decks.</param>
		/// <param name="blackjackBonus">The blackjack bonus.</param>
		private static void HandleCreateGame( [FromSource] Player source, string dealerName, string vector3Data,
			int minimumBet, int numberOfDecks, float blackjackBonus ) {
			try {
				if( !SessionManager.SessionList.ContainsKey( source.Handle ) || string.IsNullOrEmpty( vector3Data ) ||
				    !int.TryParse( source.Handle, out int netId ) ) return;

				if( dealerName.Length > 0 && char.IsDigit( dealerName[0] ) ) {
					source.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"Game name cannot start with a number." );
					return;
				}

				if( string.IsNullOrEmpty( dealerName ) ) dealerName = source.Handle;

				Log.Verbose( $"Creating blackjack game:{dealerName},{netId}" );
				var location = JsonConvert.DeserializeObject<Vector3>( vector3Data );
				var dealer = new BjDealerShared( netId, dealerName );
				var game = new BjGameShared( dealerName, dealer, location, new List<BjPlayerShared>(),
					Math.Max( 0, minimumBet ),
					MathUtil.Clamp( numberOfDecks, 2, 8 ),
					blackjackBonus );
				CreatedGames[netId] = game;

				source.TriggerEvent( "Blackjack.UpdateClientGame", JsonConvert.SerializeObject( game ) );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		/// <summary>
		///     Handles the get nearby games.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="vector3Data">The vector3 data.</param>
		private static void HandleGetNearbyGames( [FromSource] Player source, string vector3Data ) {
			try {
				if( !SessionManager.SessionList.ContainsKey( source.Handle ) ||
				    string.IsNullOrEmpty( vector3Data ) ) return;
				var location = JsonConvert.DeserializeObject<Vector3>( vector3Data );

				var gamesToRemove = new List<int>();
				foreach( var game in CreatedGames )
					if( !SessionManager.SessionList.ContainsKey( game.Value.Dealer.NetId.ToString() ) )
						gamesToRemove.Add( game.Key );

				foreach( int game in gamesToRemove ) CreatedGames.Remove( game );

				var nearbyGames = CreatedGames.Where( x => location.DistanceToSquared( x.Value.Location ) < 100 )
					.ToList();
				if( !nearbyGames.Any() ) {
					source.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX, "No games found." );
					return;
				}

				var builder = new StringBuilder();
				builder.Append(
					"<div style=\"border-bottom: 1px solid #ffffff; text-align: center;\">Nearby Games</div>" );
				foreach( var game in nearbyGames ) {
					var sanitizedName = "";
					if( !string.IsNullOrEmpty( game.Value.Name ) ) {
						sanitizedName = new String( game.Value.Name.Where( Char.IsLetterOrDigit ).ToArray() );
					}
					string minBet = $"${game.Value.MinimumBet} Minimum Bet";
					string deckString = game.Value.NumberOfDecks > 1 ? "decks" : "deck";
					builder.Append(
						$"{game.Value.Dealer.NetId} - {sanitizedName} ({game.Value.Players.Count}/{MaxPlayers}) | {minBet} | {game.Value.NumberOfDecks} {deckString} | {game.Value.BlackjackBonus}x blackjack bonus<br/>" );
				}

				source.TriggerEvent( "Chat.Message.Unsafe", builder.ToString() );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		/// <summary>
		///     Handles the join nearby game.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="gameId">The game identifier.</param>
		/// <param name="playerName">Name of the player.</param>
		private static void HandleJoinNearbyGame( [FromSource] Player source, int gameId, string playerName ) {
			try {
				if( !SessionManager.SessionList.ContainsKey( source.Handle ) ||
				    !int.TryParse( source.Handle, out int netId ) ) return;

				if( gameId < 0 || !CreatedGames.ContainsKey( gameId ) ) {
					source.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						$"{gameId} could not be found.  Use the id number or the game name to join." );
					return;
				}

				if( CreatedGames[gameId].Players.Count >= 8 ) {
					source.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						$"Game: {CreatedGames[gameId].Name} is full.  Wait until a spot is free or join another." );
					return;
				}

				var game = CreatedGames[gameId];
				if( !SessionManager.SessionList.ContainsKey( game.Dealer.NetId.ToString() ) ) {
					//Dealer has left server, remove the game
					CreatedGames.Remove( gameId );
					source.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						$"Game no longer exists: {gameId}." );
					return;
				}

				if( !AddPlayerToGame( gameId, netId, playerName ) ) {
					source.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						$"Failed to join game: {gameId}." );
					return;
				}

				source.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
					$"Joined game: {CreatedGames[gameId].Name} successfully." );
				source.TriggerEvent( "Blackjack.SetCurrentGame", JsonConvert.SerializeObject( CreatedGames[gameId] ) );

				var netIds = new List<int> {
					game.Dealer.NetId
				};
				netIds.AddRange( game.Players.Select( p => p.NetId ).ToList() );

				foreach( int id in netIds ) {
					//First remove any players that are no longer on the server
					if( !SessionManager.SessionList.TryGetValue( id.ToString(), out var session ) ) {
						RemovePlayerForLeavingServer( game, id );
						continue;
					}

					//Find player object we just added
					var playerToAdd = game.Players.FirstOrDefault( p => p.NetId == netId );
					if( playerToAdd != null ) {
						Log.Info( $"Sending out player update to netId:{id}." );
						//Update each player in the current game
						session?.TriggerEvent( "Blackjack.AddPlayerToGame",
							JsonConvert.SerializeObject( playerToAdd ) );
					}
					else {
						Log.Info( $"Could not find {netId} in game list." );
					}
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private static void RemovePlayerForLeavingServer( BjGameShared game, int playerNetId ) {
			var playerToRemove = game.Players.FirstOrDefault( p => p.NetId == playerNetId );
			if( playerToRemove != null ) {
				game.Players.Remove( playerToRemove );
				Log.Info( $"Removed player {playerToRemove.NetId} from game." );
			}
		}

		/// <summary>
		///     Adds the player to game.
		/// </summary>
		/// <param name="gameId">The game identifier.</param>
		/// <param name="playerId">The player identifier.</param>
		/// <param name="playerName">Name of the player.</param>
		/// <returns></returns>
		private static bool AddPlayerToGame( int gameId, int playerId, string playerName ) {
			try {
				if( !CreatedGames.ContainsKey( gameId ) ) return false;

				if( CreatedGames[gameId].Dealer.NetId == playerId ) {
					BaseScript.TriggerClientEvent( SessionManager.SessionList[playerId.ToString()].Player,
						"Chat.Message",
						"[Blackjack]", StandardColours.InfoHEX, "You cannot join the game you created." );
					return false;
				}

				if( CreatedGames[gameId].Players.Count >= 8 ) {
					BaseScript.TriggerClientEvent( SessionManager.SessionList[playerId.ToString()].Player,
						"Chat.Message",
						"[Blackjack]", StandardColours.InfoHEX,
						$"Game: {CreatedGames[gameId].Name} is full.  Wait until a spot is free or join another." );
					return false;
				}

				int firstAvailablePosition = 1;
				if( CreatedGames[gameId].Players.Any() ) {
					var takenPositions = CreatedGames[gameId].Players.Select( p => p.Position ).ToList();
					int freePosition = Enumerable.Range( 1, 8 ).Except( takenPositions ).FirstOrDefault();
					if( freePosition == default ) {
						BaseScript.TriggerClientEvent( SessionManager.SessionList[playerId.ToString()].Player,
							"Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
							"Unable to find a free position at the table." );
						return false;
					}

					firstAvailablePosition = freePosition;
				}

				Log.Info( $"Adding {playerId} to blackjack gameId[{gameId}] at position {firstAvailablePosition}" );
				CreatedGames[gameId].Players
					.Add( new BjPlayerShared( playerId, playerName, firstAvailablePosition, 0 ) );
				return true;
			}
			catch( Exception ex ) {
				Log.Error( ex );
				return false;
			}
		}

		/// <summary>
		///     Handles the start game.
		/// </summary>
		/// <param name="source">The source.</param>
		private static void HandleStartGame( [FromSource] Player source ) {
			try {
				// HasDealerAccess
				if( !SessionManager.SessionList.ContainsKey( source.Handle ) ||
				    !int.TryParse( source.Handle, out int netId ) || !CreatedGames.ContainsKey( netId ) ) return;

				if( !CreatedGames[netId].Players.Any() ) {
					source.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"You cannot start a game without players." );
					return;
				}

				CreatedGames[netId].IsActive = true;
				Log.Verbose( $"Starting game:{netId}" );

				var netIds = GetGamePlayerNetIds( netId );
				SessionManager.SessionList.Where( p => p.Value.IsPlaying && netIds.Contains( p.Value.NetID ) ).ToList()
					.ForEach( p =>
						p.Value.TriggerEvent( "Blackjack.SetCurrentGame",
							JsonConvert.SerializeObject( CreatedGames[netId] ) ) );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		/// <summary>
		///     Handles the request round bets.
		/// </summary>
		/// <param name="source">The source.</param>
		private static void HandleRequestRoundBets( [FromSource] Player source ) {
			try {
				// HasDealerAccess
				if( !SessionManager.SessionList.ContainsKey( source.Handle ) ||
				    !int.TryParse( source.Handle, out int netId ) || !CreatedGames.ContainsKey( netId ) ) return;

				var idsToRemove = new List<int>();
				foreach( var player in CreatedGames[netId].Players )
					if( !SessionManager.SessionList.ContainsKey( player.NetId.ToString() ) )
						idsToRemove.Add( player.NetId );

				var playersToRemove = CreatedGames[netId].Players.Where( p => idsToRemove.Contains( p.NetId ) );
				foreach( var player in playersToRemove ) CreatedGames[netId].Players.Remove( player );

				var playersToSend = CreatedGames[netId].Players;
				Log.Info(
					$"Sending playersCount={playersToSend.Count},bet={playersToSend?.FirstOrDefault()?.CurrentBet},chips={playersToSend?.FirstOrDefault()?.Chips}" );
				source.TriggerEvent( "Blackjack.SetRoundPlayersAndBets", JsonConvert.SerializeObject( playersToSend ) );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		/// <summary>
		///     Handles the set player bet.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="bet">The bet.</param>
		private static void HandleSetPlayerBet( [FromSource] Player source, int bet ) {
			try {
				if( !SessionManager.SessionList.ContainsKey( source.Handle ) ||
				    !int.TryParse( source.Handle, out int netId ) ||
				    !CreatedGames.Values.Any( g => g.Players.Any( p => p.NetId == netId ) ) ) return;

				var game = CreatedGames.Values.FirstOrDefault( g => g.Players.Any( p => p.NetId == netId ) );
				var player = game?.Players.FirstOrDefault( p => p.NetId == netId );

				if( player == null ) {
					source.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"You were not found in any active games." );
					return;
				}

				if( bet > player.Chips ) {
					source.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						$"You're trying to make a {bet} with only {player.Chips}.  Request more chips from the dealer." );
					return;
				}

				if( bet < game.MinimumBet ) {
					source.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						$"Your ${bet} does not meet the minimum bet ${game.MinimumBet}." );
					return;
				}

				Log.Info( $"Setting player {netId} bet to {bet}." );
				player.CurrentBet = bet;
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		/// <summary>
		///     Handles the insure hand.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="insureAmount">The insure amount.</param>
		private static void HandleInsureHand( [FromSource] Player source, int insureAmount ) {
			try {
				//Player access
				if( !SessionManager.SessionList.ContainsKey( source.Handle ) ||
				    !int.TryParse( source.Handle, out int netId ) ||
				    !CreatedGames.Values.Any( g => g.Players.Any( p => p.NetId == netId ) ) ) return;

				var player = CreatedGames.Values.FirstOrDefault( g => g.Players.Any( p => p.NetId == netId ) )
					?.Players
					.FirstOrDefault( p => p.NetId == netId );

				if( player == null ) {
					source.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"You were not found in any active games." );
					return;
				}

				if( insureAmount > player.Chips ) {
					source.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						$"You're trying to insure ${insureAmount} with only {player.Chips}.  Request more chips from the dealer." );
					return;
				}

				player.HandInsurance = insureAmount;
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		/// <summary>
		///     Handles the show player list information.
		/// </summary>
		/// <param name="source">The source.</param>
		private static void HandleShowPlayerListInfo( [FromSource] Player source ) {
			try {
				// Check client is player or dealer
				if( !SessionManager.SessionList.ContainsKey( source.Handle ) ||
				    !int.TryParse( source.Handle, out int netId ) || !CreatedGames.ContainsKey( netId ) &&
				    !CreatedGames.Values.Any( g => g.Players.Any( p => p.NetId == netId ) ) ) return;

				int gameId = 0;
				if( CreatedGames.ContainsKey( netId ) ) {
					gameId = netId;
				}
				else {
					var playerGame = CreatedGames?.Values.FirstOrDefault( g => g.Players.Any( p => p.NetId == netId ) );
					if( playerGame?.Dealer !=
					    null )
						gameId = playerGame.Dealer.NetId;
				}

				if( !CreatedGames.ContainsKey( gameId ) ) return;

				var game = CreatedGames[gameId];

				var builder = new StringBuilder();
				builder.Append(
					"<div style=\"border-bottom: 1px solid #ffffff; text-align: center;\">Position | Name | Chip Count</div>" );
				foreach( var player in game.Players ) {
					var sanitizedName = "";
					if( !string.IsNullOrEmpty( player.Name ) ) {
						sanitizedName = new String( player.Name.Where( Char.IsLetterOrDigit ).ToArray() );
					}
					builder.Append( $"{player.Position} | {sanitizedName} | ${player.Chips} <br/>" );
				}

				source.TriggerEvent( "Chat.Message.Unsafe", builder.ToString() );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		/// <summary>
		///     Handles the set chip count.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="position">The position.</param>
		/// <param name="chipCount">The chip count.</param>
		private static void HandleSetChipCount( [FromSource] Player source, int position, int chipCount ) {
			try {
				// HasDealerAccess
				if( !SessionManager.SessionList.ContainsKey( source.Handle ) ||
				    !int.TryParse( source.Handle, out int netId ) || !CreatedGames.ContainsKey( netId ) ) return;

				var game = CreatedGames[netId];
				var playerToUpdate = game.Players.FirstOrDefault( p => p.Position == position );
				if( playerToUpdate == null ) {
					source.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						$"Could not locate player in position: {position}" );
					return;
				}

				Log.Verbose(
					$"{netId} updated player {playerToUpdate.NetId}-{playerToUpdate.Name} from ${playerToUpdate.Chips} to ${chipCount}" );
				playerToUpdate.Chips = chipCount;
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		/// <summary>
		///     Gets the game player net ids.
		/// </summary>
		/// <param name="gameId">The game identifier.</param>
		/// <returns></returns>
		private static List<string> GetGamePlayerNetIds( int gameId ) {
			try {
				var netIds = new List<string> {
					CreatedGames[gameId]?.Dealer?.NetId.ToString()
				};
				var playerNetIds = CreatedGames[gameId]?.Players?.Select( p => p?.NetId.ToString() ).ToList();
				if( playerNetIds != null ) netIds.AddRange( playerNetIds );
				return netIds;
			}
			catch( Exception ex ) {
				Log.Error( ex );
				return null;
			}
		}

		/// <summary>
		///     Handles the blackjack action.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="data">The data.</param>
		private static void HandleBlackjackAction( [FromSource] Player source, string data ) {
			try {
				if( !SessionManager.SessionList.ContainsKey( source.Handle ) )
					return;

				var model = JsonConvert.DeserializeObject<LocalChatModel>( data );
				var session = SessionManager.SessionList[source.Handle];
				if( !session.IsPlaying ) return;

				model.Sender = Convert.ToInt32( session.NetID );
				BaseScript.TriggerClientEvent( "Chat.BlackjackAction", JsonConvert.SerializeObject( model ) );
			}
			catch( Exception ex ) {
				Log.Error( ex, "Exception thrown on ChatHandler#HandleAction()" );
			}
		}
	}
}