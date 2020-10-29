//  Blackjack.cs
//  Author: Timothy Dexter
//  Release: 0.0.1
//  Date: 04/20/2019
//  
//   
//  Known Issues
//   
//   
//  Please send any edits/improvements/bugs to this script back to the author. 
//   
//  Usage 
//  - /bj command lists available commands.
//   
//  History:
//  Revision 0.0.1 2019/05/02 10:17 PM EDT TimothyDexter 
//  - Initial release
//   

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using Common;
using Roleplay.Client.Classes.Environment;
using Roleplay.Client.Classes.Environment.UI;
using Roleplay.Client.Classes.Institutions.GamblingGames.Blackjack.SharedModels;
using Roleplay.Client.Classes.Player;
using Roleplay.SharedClasses;
using Roleplay.SharedModels;
using Newtonsoft.Json;

namespace Roleplay.Client.Classes.Institutions.GamblingGames.Blackjack
{
	internal class Blackjack
	{
		private const Control StandControl = Control.Enter;
		private const Control HitControl = Control.ThrowGrenade;
		private const Control SplitControl = Control.VehicleHeadlight;
		private const Control DoubleDownControl = Control.SpecialAbilitySecondary;
		private const Control ShowPlayersControl = Control.Sprint;

		public static BjGame CurrentGame;

		public static bool ShowPlayersHandHud = true;
		public static bool ShowExtraHandsHud;
		public static bool ShowInfoHud = true;
		public static bool ShowControls = true;

		private static bool _hasPlayerLeftTable;
		private static bool _isDealerRetrievingBets;

		private static DateTime _lastPlayerInput;
		public static bool HasPlayerLeftTable => _hasPlayerLeftTable || CurrentGame == null;

		/// <summary>
		///     Initializes this instance.
		/// </summary>
		public static void Init() {
			try {
				Client.ActiveInstance.ClientCommands.Register( "/bj", BlackjackCommands );

				Client.ActiveInstance.RegisterEventHandler( "Blackjack.SetCurrentGame",
					new Action<string>( HandleSetCurrentGame ) );

				Client.ActiveInstance.RegisterEventHandler( "Blackjack.AddPlayerToGame",
					new Action<string>( HandleAddPlayerToGame ) );

				Client.ActiveInstance.RegisterEventHandler( "Blackjack.SetRoundPlayersAndBets",
					new Action<string>( HandleSetRoundPlayersAndBets ) );

				Client.ActiveInstance.RegisterEventHandler( "Blackjack.UpdateClientGame",
					new Action<string>( HandleUpdateClientGame ) );

				Client.ActiveInstance.RegisterEventHandler( "Blackjack.PlayerQuit",
					new Action<string>( HandlePlayerQuit ) );

				Client.ActiveInstance.RegisterEventHandler( "Blackjack.TableTerminated",
					new Action( HandleTableTerminated ) );

				Client.ActiveInstance.RegisterEventHandler( "Chat.BlackjackAction",
					new Action<string>( HandleBlackjackAction ) );


				Client.ActiveInstance.RegisterTickHandler( RunDealerGameTick );
				Client.ActiveInstance.RegisterTickHandler( RunPlayerGameTick );
				Client.ActiveInstance.RegisterTickHandler( CheckHasPlayerLeftTableTick );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		/// <summary>
		///     Gets the instruction viewer.
		/// </summary>
		/// <returns></returns>
		public static InstructionViewer GetInstructionViewer() {
			Dictionary<Control, InstructionViewer.InstructionModel> instructions;
			if( IsClientDealer() && CurrentGame.Dealer.CurrentHand.IsActive )
				instructions = new Dictionary<Control, InstructionViewer.InstructionModel> {
					{StandControl, new InstructionViewer.InstructionModel( "Stand", 2 )},
					{HitControl, new InstructionViewer.InstructionModel( "Hit", 1 )},
					{ShowPlayersControl, new InstructionViewer.InstructionModel( "Shows Players", 0 )}
				};
			else
				instructions = new Dictionary<Control, InstructionViewer.InstructionModel> {
					{StandControl, new InstructionViewer.InstructionModel( "Stand", 4 )},
					{HitControl, new InstructionViewer.InstructionModel( "Hit", 3 )},
					{SplitControl, new InstructionViewer.InstructionModel( "Split", 2 )},
					{DoubleDownControl, new InstructionViewer.InstructionModel( "Double Down", 1 )},
					{ShowPlayersControl, new InstructionViewer.InstructionModel( "Shows Players", 0 )}
				};

			var instructionViewer = new InstructionViewer( instructions );

			return instructionViewer;
		}

		/// <summary>
		///     Determines whether [is client dealer].
		/// </summary>
		/// <returns>
		///     <c>true</c> if [is client dealer]; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsClientDealer() {
			return CurrentGame?.Dealer != null && CurrentGame.Dealer.NetId == CurrentPlayer.NetID;
		}

		/// <summary>
		///     Determines whether [is client player].
		/// </summary>
		/// <returns>
		///     <c>true</c> if [is client player]; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsClientPlayer() {
			return CurrentGame?.Players != null && CurrentGame.Players.Any( p => p.NetId == CurrentPlayer.NetID );
		}

		/// <summary>
		///     Determines whether [is client playing game].
		/// </summary>
		/// <returns>
		///     <c>true</c> if [is client playing game]; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsClientPlayingGame() {
			return IsClientPlayer() && CurrentGame.Location.DistanceToSquared( Cache.PlayerPos ) < 64f;
		}

		/// <summary>
		///     Gets the client player.
		/// </summary>
		/// <returns></returns>
		public static BjPlayer GetClientPlayer() {
			return CurrentGame?.Players?.FirstOrDefault( p => p.NetId == CurrentPlayer.NetID );
		}

		/// <summary>
		///     Handles the blackjack action.
		/// </summary>
		/// <param name="data">The data.</param>
		private static void HandleBlackjackAction( string data ) {
			var model = JsonConvert.DeserializeObject<LocalChatModel>( data );
			var sender = Session.OtherPlayers.FromNetID( model.Sender );
			if( sender == null || sender.Ped.Position.DistanceToSquared( Cache.PlayerPos ) > model.Radius ||
			    InstanceManager.GetInstanceIDForPlayer( sender.NetID ) !=
			    InstanceManager.GetInstanceIDForPlayer( CurrentPlayer.NetID ) )
				return;

			BaseScript.TriggerEvent( "Chat.Message", "", StandardColours.SlashMe, $"{model.Message}" );
		}

		/// <summary>
		///     Handles the table terminated.
		/// </summary>
		private static void HandleTableTerminated() {
			if( CurrentGame == null || !IsClientPlayer() ) return;

			BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
				$"Table has closed.  You walk away with ${CurrentGame?.Players?.FirstOrDefault( p => p.NetId == CurrentPlayer.NetID )?.Chips} chips." );

			CurrentGame = null;
		}

		/// <summary>
		///     Handles the player quit.
		/// </summary>
		/// <param name="message">The message.</param>
		private static void HandlePlayerQuit( string message ) {
			if( !string.IsNullOrEmpty( message ) )
				BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
					message );
		}

		/// <summary>
		///     Removes the player and update game.
		/// </summary>
		/// <param name="player">The player.</param>
		/// <returns></returns>
		private static bool RemovePlayerAndUpdateGame( BjPlayer player ) {
			CurrentGame.Players.Remove( player );
			Log.Info( "Attempting to update after leaving game." );
			if( !UpdatePlayersWithLatestGame() ) {
				BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
					"Failed to remove player from game." );
				return false;
			}

			return true;
		}

		/// <summary>
		///     Handles the add player to game.
		/// </summary>
		/// <param name="playerSharedModel">The player shared model.</param>
		private static void HandleAddPlayerToGame( string playerSharedModel ) {
			if( string.IsNullOrEmpty( playerSharedModel ) || CurrentGame == null ||
			    CurrentGame.Players.Count >= 8 ) return;

			var playerData = JsonConvert.DeserializeObject<BjPlayerShared>( playerSharedModel );
			if( playerData == null ) {
				Log.Info( "HandleAddPlayerToGame: playerData was null." );
				return;
			}

			var player = new BjPlayer( playerData.NetId, playerData.Name, playerData.Position, playerData.Chips );
			CurrentGame?.Players?.Add( player );
		}

		/// <summary>
		///     Called when dealer starts a game.
		/// </summary>
		/// <param name="gameSharedModel">The game shared model.</param>
		private static void HandleSetCurrentGame( string gameSharedModel ) {
			if( string.IsNullOrEmpty( gameSharedModel ) ) {
				Log.Info( "Received game model was empty." );
				return;
			}

			var gameData = JsonConvert.DeserializeObject<BjGameShared>( gameSharedModel );
			if( gameData == null ) return;

			var dealer = new BjDealer( gameData.Dealer.NetId, gameData.Dealer.Name );

			var players = new List<BjPlayer>();
			foreach( var player in gameData.Players )
				players.Add( new BjPlayer( player.NetId, player.Name, player.Position, player.Chips ) );

			CurrentGame = new BjGame( dealer, gameData.Location, players, gameData.MinimumBet, gameData.NumberOfDecks,
				gameData.BlackjackBonus ) {
				IsActive = true
			};
			_hasPlayerLeftTable = false;

			Log.Info(
				$"Set blackjack game: {CurrentGame.Dealer.Name}, {CurrentGame.Players.Count}, {CurrentGame.NumberOfDecks}, {CurrentGame.BlackjackBonus}" );
		}

		/// <summary>
		///     Handles the set round players and bets.
		/// </summary>
		/// <param name="playerSharedModel">The player shared model.</param>
		private static void HandleSetRoundPlayersAndBets( string playerSharedModel ) {
			if( CurrentGame == null ) return;
			if( string.IsNullOrEmpty( playerSharedModel ) ) {
				Log.Info( "Received players model was empty." );
				return;
			}

			var playerData = JsonConvert.DeserializeObject<List<BjPlayerShared>>( playerSharedModel );
			var convertedPlayerData = playerData.ConvertToPlayers();
			if( convertedPlayerData == null ) {
				Log.Info( "HandleSetRoundPlayersAndBets convertedPlayerData is null" );
				return;
			}

			var idsToRemove = new List<int>();
			for( int index = 0; index < CurrentGame.Players.Count; index++ ) {
				var player = CurrentGame.Players[index];
				var matchedPlayerData = convertedPlayerData.FirstOrDefault( p => p.NetId == player.NetId );
				if( matchedPlayerData == null ) {
					//Player has left the game
					idsToRemove.Add( player.NetId );
					Log.Info( $"HandleSetRoundPlayersAndBets remove {player.NetId}" );
					continue;
				}

				CurrentGame.Players[index] = matchedPlayerData;
			}

			var playersToRemove = CurrentGame.Players.Where( p => idsToRemove.Contains( p.NetId ) );
			foreach( var player in playersToRemove ) CurrentGame.Players.Remove( player );

			_isDealerRetrievingBets = false;
		}

		/// <summary>
		///     Handles the update client game.
		/// </summary>
		/// <param name="gameModel">The game model.</param>
		private static void HandleUpdateClientGame( string gameModel ) {
			if( string.IsNullOrEmpty( gameModel ) ) {
				Log.Info( "Received game model was empty." );
				return;
			}

			var gameData = JsonConvert.DeserializeObject<BjGameShared>( gameModel );
			var updatedGame = gameData?.ConvertToGame( CurrentGame?.Shoe );
			if( updatedGame == null ) {
				Log.Info( "HandleUpdateClientGame:Received null game" );
				return;
			}

			if( updatedGame.Dealer.NetId != CurrentPlayer.NetID &&
			    updatedGame.Players.All( p => p.NetId != CurrentPlayer.NetID ) )
				CurrentGame = null;
			else
				CurrentGame = updatedGame;
		}

		/// <summary>
		///     Blackjacks the commands.
		/// </summary>
		/// <param name="cmd">The command.</param>
		private static void BlackjackCommands( Command cmd ) {
			if( cmd.Args.Count < 1 ) {
				BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
					"Usage: /bj create|start|find|join|bet|chips|insure|hud|list|kick|end" );
				return;
			}

			string[] args = cmd.Args.ToString().Split( ' ' ).ToArray();
			string childCommand = cmd.Args.Get( 0 ).ToLower();
			if( "create".StartsWith( childCommand ) ) {
				if( IsClientPlayingGame() )
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"Leave your current game before creating a new one." );

				if( cmd.Args.Count < 5 || !int.TryParse( args[cmd.Args.Count - 3], out int minBet ) ||
				    !int.TryParse( args[cmd.Args.Count - 2], out int numOfDecks ) ||
				    !float.TryParse( args[cmd.Args.Count - 1], out float blackjackBonus ) ||
				    blackjackBonus < 1 ) {
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"Usage: /bj create [dealer name] [minimum bet] [number of decks] [blackjack bonus] e.g. /bj create Charlie Chips 10 6 1.5" );
					return;
				}

				BaseScript.TriggerServerEvent( "Blackjack.CreateGame", string.Join( " ", args, 1, cmd.Args.Count - 4 ),
					JsonConvert.SerializeObject( Cache.PlayerPos ), minBet, numOfDecks, blackjackBonus );
			}
			else if( "start".StartsWith( childCommand ) ) {
				//Server deals with user access to starting a game
				BaseScript.TriggerServerEvent( "Blackjack.StartGame", JsonConvert.SerializeObject( Cache.PlayerPos ) );
			}
			else if( "find".StartsWith( childCommand ) ) {
				BaseScript.TriggerServerEvent( "Blackjack.GetNearbyGames",
					JsonConvert.SerializeObject( Cache.PlayerPos ) );
			}
			else if( "join".StartsWith( childCommand ) ) {
				if( cmd.Args.Count < 3 || !int.TryParse( args[1], out int gameId ) ) {
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"Usage: /bj join [game id] [player name] e.g. /bj join 1 Pistol Pete" );
					return;
				}

				// startIndex at 2 throws an exception for some reason
				string argString = string.Join( " ", args, 1, cmd.Args.Count - 1 );
				string playerName = argString.Remove( 0, argString.IndexOf( ' ' ) + 1 );
				BaseScript.TriggerServerEvent( "Blackjack.JoinNearbyGame", gameId, playerName );
			}
			else if( "bet".StartsWith( childCommand ) ) {
				if( !IsClientPlayingGame() ) {
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"You are not currently playing a game." );
					return;
				}

				int betAmount = 0;
				if( cmd.Args.Count < 2 ) {
					var player = CurrentGame?.Players.FirstOrDefault( p => p.NetId == CurrentPlayer.NetID );
					if( player != null ) betAmount = player.CurrentBet;
				}
				else {
					int.TryParse( cmd.Args.Get( 1 ), out betAmount );
				}

				if( betAmount < 0 ) {
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"Usage: /bj bet [bet amount (Note: leave blank to repeat previous round bet or 0 to sit out)] e.g. /bj bet 60" );
					return;
				}

				BaseScript.TriggerServerEvent( "Blackjack.SetPlayerBet", betAmount );
			}
			else if( "chips".StartsWith( childCommand ) ) {
				if( !IsClientDealingGame() ) {
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"You can't give chips if you are not currently dealing a game." );
					return;
				}

				if( cmd.Args.Count < 3 || !int.TryParse( cmd.Args.Get( 1 ), out int playerPosition ) ||
				    playerPosition < 1 || playerPosition > 8 ||
				    !int.TryParse( cmd.Args.Get( 2 ), out int chipCount ) ||
				    chipCount < 0 ) {
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"Usage: /bj chips [player position] [chip amount] e.g. /bj chips 1 5000" );
					return;
				}

				BaseScript.TriggerServerEvent( "Blackjack.SetChipCount", playerPosition, chipCount );
			}
			else if( "insure".StartsWith( childCommand ) ) {
				if( !IsClientPlayer() ) {
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"You are not currently dealing a game." );
					return;
				}

				if( cmd.Args.Count < 2 || !int.TryParse( cmd.Args.Get( 1 ), out int insureAmount ) ) {
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"Usage: /bj insure [insurance amount] e.g. /bj insure 100" );
					return;
				}

				BaseScript.TriggerServerEvent( "Blackjack.InsureHand", insureAmount );
			}
			else if( "hud".StartsWith( childCommand ) ) {
				if( cmd.Args.Count < 2 ) {
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"Usage: /bj hud info|controls|players|hands" );
					return;
				}

				string hudOption = cmd.Args.Get( 1 ).ToLower();
				if( "info".StartsWith( hudOption ) ) {
					ShowInfoHud = !ShowInfoHud;
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						$"Showing info hud:{ShowInfoHud}" );
				}
				else if( "controls".StartsWith( hudOption ) ) {
					ShowControls = !ShowControls;
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						$"Showing controls:{ShowControls}" );
				}
				else if( "players".StartsWith( hudOption ) ) {
					ShowPlayersHandHud = !ShowPlayersHandHud;
					if( IsClientPlayer() && ShowPlayersHandHud && ShowExtraHandsHud ) ShowExtraHandsHud = false;
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						$"Showing other plays hands hud:{ShowPlayersHandHud}" );
				}
				else if( "hands".StartsWith( hudOption ) ) {
					if( IsClientDealer() ) return;
					ShowExtraHandsHud = !ShowExtraHandsHud;
					if( IsClientPlayer() && ShowExtraHandsHud && ShowPlayersHandHud ) ShowPlayersHandHud = false;
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						$"Showing your additional hand hud:{ShowExtraHandsHud}" );
				}
			}
			else if( "list".StartsWith( childCommand ) ) {
				if( !IsClientDealingGame() || !IsClientPlayingGame() ) {
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"You are not currently in an active game." );
					return;
				}

				BaseScript.TriggerServerEvent( "Blackjack.ShowPlayerListInfo" );
			}
			else if( "kick".StartsWith( childCommand ) ) {
				if( !IsClientDealingGame() ) {
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"You are not currently dealing a game." );
					return;
				}

				if( cmd.Args.Count < 2 || !int.TryParse( cmd.Args.Get( 1 ), out int playerPosition ) ) {
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"Usage: /bj kick [player position] e.g. /bj kick 3" );
					return;
				}

				var playerToRemove = CurrentGame.Players.FirstOrDefault( p => p.Position == playerPosition );
				if( playerToRemove == null ) {
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						$"There is no player in position {playerPosition}." );
					return;
				}

				if( RemovePlayerAndUpdateGame( playerToRemove ) ) {
					var playersToInform = CurrentGame.Players.Select( p => p.NetId ).ToList();
					playersToInform.Add( CurrentGame.Dealer.NetId );
					string message =
						$"{playerToRemove.Name} in seat {playerToRemove.Position} has been removed from the game with ${playerToRemove.Chips} remaining.";

					BaseScript.TriggerServerEvent( "Blackjack.PlayerKicked", message );
				}
			}
			else if( "end".StartsWith( childCommand ) ) {
				if( !IsClientDealingGame() && !IsClientPlayingGame() ) {
					BaseScript.TriggerEvent( "Chat.Message", "[Blackjack]", StandardColours.InfoHEX,
						"You are not in an active game." );
					return;
				}

				if( IsClientDealer() ) {
					BaseScript.TriggerServerEvent( "Blackjack.EndGame" );
				}
				else {
					ClientPlayerQuitGame();
				}

				CurrentGame = null;
			}
		}

		/// <summary>
		///     Runs the dealer game tick.
		/// </summary>
		/// <returns></returns>
		private static async Task RunDealerGameTick() {
			try {
				if( !Session.HasJoinedRP || CurrentGame == null || !CurrentGame.IsActive ||
				    !IsClientDealer() || HasPlayerLeftTable ) {
					if( CurrentGame != null && CurrentGame.IsActive && !IsClientPlayer() ) {
						CurrentGame.IsActive = false;
						UpdatePlayersWithLatestGame();
					}

					await BaseScript.Delay( 100 );
					return;
				}

				BjDraw.DrawPlayerNamesAndChips();
				while( CurrentGame != null && CurrentGame.IsActive && !HasPlayerLeftTable ) {
					//Retrieve round bets
					await GetPlayerBets();
					if( HasPlayerLeftTable ) return;
					//Make sure shoe has at least 15% of its original size
					//Otherwise offer dealer reload before they deal
					await CheckIfShoeNeedsReload();
					if( HasPlayerLeftTable ) return;

					if( !CurrentGame.DealNewRound() ) {
						Log.Info( $"Shoe ran out of cards - only {CurrentGame.Shoe.ShoeCards.Count} cards remaining" );
						return;
					}

					//Update chips
					foreach( var player in CurrentGame.Players ) {
						if( player.CurrentBet <= 0 ) continue;
						player.Chips = player.Chips - player.CurrentBet;
					}

					//Everyone has cards, update players
					UpdatePlayersWithLatestGame();
					//Draw dealer hand until dealer clears round
					BjDraw.DrawDealerHand();
					BjDraw.DrawDealerInstructions();
					BjDraw.DrawOtherPlayersHands();
					BjDraw.DrawWagerWindow();

					if( ShouldCheckForDealerBlackjack() ) await GetPlayerInsurance();
					//check for dealer blackjack
					if( ShouldCheckForDealerBlackjack() && CurrentGame.Dealer.CurrentHand.IsBlackjack ) {
						//Do nothing
					}
					else {
						await ProcessPlayersHands();
						if( HasPlayerLeftTable ) return;

						await ProcessDealerHand();
						if( HasPlayerLeftTable ) return;
					}

					await FinalizeRound( CurrentGame.Dealer.CurrentHand.IsBlackjack );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		/// <summary>
		///     Runs the player game tick.
		/// </summary>
		/// <returns></returns>
		private static async Task RunPlayerGameTick() {
			try {
				if( !Session.HasJoinedRP || CurrentGame == null || !CurrentGame.IsActive || !IsClientPlayer() ||
				    HasPlayerLeftTable ) {
					if( CurrentGame != null && !IsClientDealer() && HasPlayerLeftTable ) ClientPlayerQuitGame();

					await BaseScript.Delay( 100 );
					return;
				}

				BjDraw.DrawPlayerNamesAndChips();
				while( !HasPlayerLeftTable && CurrentGame != null && CurrentGame.IsActive ) {
					while( !HasPlayerLeftTable && GetClientPlayer() != null &&
					       GetClientPlayer().CurrentHands?.FirstOrDefault() == null ) {
						Screen.DisplayHelpTextThisFrame(
							"Waiting for cards to be dealt." );
						await BaseScript.Delay( 0 );
					}

					//Draw dealer hand until dealer clears round
					BjDraw.DrawDealerHand();
					BjDraw.DrawWagerWindow();
					BjDraw.DrawOtherPlayersHands();
					DrawClientPlayerHand();

					await ProcessClientPlayerHands();


					await BaseScript.Delay( 0 );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		/// <summary>
		/// Clients the player quit game.
		/// </summary>
		/// <exception cref="System.NotImplementedException"></exception>
		private static void ClientPlayerQuitGame() {
			BaseScript.TriggerServerEvent( "Blackjack.PlayerQuit" );
		}

		/// <summary>
		///     Processes the client player hands.
		/// </summary>
		/// <returns></returns>
		private static async Task ProcessClientPlayerHands() {
			while( !HasPlayerLeftTable && CurrentGame?.Dealer != null && GetClientPlayer() != null &&
			       GetClientPlayer().CurrentWager > 0 &&
			       !CurrentGame.Dealer.HasDeclaredActionOver ) {
				var instructionViewer = GetInstructionViewer();
				if( GetClientPlayer()?.GetActiveHand != null ) {
					instructionViewer.ShowInstructions( InstructionViewer.OrientationEnum.Vertical );

					if( DateTime.Now.CompareTo( _lastPlayerInput.AddMilliseconds( 1500 ) ) > 0 ) {
						var playerAction = GetPlayerActionInput();
						if( Enum.IsDefined( typeof(BjActionsEnum), playerAction ) ) {
							ProcessClientPlayerAction( playerAction, GetClientPlayer().GetActiveHand );
							_lastPlayerInput = DateTime.Now;
						}
					}
				}

				await BaseScript.Delay( 0 );
			}
		}

		/// <summary>
		///     Processes the client player action.
		/// </summary>
		/// <param name="playerAction">The player action.</param>
		/// <param name="hand">The hand.</param>
		private static void ProcessClientPlayerAction( BjActionsEnum playerAction, BjHand hand ) {
			var player = GetClientPlayer();
			if( player == null ) return;
			string actionString = Enum.GetName( typeof(BjActionsEnum), playerAction );

			string message =
				$"{player.Name} in seat {player.Position} signals to {actionString.AddSpacesToCamelCase().ToLower()} with {hand} on a ${hand.Bet} bet. ";
			var model = new LocalChatModel {
				Message = message,
				Radius = 64f,
				RequiresLineOfSight = true
			};

			BaseScript.TriggerServerEvent( "Chat.BlackjackAction", JsonConvert.SerializeObject( model ) );
		}

		/// <summary>
		///     Checks the has player left table tick.
		/// </summary>
		/// <returns></returns>
		private static async Task CheckHasPlayerLeftTableTick() {
			if( !Session.HasJoinedRP || CurrentGame == null || !CurrentGame.IsActive ) {
				await BaseScript.Delay( 100 );
				return;
			}

			_hasPlayerLeftTable = Cache.PlayerPos.DistanceToSquared( CurrentGame.Location ) > 64f;
			await BaseScript.Delay( 0 );
		}

		/// <summary>
		///     Finalizes the round.
		/// </summary>
		/// <param name="dealerHasBlackjack">if set to <c>true</c> [dealer has blackjack].</param>
		/// <returns></returns>
		private static async Task FinalizeRound( bool dealerHasBlackjack ) {
			for( int index = 0; !HasPlayerLeftTable && index < CurrentGame.Players.Count; index++ )
				CurrentGame.Players[index].HandleRoundPayout( CurrentGame.Dealer.CurrentHand.MaxValue,
					CurrentGame.BlackjackBonus,
					dealerHasBlackjack );

			while( !HasPlayerLeftTable ) {
				Screen.DisplayHelpTextThisFrame(
					$"Press {GetControlString( Control.Pickup )} to declare round over. " );
				if( ControlHelper.IsControlPressed( Control.Pickup ) ) {
					CurrentGame.Dealer.HasDeclaredActionOver = true;
					//Chips updated, update players
					UpdatePlayersWithLatestGame();
					await BaseScript.Delay( 250 );
					break;
				}

				await BaseScript.Delay( 0 );
			}
		}

		/// <summary>
		///     Updates the players with latest game.
		/// </summary>
		/// <returns></returns>
		private static bool UpdatePlayersWithLatestGame() {
			var sharedGame = CurrentGame?.ConvertToGameShared();
			if( sharedGame == null ) {
				Log.Info( "UpdatePlayersWithLatestGame: Failed to convert current game to sharedGame." );
				return false;
			}

			BaseScript.TriggerServerEvent( "Blackjack.UpdateGame", JsonConvert.SerializeObject( sharedGame ) );

			return true;
		}

		/// <summary>
		///     Gets the player bets.
		/// </summary>
		/// <returns></returns>
		private static async Task GetPlayerBets() {
			while( !HasPlayerLeftTable ) {
				var control = Control.Pickup;
				Screen.DisplayHelpTextThisFrame(
					$"Inform players to make their bets.\nWhen players are ready, press {GetControlString( control )} to start a new round. " );
				if( ControlHelper.IsControlPressed( control ) ) {
					await BaseScript.Delay( 250 );
					CurrentGame.Dealer.HasDeclaredActionOver = false;
					BaseScript.TriggerServerEvent( "Blackjack.RequestRoundBets" );
					break;
				}

				await BaseScript.Delay( 0 );
			}

			_isDealerRetrievingBets = true;
			var timeout = DateTime.Now.AddMinutes( 2 );
			while( !HasPlayerLeftTable && _isDealerRetrievingBets && DateTime.Now.CompareTo( timeout ) < 0 )
				await BaseScript.Delay( 50 );
		}

		/// <summary>
		///     Gets the player insurance.
		/// </summary>
		/// <returns></returns>
		private static async Task GetPlayerInsurance() {
			while( !HasPlayerLeftTable ) {
				var continueControl = Control.Pickup;
				var peekControl = Control.Sprint;
				Screen.DisplayHelpTextThisFrame(
					$"Offer plays insurance up to 1/2 their bet paying 2:1.\n Press{GetControlString( peekControl )} to peek card.\nWhen players are ready, press {GetControlString( continueControl )} to continue. " );
				if( ControlHelper.IsControlPressed( continueControl ) ) {
					BaseScript.TriggerServerEvent( "Blackjack.RequestRoundBets" );
					break;
				}

				if( CurrentGame?.Dealer != null && Game.IsControlPressed( 0, peekControl ) ) {
					CurrentGame.Dealer.IsDealerPeekingCard = true;
				}
				else {
					if( CurrentGame?.Dealer != null ) CurrentGame.Dealer.IsDealerPeekingCard = false;
				}

				await BaseScript.Delay( 0 );
			}

			_isDealerRetrievingBets = true;
			var timeout = DateTime.Now.AddMinutes( 2 );
			while( !HasPlayerLeftTable && _isDealerRetrievingBets && DateTime.Now.CompareTo( timeout ) < 0 )
				await BaseScript.Delay( 50 );
			Log.Info( "Received player insurance" );
		}

		/// <summary>
		///     Checks if shoe needs reload.
		/// </summary>
		/// <returns></returns>
		private static async Task CheckIfShoeNeedsReload() {
			int cardsLeftInShoe = CurrentGame.Shoe.ShoeCards.Count;
			int shoeOriginalSize = 52 * CurrentGame.NumberOfDecks;
			if( cardsLeftInShoe <= 0.15 * shoeOriginalSize )
				while( !HasPlayerLeftTable ) {
					var yesControl = Control.MpTextChatTeam;
					var noControl = Control.PushToTalk;
					Screen.DisplayHelpTextThisFrame(
						$"Only {cardsLeftInShoe} remaining, do you want to load a new shoe? {GetControlString( yesControl )} to load new a shoe or {GetControlString( noControl )} to continue with current shoe." );
					if( ControlHelper.IsControlPressed( yesControl ) ) {
						CurrentGame.Shoe.LoadNewShoe( CurrentGame.NumberOfDecks );
						break;
					}

					if( ControlHelper.IsControlPressed( noControl ) ) break;

					await BaseScript.Delay( 0 );
				}
		}

		/// <summary>
		///     Processes the players hands.
		/// </summary>
		/// <returns></returns>
		private static async Task ProcessPlayersHands() {
			while( !HasPlayerLeftTable && CurrentGame.DoesPlayerActionRemain() ) {
				for( int position = 1;
					!HasPlayerLeftTable && position <= CurrentGame.Players.OrderBy( p => p.Position ).ToArray().Length;
					position++ ) {
					if( CurrentGame.Players.FirstOrDefault( p => p.Position == position ) == null ) continue;

					for( int index = 0;
						!HasPlayerLeftTable && index < CurrentGame.Players.FirstOrDefault( p => p.Position == position )
							?.CurrentHands.Count;
						index++ ) {
						var currentHand = CurrentGame.Players.FirstOrDefault( p => p.Position == position )
							?.CurrentHands[index];
						if( currentHand != null ) {
							currentHand.IsActive = true;
							//Hand changed, update players
							UpdatePlayersWithLatestGame();
							BjDraw.DrawPlayerCurrentHandForDealer( position, 0.346f, 0.875f, 0.07f );

							while( !HasPlayerLeftTable && currentHand != null && currentHand.IsActive ) {
								//Get and respond to player input until hand is finished
								while( !HasPlayerLeftTable ) {
									if( CurrentGame.Players.FirstOrDefault( p => p.Position == position ) == null )
										continue;
									currentHand = CurrentGame.Players.FirstOrDefault( p => p.Position == position )
										?.CurrentHands[index];

									if( currentHand != null && currentHand.ActionFinished ) {
										var currentControl = Control.Pickup;
										Screen.DisplayHelpTextThisFrame(
											$"Press {GetControlString( currentControl )} to move on to next hand. " );
										if( ControlHelper.IsControlPressed( currentControl ) ) {
											if( CurrentGame?.Players?.FirstOrDefault( p => p.Position == position ) ==
											    null ) continue;
											CurrentGame.Players.FirstOrDefault( p => p.Position == position )
												.CurrentHands[index].IsActive = false;
											await BaseScript.Delay( 250 );
											break;
										}
									}
									else {
										var playerAction = GetPlayerActionInput();

										if( Enum.IsDefined( typeof(BjActionsEnum), playerAction ) ) {
											if( !CurrentGame.Shoe.ShoeHasCardsToCompleteAction( playerAction ) ) {
												Log.Info( "Shoe out of cards." );
												CurrentGame?.Shoe.LoadNewShoe( CurrentGame.NumberOfDecks );
											}

											if( CurrentGame?.Players?.FirstOrDefault( p => p.Position == position ) ==
											    null ) continue;
											CurrentGame.Players.FirstOrDefault( p => p.Position == position )
												?.PerformAction( playerAction, CurrentGame.Shoe );
											//Hand updated, update players
											UpdatePlayersWithLatestGame();
											await BaseScript.Delay( 250 );
											break;
										}
									}

									await BaseScript.Delay( 0 );
								}

								await BaseScript.Delay( 0 );
							}
						}
					}
				}

				await BaseScript.Delay( 0 );
			}
		}

		/// <summary>
		///     Processes the dealer hand.
		/// </summary>
		/// <returns></returns>
		private static async Task ProcessDealerHand() {
			CurrentGame.Dealer.CurrentHand.IsActive = true;
			UpdatePlayersWithLatestGame();

			int lastHandValue = CurrentGame.Dealer.CurrentHand.MaxValue;
			while( !HasPlayerLeftTable && !CurrentGame.Dealer.CurrentHand.ActionFinished ) {
				if( lastHandValue != CurrentGame.Dealer.CurrentHand.MaxValue ) {
					UpdatePlayersWithLatestGame();
					lastHandValue = CurrentGame.Dealer.CurrentHand.MaxValue;
				}

				//Get and respond to player input until hand is finished
				while( !HasPlayerLeftTable ) {
					if( ControlHelper.IsControlPressed( HitControl ) ) {
						Log.Info( "Hitting hand." );
						if( !CurrentGame.Dealer.CurrentHand.HitHand( CurrentGame.Shoe ) ) {
							Log.Info( $"Shoe out of cards. Reloading shoe with {CurrentGame.NumberOfDecks} decks" );
							CurrentGame.Shoe.LoadNewShoe( CurrentGame.NumberOfDecks );
						}

						await BaseScript.Delay( 250 );
						break;
					}

					if( ControlHelper.IsControlPressed( StandControl ) ) {
						Log.Info( "Standing hand." );
						CurrentGame.Dealer.CurrentHand.IsStanding = true;
						await BaseScript.Delay( 250 );
						break;
					}

					await BaseScript.Delay( 0 );
				}

				await BaseScript.Delay( 0 );
			}
		}

		/// <summary>
		///     Gets the player action input.
		/// </summary>
		/// <returns></returns>
		private static BjActionsEnum GetPlayerActionInput() {
			var playerAction = (BjActionsEnum)(-666);

			if( ControlHelper.IsControlPressed( HitControl ) )
				playerAction = BjActionsEnum.Hit;
			else if( ControlHelper.IsControlPressed( StandControl ) )
				playerAction = BjActionsEnum.Stand;
			else if( ControlHelper.IsControlPressed( SplitControl ) )
				playerAction = BjActionsEnum.Split;
			else if( ControlHelper.IsControlPressed( DoubleDownControl ) ) playerAction = BjActionsEnum.DoubleDown;

			return playerAction;
		}

		/// <summary>
		///     Draws the client player hand.
		/// </summary>
		private static async void DrawClientPlayerHand() {
			while( !HasPlayerLeftTable &&
			       CurrentGame?.Players?.FirstOrDefault( p => p.NetId == CurrentPlayer.NetID ) != null &&
			       CurrentGame?.Players?.FirstOrDefault( p => p.NetId == CurrentPlayer.NetID )?.CurrentHands
				       ?.FirstOrDefault() != null
			       && (CurrentGame?.Dealer != null) & !CurrentGame.HasDealerFinishedHand ) {
				var clientPlayer = CurrentGame?.Players?.FirstOrDefault( p => p.NetId == CurrentPlayer.NetID );
				var handToDraw = clientPlayer?.GetActiveHand ?? clientPlayer?.CurrentHands.LastOrDefault();
				if( clientPlayer == null || handToDraw == null ) continue;

				handToDraw.DrawActiveHand( 0.346f, 0.875f, 0.07f );

				if( ShowExtraHandsHud && clientPlayer.CurrentHands.Count > 1 ) {
					float yStart = 0.0625f;
					foreach( var otherHand in clientPlayer.CurrentHands ) {
						if( otherHand == handToDraw ) continue;
						otherHand.DrawActiveHand( 0.85375f, yStart, 0.025f );
						UI.DrawText( $"{otherHand.GetHandValueString()} ${otherHand.Bet}",
							new Vector2( 0.85375f - 0.0135f, yStart + 0.025f ), Color.FromArgb( 255, 255, 255, 255 ),
							0.5f,
							Font.ChaletComprimeCologne );
						yStart += 0.1f;
					}
				}

				await BaseScript.Delay( 0 );
			}
		}

		/// <summary>
		///     Determines whether [is client dealing game].
		/// </summary>
		/// <returns>
		///     <c>true</c> if [is client dealing game]; otherwise, <c>false</c>.
		/// </returns>
		private static bool IsClientDealingGame() {
			return IsClientDealer() &&
			       CurrentGame.Location.DistanceToSquared( Cache.PlayerPos ) < 64f;
		}

		/// <summary>
		///     Should check for dealer blackjack.
		/// </summary>
		/// <returns></returns>
		private static bool ShouldCheckForDealerBlackjack() {
			if( CurrentGame?.Dealer == null ) return false;
			return CurrentGame.Dealer.ShouldOfferInsurance();
		}

		/// <summary>
		///     Gets the control string.
		/// </summary>
		/// <param name="inputControl">The input control.</param>
		/// <returns></returns>
		private static string GetControlString( Control inputControl ) {
			string controlName = Enum.GetName( typeof(Control), inputControl );
			if( string.IsNullOrEmpty( controlName ) ) return "";

			string formattedControlName = controlName.Aggregate( string.Empty, ( result, next ) => {
				if( char.IsUpper( next ) && result.Length > 0 ) result += '_';
				return result + next;
			} );
			return "~INPUT_" + formattedControlName + '~';
		}
	}
}