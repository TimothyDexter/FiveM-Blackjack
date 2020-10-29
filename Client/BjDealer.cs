using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Roleplay.Client.Classes.Environment.UI;
using Roleplay.SharedClasses;

namespace Roleplay.Client.Classes.Institutions.GamblingGames.Blackjack
{
	public class BjDealer
	{
		public BjDealer(  int netId, string name ) {
			NetId = netId;
			Name = name;
			CurrentHand = new BjHand();
		}

		public string Name { get; set; }
		public int NetId { get; set; }
		public BjHand CurrentHand { get; set; }
		public bool HasDeclaredActionOver { get; set; }
		public bool IsDealerPeekingCard { get; set; }

		/// <summary>
		///     Should offer insurance.
		/// </summary>
		/// <returns></returns>
		public bool ShouldOfferInsurance() {
			return CurrentHand?.Cards[1]?.Face == CardFace.Ace;
		}

		/// <summary>
		///     Deals the round starting cards.
		/// </summary>
		/// <param name="shoe">The shoe.</param>
		/// <param name="players">The players.</param>
		/// <returns></returns>
		public bool DealRoundStartingCards( BjShoe shoe, List<BjPlayer> players ) {
			var allPlayersDealt = true;
			CurrentHand = new BjHand();
			for( int i = 0; i < 2; i++ ) {
				foreach( var player in players ) {
					if( player.CurrentBet <= 0 || player.CurrentBet > player.Chips ||  player.CurrentBet < Blackjack.CurrentGame.MinimumBet ) {
						Log.Info($"DealRoundStartingCards: bet={player.CurrentBet},chips={player.Chips},minBet={Blackjack.CurrentGame.MinimumBet}" );
						allPlayersDealt = false;
						continue;
					}
					if( i == 0 ) {
						if( !player.DealNewRound( shoe ) ) {
							Log.Info($"Failed to deal player new round.");
							return false;
						}
					}
					else {
						var hand = player.CurrentHands.FirstOrDefault();
						if( !player.IsHitSuccess( hand, shoe ) ) {
							Log.Info( $"Failed to deal player 2nd card." );
							return false;
						}
					}
				}

				if( !CurrentHand.HitHand( shoe ) ) {
					Log.Info( $"Failed to hit dealer." );
					return false;
				}
			}

			return allPlayersDealt;
		}
	}
}