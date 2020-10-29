using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Roleplay.SharedClasses;

namespace Roleplay.Client.Classes.Institutions.GamblingGames.Blackjack.SharedModels
{
	public static class BjSharedConverters
	{
		/// <summary>
		///     Converts to game shared.
		/// </summary>
		/// <param name="game">The game.</param>
		/// <returns></returns>
		public static BjGameShared ConvertToGameShared( this BjGame game ) {
			var sharedGame = new BjGameShared( game.Dealer.Name, game.Dealer.ConvertToDealerShared(),
				game.Location, game.Players.ConvertToPlayersShared(), game.MinimumBet, game.NumberOfDecks,
				game.BlackjackBonus ) {
				IsActive = game.IsActive
			};
			return sharedGame;
		}

		/// <summary>
		/// Converts to game.
		/// </summary>
		/// <param name="sharedGame">The shared game.</param>
		/// <param name="existingShoe">The existing shoe.</param>
		/// <returns></returns>
		public static BjGame ConvertToGame( this BjGameShared sharedGame, BjShoe existingShoe ) {
			BjGame updatedGame;
			if( existingShoe == null ) {
				updatedGame = new BjGame( sharedGame.Dealer.ConvertToDealer(), sharedGame.Location,
					sharedGame.Players.ConvertToPlayers(), sharedGame.MinimumBet, sharedGame.NumberOfDecks, sharedGame.BlackjackBonus ) {
					IsActive = sharedGame.IsActive
				};
			}
			else {
				updatedGame = new BjGame( sharedGame.Dealer.ConvertToDealer(), sharedGame.Location,
					sharedGame.Players.ConvertToPlayers(), existingShoe, sharedGame.MinimumBet, sharedGame.NumberOfDecks, sharedGame.BlackjackBonus ) {
					IsActive = sharedGame.IsActive
				};
			}

			return updatedGame;
		}

		/// <summary>
		///     Converts to dealer shared.
		/// </summary>
		/// <param name="dealer">The dealer.</param>
		/// <returns></returns>
		private static BjDealerShared ConvertToDealerShared( this BjDealer dealer ) {
			var sharedDealer = new BjDealerShared( dealer.NetId, dealer.Name ) {
				CurrentHand = new BjHandShared( dealer.CurrentHand.Bet,
					dealer.CurrentHand.Cards ) {
					IsActive = dealer.CurrentHand.IsActive
				},
				HasDeclaredActionOver = dealer.HasDeclaredActionOver
			};

			return sharedDealer;
		}

		/// <summary>
		///     Converts to dealer.
		/// </summary>
		/// <param name="sharedDealer">The shared dealer.</param>
		/// <returns></returns>
		private static BjDealer ConvertToDealer( this BjDealerShared sharedDealer ) {
			var updatedDealer = new BjDealer( sharedDealer.NetId, sharedDealer.Name ) {
				CurrentHand = new BjHand( sharedDealer.CurrentHand.Bet,
					sharedDealer.CurrentHand.Cards ) {IsActive = sharedDealer.CurrentHand.IsActive},
				HasDeclaredActionOver = sharedDealer.HasDeclaredActionOver
			};
			return updatedDealer;
		}

		/// <summary>
		///     Converts to players shared.
		/// </summary>
		/// <param name="players">The players.</param>
		/// <returns></returns>
		private static List<BjPlayerShared> ConvertToPlayersShared( this List<BjPlayer> players ) {
			var sharedPlayers = new List<BjPlayerShared>();
			foreach( var player in players ) {
				var sharedPlayer = new BjPlayerShared( player.NetId, player.Name, player.Position, player.Chips ) {
					CurrentBet = player.CurrentBet,
					HandInsurance = player.HandInsurance,
					CurrentHands = new List<BjHandShared>()
				};

				foreach( var hand in player.CurrentHands ) {
					var sharedHand = new BjHandShared( hand.Bet, hand.Cards ) {
						IsActive = hand.IsActive,
						IsStanding = hand.IsStanding,
						IsDoubleDown = hand.IsDoubleDown
					};
					
					sharedPlayer.CurrentHands.Add( sharedHand );
				}

				sharedPlayers.Add( sharedPlayer );
			}

			return sharedPlayers;
		}

		/// <summary>
		///     Converts to players.
		/// </summary>
		/// <param name="sharedPlayers">The shared players.</param>
		/// <returns></returns>
		public static List<BjPlayer> ConvertToPlayers( this List<BjPlayerShared> sharedPlayers ) {
			var players = new List<BjPlayer>();
			foreach( var player in sharedPlayers ) {
				var updatedPlayer = new BjPlayer( player.NetId, player.Name, player.Position, player.Chips ) {
					CurrentBet = player.CurrentBet,
					HandInsurance = player.HandInsurance,
					CurrentHands = new List<BjHand>()
				};

				foreach( var hand in player.CurrentHands ) {
					var playerHand = new BjHand( hand.Bet, hand.Cards ) {
						IsActive = hand.IsActive,
						IsStanding = hand.IsStanding,
						IsDoubleDown = hand.IsDoubleDown
					};
					playerHand.IsActive = hand.IsActive;
					updatedPlayer.CurrentHands.Add( playerHand );
				}

				players.Add( updatedPlayer );
			}

			return players;
		}

		public static bool TestConverters() {
			var dealer = new BjDealer( 1, "Dealer" );
			dealer.CurrentHand = new BjHand( 0 ) {
				Cards = new List<PlayingCard> {
					new PlayingCard( CardSuit.Clubs, CardFace.Ace ),
					new PlayingCard( CardSuit.Hearts, CardFace.Jack )
				}
			};

			var player = new BjPlayer( 2, "Player", 1, 5000 );
			player.CurrentHands = new List<BjHand> {
				new BjHand {
					Bet = 10,
					IsActive = true,
					Cards = new List<PlayingCard> {
						new PlayingCard( CardSuit.Spades, CardFace.Two ),
						new PlayingCard( CardSuit.Diamonds, CardFace.Three )
					}
				}
			};

			var game = new BjGame( dealer, Vector3.One, new List<BjPlayer> { player }, 10, 4, 1.5f );
			game.IsActive = true;

			var sharedDealer = new BjDealerShared( 1, "Dealer" );
			sharedDealer.CurrentHand = new BjHandShared( 0 ) {
				Cards = new List<PlayingCard> {
					new PlayingCard( CardSuit.Clubs, CardFace.Ace ),
					new PlayingCard( CardSuit.Hearts, CardFace.Jack )
				}
			};

			var sharedPlayer = new BjPlayerShared( 2, "Player", 1, 5000 );
			sharedPlayer.CurrentHands = new List<BjHandShared> {
				new BjHandShared {
					Bet = 10,
					IsActive = true,
					Cards = new List<PlayingCard> {
						new PlayingCard( CardSuit.Spades, CardFace.Two ),
						new PlayingCard( CardSuit.Diamonds, CardFace.Three )
					}
				}
			};

			var sharedGame = new BjGameShared( sharedDealer.Name, sharedDealer, Vector3.One,
				new List<BjPlayerShared> { sharedPlayer }, 10, 4, 1.5f );


			var gameConverted = game.ConvertToGameShared();
			var sharedConverted = sharedGame.ConvertToGame( null );

			bool test1 = BjSharedConverters.TestConvertSharedToGame( sharedConverted, game );
			bool test2 = BjSharedConverters.TestConvertGameToShared( gameConverted, sharedGame );

			Log.ToChat( $"TestConvertSharedToGame success={test1}" );
			Log.ToChat( $"TestConvertSharedToGame success={test2}" );

			return test1 && test2;
		}

		private static bool TestConvertGameToShared( BjGameShared actualGame, BjGameShared expectedGame ) {
			var success = true;

			var doesDealerNameMatch = actualGame.Dealer.Name == expectedGame.Dealer.Name;
			var doesDealerNetIdMatch = actualGame.Dealer.NetId == expectedGame.Dealer.NetId;
			var doesDealerHandMatch = actualGame.Dealer.CurrentHand.Cards[0].Face == expectedGame.Dealer.CurrentHand.Cards[0].Face && (actualGame.Dealer.CurrentHand.Cards[1].Face == expectedGame.Dealer.CurrentHand.Cards[1].Face);

			if( !doesDealerHandMatch || !doesDealerNameMatch || !doesDealerNetIdMatch ) {
				success = false;
				Log.Info( $"doesDealerNameMatch={doesDealerNameMatch}" );
				Log.Info( $"doesDealerNetIdMatch={doesDealerNetIdMatch}" );
				Log.Info( $"doesDealerHandMatch={doesDealerHandMatch}" );
			}
			else {
				Log.Info( $"Dealer matched." );
			}

			var actualPlayer = actualGame.Players.FirstOrDefault();
			var expectedPlayer = expectedGame.Players.FirstOrDefault();

			var doesPlayerNameMatch = actualPlayer.Name == expectedPlayer.Name;
			var doesPlayerNetIdMatch = actualPlayer.NetId == expectedPlayer.NetId;
			var doesPlayerBetMatch = actualPlayer.CurrentBet == expectedPlayer.CurrentBet;
			var doesPlayerChipsMatch = actualPlayer.Chips == expectedPlayer.Chips;
			var doesPlayerPositionMatch = actualPlayer.Position == expectedPlayer.Position;
			var doesPlayerHandBetMatch = actualPlayer.CurrentHands.FirstOrDefault().Bet == expectedPlayer.CurrentHands.FirstOrDefault().Bet;
			var doesPlayerHandIsActiveMatch = actualPlayer.CurrentHands.FirstOrDefault().IsActive == expectedPlayer.CurrentHands.FirstOrDefault().IsActive;
			var doesPlayerHandMatch = actualPlayer.CurrentHands.FirstOrDefault().Cards[0].Face == expectedPlayer.CurrentHands.FirstOrDefault().Cards[0].Face && actualPlayer.CurrentHands.FirstOrDefault().Cards[1].Face == expectedPlayer.CurrentHands.FirstOrDefault().Cards[1].Face;

			if( !doesPlayerNameMatch || !doesPlayerNetIdMatch || !doesPlayerBetMatch ||
				!doesPlayerChipsMatch || !doesPlayerPositionMatch || !doesPlayerHandBetMatch ||
				!doesPlayerHandIsActiveMatch || !doesPlayerHandMatch ) {
				success = false;

				Log.Info( $"doesPlayerNameMatch={doesPlayerNameMatch}" );
				Log.Info( $"doesPlayerNetIdMatch={doesPlayerNetIdMatch}" );
				Log.Info( $"doesPlayerBetMatch={doesPlayerBetMatch}" );
				Log.Info( $"doesPlayerChipsMatch={doesPlayerChipsMatch}" );
				Log.Info( $"doesPlayerPositionMatch={doesPlayerPositionMatch}" );
				Log.Info( $"doesPlayerHandBetMatch={doesPlayerHandBetMatch}" );
				Log.Info( $"doesPlayerHandIsActiveMatch={doesPlayerHandIsActiveMatch}" );
				Log.Info( $"doesPlayerHandMatch={doesPlayerHandMatch}" );
			}
			else {
				Log.Info( "Player matched." );
			}

			var doesDealerMatch = actualGame.Dealer.Name == expectedGame.Dealer.Name;
			var doesLocMatch = actualGame.Location == expectedGame.Location;
			var doPlayersMatch = actualGame.Players.Count == expectedGame.Players.Count;
			var decksMatch = actualGame.NumberOfDecks == expectedGame.NumberOfDecks;
			var bonusMatch = actualGame.BlackjackBonus == expectedGame.BlackjackBonus;

			if( !doesDealerMatch || !doesLocMatch || !doPlayersMatch || !decksMatch || !bonusMatch ) {
				success = false;

				Log.Info( $"doesDealerMatch={doesDealerMatch}" );
				Log.Info( $"doesLocMatch={doesLocMatch}" );
				Log.Info( $"doPlayersMatch={doPlayersMatch}" );
				Log.Info( $"decksMatch={decksMatch}" );
				Log.Info( $"bonusMatch={bonusMatch}" );
			}
			else {
				Log.Info( $"expectedGame matched." );
			}

			return success;
		}
		private static bool TestConvertSharedToGame( BjGame actualGame, BjGame expectedGame ) {
			var success = true;

			var doesDealerNameMatch = actualGame.Dealer.Name == expectedGame.Dealer.Name;
			var doesDealerNetIdMatch = actualGame.Dealer.NetId == expectedGame.Dealer.NetId;
			var doesDealerHandMatch = actualGame.Dealer.CurrentHand.Cards[0].Face == expectedGame.Dealer.CurrentHand.Cards[0].Face && (actualGame.Dealer.CurrentHand.Cards[1].Face == expectedGame.Dealer.CurrentHand.Cards[1].Face);

			if( !doesDealerHandMatch || !doesDealerNameMatch || !doesDealerNetIdMatch ) {
				success = false;
				Log.Info( $"doesDealerNameMatch={doesDealerNameMatch}" );
				Log.Info( $"doesDealerNetIdMatch={doesDealerNetIdMatch}" );
				Log.Info( $"doesDealerHandMatch={doesDealerHandMatch}" );
			}
			else {
				Log.Info( $"Dealer matched." );
			}

			var actualPlayer = actualGame.Players.FirstOrDefault();
			var expectedPlayer = expectedGame.Players.FirstOrDefault();

			var doesPlayerNameMatch = actualPlayer.Name == expectedPlayer.Name;
			var doesPlayerNetIdMatch = actualPlayer.NetId == expectedPlayer.NetId;
			var doesPlayerBetMatch = actualPlayer.CurrentBet == expectedPlayer.CurrentBet;
			var doesPlayerChipsMatch = actualPlayer.Chips == expectedPlayer.Chips;
			var doesPlayerPositionMatch = actualPlayer.Position == expectedPlayer.Position;
			var doesPlayerHandBetMatch = actualPlayer.CurrentHands.FirstOrDefault().Bet == expectedPlayer.CurrentHands.FirstOrDefault().Bet;
			var doesPlayerHandIsActiveMatch = actualPlayer.CurrentHands.FirstOrDefault().IsActive == expectedPlayer.CurrentHands.FirstOrDefault().IsActive;
			var doesPlayerHandMatch = actualPlayer.CurrentHands.FirstOrDefault().Cards[0].Face == expectedPlayer.CurrentHands.FirstOrDefault().Cards[0].Face && actualPlayer.CurrentHands.FirstOrDefault().Cards[1].Face == expectedPlayer.CurrentHands.FirstOrDefault().Cards[1].Face;

			if( !doesPlayerNameMatch || !doesPlayerNetIdMatch || !doesPlayerBetMatch ||
				!doesPlayerChipsMatch || !doesPlayerPositionMatch || !doesPlayerHandBetMatch ||
				!doesPlayerHandIsActiveMatch || !doesPlayerHandMatch ) {
				success = false;

				Log.Info( $"doesPlayerNameMatch={doesPlayerNameMatch}" );
				Log.Info( $"doesPlayerNetIdMatch={doesPlayerNetIdMatch}" );
				Log.Info( $"doesPlayerBetMatch={doesPlayerBetMatch}" );
				Log.Info( $"doesPlayerChipsMatch={doesPlayerChipsMatch}" );
				Log.Info( $"doesPlayerPositionMatch={doesPlayerPositionMatch}" );
				Log.Info( $"doesPlayerHandBetMatch={doesPlayerHandBetMatch}" );
				Log.Info( $"doesPlayerHandIsActiveMatch={doesPlayerHandIsActiveMatch}" );
				Log.Info( $"doesPlayerHandMatch={doesPlayerHandMatch}" );
			}
			else {
				Log.Info( "Player matched." );
			}

			var doesDealerMatch = actualGame.Dealer.Name == expectedGame.Dealer.Name;
			var doesLocMatch = actualGame.Location == expectedGame.Location;
			var doPlayersMatch = actualGame.Players.Count == expectedGame.Players.Count;
			var decksMatch = actualGame.NumberOfDecks == expectedGame.NumberOfDecks;
			var bonusMatch = actualGame.BlackjackBonus == expectedGame.BlackjackBonus;

			if( !doesDealerMatch || !doesLocMatch || !doPlayersMatch || !decksMatch || !bonusMatch ) {
				success = false;

				Log.Info( $"doesDealerMatch={doesDealerMatch}" );
				Log.Info( $"doesLocMatch={doesLocMatch}" );
				Log.Info( $"doPlayersMatch={doPlayersMatch}" );
				Log.Info( $"decksMatch={decksMatch}" );
				Log.Info( $"bonusMatch={bonusMatch}" );
			}
			else {
				Log.Info( $"expectedGame matched." );
			}

			return success;
		}
	}
}