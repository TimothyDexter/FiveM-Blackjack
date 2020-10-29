using System.Collections.Generic;
using CitizenFX.Core;

namespace Roleplay.Server.Classes.Institutions.GamblingGames.Blackjack.SharedModels
{
	public class BjGameShared
	{
		public BjGameShared( string name, BjDealerShared dealer, Vector3 location, List<BjPlayerShared> players,
			int minimumBet, int numberOfDecks, float blackjackBonus ) {
			Name = name;
			Dealer = dealer;
			Location = location;
			Players = players;
			MinimumBet = minimumBet;
			NumberOfDecks = numberOfDecks;
			BlackjackBonus = blackjackBonus;
		}

		public string Name { get; set; }
		public BjDealerShared Dealer { get; set; }
		public List<BjPlayerShared> Players { get; set; }
		public Vector3 Location { get; set; }
		public float BlackjackBonus { get; set; }
		public int NumberOfDecks { get; set; }
		public int MinimumBet { get; set; }
		public bool IsActive { get; set; }
	}
}