using System.Collections.Generic;

namespace Roleplay.Client.Classes.Institutions.GamblingGames.Blackjack.SharedModels
{
	public class BjHandShared
	{
		public BjHandShared() {
			Cards = new List<PlayingCard>();
			Bet = 0;
		}

		public BjHandShared( int bet ) {
			Cards = new List<PlayingCard>();
			Bet = bet;
		}

		public BjHandShared( int bet, PlayingCard card ) {
			Cards = new List<PlayingCard> {card};
			Bet = bet;
		}

		public BjHandShared( int bet, List<PlayingCard> cards ) {
			Cards = cards;
			Bet = bet;
		}

		public List<PlayingCard> Cards { get; set; }
		public int Bet { get; set; }
		public bool IsActive { get; set; }
		public bool IsDoubleDown { get; set; }
		public bool IsStanding { get; set; }
	}
}