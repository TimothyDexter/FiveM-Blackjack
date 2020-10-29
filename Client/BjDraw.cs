using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Roleplay.Client.Classes.Environment.UI;
using Roleplay.SharedClasses;

namespace Roleplay.Client.Classes.Institutions.GamblingGames.Blackjack
{
	internal static class BjDraw
	{
		/// <summary>
		/// Draws the player current hand.
		/// </summary>
		/// <param name="playerPosition">The player position.</param>
		/// <param name="xStartPos">The x start position.</param>
		/// <param name="yPos">The y position.</param>
		/// <param name="width">The width.</param>
		public static async void DrawPlayerCurrentHandForDealer( int playerPosition, float xStartPos, float yPos, float width ) {
			var player = Blackjack.CurrentGame.Players.FirstOrDefault( p => p.Position == playerPosition );
			while( !Blackjack.HasPlayerLeftTable && player?.GetActiveHand != null && player.GetActiveHand.IsActive ) {

				player = Blackjack.CurrentGame.Players.FirstOrDefault( p => p.Position == playerPosition );

				player?.GetActiveHand?.DrawActiveHand( xStartPos, yPos, width );

				if( Blackjack.IsClientDealer() )
					UI.DrawText( $"{player?.Position} - {player?.Name} ${player?.GetActiveHand?.Bet}",
						new Vector2( xStartPos + -0.06f, yPos + -.125f ),
						Color.FromArgb( 255, 255, 255, 255 ), 0.5f,
						Font.ChaletComprimeCologne ); //only for dealer

				await BaseScript.Delay( 0 );
			}

			Log.Info($"DrawPlayerCurrentHandForDealer: {player?.GetActiveHand != null} && {!Blackjack.HasPlayerLeftTable} && {player?.GetActiveHand?.IsActive}" );
		}

		/// <summary>
		///     Draws the card.
		/// </summary>
		/// <param name="card">The card.</param>
		/// <param name="screenX">The screen x.</param>
		/// <param name="screenY">The screen y.</param>
		/// <param name="width">The width.</param>
		public static void DrawCard( PlayingCard card, float screenX, float screenY, float width ) {
			const float heightMultiplier = 2.5f;
			string dict = "standard_cards";

			API.DrawSprite( dict, card.ToString(), screenX, screenY, width,
				width * heightMultiplier, 0,
				255, 255, 255, 255 );
		}

		/// <summary>
		///     Draws the active hand background.
		/// </summary>
		/// <param name="cardWidth">Width of the card.</param>
		/// <param name="xPos">The x position.</param>
		/// <param name="yPos">The y position.</param>
		public static void DrawActiveHandBackground( float cardWidth, float xPos, float yPos ) {
			API.DrawRect( xPos, yPos, cardWidth * 6.071f, cardWidth * 2.75f, 0, 0, 0, 130 );
		}

		/// <summary>
		///     Draws the dealer background.
		/// </summary>
		/// <param name="cardWidth">Width of the card.</param>
		public static void DrawDealerBackground( float cardWidth ) {
			API.DrawRect( 0.5f, 0.125f, cardWidth * 6.071f, cardWidth * 2.75f, 0, 0, 0, 130 );
		}

		/// <summary>
		///     Draws the wager window.
		/// </summary>
		/// <param name="game">The game.</param>
		public static async void DrawWagerWindow() {
			float backgroundYPos = 0.604f;
			float backgroundXPos = 0.1f;
			float xPos = backgroundXPos - 0.0475f;
			float startY = backgroundYPos - 0.18f;

			while( !Blackjack.HasPlayerLeftTable && Blackjack.CurrentGame.IsActive && Blackjack.CurrentGame.Dealer != null && !Blackjack.CurrentGame.Dealer.HasDeclaredActionOver ) {
				var clientPlayer = Blackjack.GetClientPlayer();
				if( Blackjack.ShowInfoHud &&
				    (Blackjack.IsClientDealer() || clientPlayer?.CurrentHands?.FirstOrDefault() != null) ) {
					//Draw window background
					float yPos = Blackjack.IsClientDealer() && Blackjack.CurrentGame.Dealer.CurrentHand.IsActive
						? backgroundYPos - 0.1f
						: backgroundYPos;
					float height = Blackjack.IsClientDealer() && Blackjack.CurrentGame.Dealer.CurrentHand.IsActive ? 0.15f : 0.35f;
					API.DrawRect( backgroundXPos, yPos, 0.1f, height, 0, 0, 0, 130 );
					string dealerScore = Blackjack.CurrentGame.Dealer.CurrentHand.IsActive ? Blackjack.CurrentGame.Dealer.CurrentHand.GetHandValueString() : "???";
					//If client is player,get their active hand.  Else we're dealer and need the current players active hand.
					var activeHand = Blackjack.IsClientPlayer()
						? clientPlayer?.GetActiveHand
						: Blackjack.CurrentGame.CurrentPlayer?.GetActiveHand;
					var textString = new StringBuilder( "DEALER'S SCORE \n" +
					                                    $"{dealerScore} \n" );
					if( Blackjack.IsClientPlayer() || activeHand != null ) {
						if( Blackjack.IsClientPlayer() )
							if( activeHand == null )
								activeHand = clientPlayer?.CurrentHands?.LastOrDefault();

						if( activeHand != null ) {
							string chipsOrWagerString = Blackjack.IsClientPlayer() ? "CHIPS" : "ROUND WAGER";

							textString.Append(
								"HAND SCORE \n" +
								$"{activeHand.GetHandValueString()} \n" +
								"HAND WAGER \n" +
								$"\n{chipsOrWagerString} \n" );

							var playerChips = Blackjack.IsClientPlayer()
								? clientPlayer?.Chips
								: Blackjack.CurrentGame.CurrentPlayer?.CurrentWager;
							string moneyString = $"${activeHand.Bet} \n" +
							                     $"\n${playerChips}";

							UI.DrawText( moneyString, new Vector2( xPos, startY + 0.2225f ),
								Color.FromArgb( 255, 14, 142, 61 ), 0.75f,
								Font.ChaletComprimeCologne );
						}
					}

					UI.DrawText( textString.ToString(), new Vector2( xPos, startY ),
						Color.FromArgb( 255, 255, 255, 255 ), 0.75f,
						Font.ChaletComprimeCologne );
				}

				await BaseScript.Delay( 0 );
			}
		}

		/// <summary>
		/// Draws the other players hands.
		/// </summary>
		public static async void DrawOtherPlayersHands() {
			while( !Blackjack.HasPlayerLeftTable && Blackjack.CurrentGame != null && Blackjack.CurrentGame.IsActive && !Blackjack.CurrentGame.HasDealerFinishedHand ) {
				if( Blackjack.ShowPlayersHandHud ) {
					var players = GetCleanedPlayerList();
					if( players.Any() ) {
						float yStart = 0.0625f;
						foreach( var player in players.OrderBy( p => p.Position ) ) {
							var hand = Blackjack.IsClientPlayer()
								? player.GetActiveHand ?? player.CurrentHands.LastOrDefault()
								: player.CurrentHands.LastOrDefault();
							if( hand != null ) {
								hand.DrawActiveHand( 0.85375f, yStart, 0.025f );
								UI.DrawText( $"{hand.GetHandValueString()} ${hand.Bet}",
									new Vector2( 0.85375f - 0.0135f, yStart + 0.025f ), Color.FromArgb( 255, 255, 255, 255 ),
									0.5f,
									Font.ChaletComprimeCologne );
								yStart = yStart + 0.1f;
							}
						}
					}
				}

				await BaseScript.Delay( 0 );
			}
		}

		/// <summary>
		///     Draws the hand.
		/// </summary>
		public static async void DrawDealerHand() {
			float width = 0.07f;

			while( !Blackjack.HasPlayerLeftTable && Blackjack.CurrentGame?.Dealer != null && Blackjack.CurrentGame.IsActive && !Blackjack.CurrentGame.Dealer.HasDeclaredActionOver ) {
				if( (Blackjack.CurrentGame?.Dealer?.CurrentHand != null && Blackjack.CurrentGame.Dealer.CurrentHand.IsActive) || Blackjack.IsClientDealer() && (Blackjack.CurrentGame.Dealer.IsDealerPeekingCard ) || Blackjack.CurrentGame.Dealer.ShouldOfferInsurance() && Blackjack.CurrentGame.Dealer.CurrentHand.IsBlackjack ) {
					Blackjack.CurrentGame.Dealer.CurrentHand.DrawActiveHand( 0.346f, 0.125f, width );
				}
				else {
					if( Blackjack.CurrentGame?.Dealer?.CurrentHand?.Cards.Count == 2 && Blackjack.CurrentGame?.Dealer?.CurrentHand?.Cards[0] != null && Blackjack.CurrentGame?.Dealer?.CurrentHand?.Cards[1] != null )
						Blackjack.CurrentGame.Dealer.CurrentHand.DrawInitialDealerHand( 0.346f, 0.125f, width );
				}

				await BaseScript.Delay( 0 );
			}
		}

		/// <summary>
		/// Draws the instructions.
		/// </summary>
		public static async void DrawDealerInstructions() {
			var instructionViewer = Blackjack.GetInstructionViewer();
			bool isDealersTurn = false;
			while( !Blackjack.HasPlayerLeftTable && Blackjack.CurrentGame?.Dealer != null && Blackjack.CurrentGame.IsActive && !Blackjack.CurrentGame.Dealer.HasDeclaredActionOver ) {
				if( Blackjack.ShowControls ) {
					if( Blackjack.CurrentGame.Dealer.CurrentHand.IsActive )
						if( !isDealersTurn ) {
							instructionViewer = Blackjack.GetInstructionViewer();
							isDealersTurn = true;
						}

					instructionViewer.ShowInstructions( InstructionViewer.OrientationEnum.Vertical );
				}

				await BaseScript.Delay( 0 );
			}
		}


		/// <summary>
		/// Draws the player names and chips.
		/// </summary>
		public static async void DrawPlayerNamesAndChips() {
			while( !Blackjack.HasPlayerLeftTable && Blackjack.CurrentGame.IsActive ) {
				if( Blackjack.ShowPlayersHandHud && Game.IsControlPressed( 0, Control.Sprint ) ) {
					var players = GetCleanedPlayerList();

					float yStart = 0.0625f;
					foreach( var player in players.OrderBy( p => p.Position ) ) {
						UI.DrawText( $"{player.Name} \nSeat {player.Position} ${player.Chips}",
							new Vector2( 0.85375f + 0.03f, yStart - 0.03f ), Color.FromArgb( 255, 255, 255, 255 ),
							0.5f, //only for other players and dealer
							Font.ChaletComprimeCologne );
						yStart = yStart + 0.1f;
					}
				}

				await BaseScript.Delay( 0 );
			}
		}

		/// <summary>
		/// Gets the cleaned player list.
		/// </summary>
		/// <returns></returns>
		private static List<BjPlayer> GetCleanedPlayerList() {
			if( Blackjack.CurrentGame.Players == null ) {
				Log.Info($"GetCleanedPlayerList: some shit is null" );
				return new List<BjPlayer>();
			}

			var activePlayer = Blackjack.GetClientPlayer() ?? Blackjack.CurrentGame.CurrentPlayer;

			var players = new List<BjPlayer>( Blackjack.CurrentGame.Players );
			if( activePlayer != null ) players.Remove( activePlayer );

			return players;
		}
	}
}