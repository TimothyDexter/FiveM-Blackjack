using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;

namespace Roleplay.Client.Classes.Institutions.GamblingGames.Blackjack
{
	public class BjGame
	{
		public BjGame( BjDealer dealer, Vector3 location, List<BjPlayer> players, int minimumBet, int numberOfDecks,
			float blackjackBonus ) {
			Location = location;
			Dealer = dealer;
			Players = players;
			MinimumBet = minimumBet;
			NumberOfDecks = numberOfDecks;
			Shoe = new BjShoe( numberOfDecks );
			BlackjackBonus = blackjackBonus;
		}

		public BjGame( BjDealer dealer, Vector3 location, List<BjPlayer> players, BjShoe shoe, int minimumBet, int numberOfDecks,
			float blackjackBonus ) {
			Location = location;
			Dealer = dealer;
			Players = players;
			MinimumBet = minimumBet;
			NumberOfDecks = numberOfDecks;
			Shoe = shoe;
			BlackjackBonus = blackjackBonus;
		}

		public BjGame() {
		}

		public Vector3 Location { get; set; }
		public BjDealer Dealer { get; set; }
		public List<BjPlayer> Players { get; set; }
		public BjPlayer CurrentPlayer => Players.OrderBy(p => p.Position).FirstOrDefault( p => p.GetActiveHand != null );
		public BjShoe Shoe { get; set; }
		public float BlackjackBonus { get; set; }
		public int NumberOfDecks { get; set; }
		public int MinimumBet { get; set; }
		public bool IsActive { get; set; }
		public bool HasDealerFinishedHand => Dealer.HasDeclaredActionOver;

		/// <summary>
		///     Deals the new round.
		/// </summary>
		/// <returns></returns>
		public bool DealNewRound() {
			return Dealer.DealRoundStartingCards( Shoe, Players );
		}

		/// <summary>
		///     Does the player action remain.
		/// </summary>
		/// <returns></returns>
		public bool DoesPlayerActionRemain() {
			return Players.Any( p => p.CurrentHands.Any( h => !h.ActionFinished ) );
		}
	}
}