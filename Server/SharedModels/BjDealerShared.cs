namespace Roleplay.Server.Classes.Institutions.GamblingGames.Blackjack.SharedModels
{
	public class BjDealerShared
	{
		public BjDealerShared( int netId, string name) {
			NetId = netId;
			Name = name;
			CurrentHand = new BjHandShared();
		}

		public string Name { get; set; }
		public BjHandShared CurrentHand { get; set; }
		public int NetId { get; set; }
		public bool HasDeclaredActionOver { get; set; }

	}
}