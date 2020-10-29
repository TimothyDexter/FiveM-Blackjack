using System;
using Roleplay.Server.Classes.Institutions.GamblingGames.Blackjack;

namespace Roleplay.Server.Classes.Institutions.GamblingGames
{
	public class PlayingCard
	{
		public CardFace Face;
		public CardSuit Suit;

		public PlayingCard( CardSuit suit, CardFace face ) {
			Suit = suit;
			Face = face;
		}

		public int Value
		{
			get {
				int cardValue = Math.Min( (int)Face, 10 );
				return cardValue;
			}
		}

		public override string ToString() {
			string shorthandNotation = "";
			switch( Face ) {
			case CardFace.Ace:
				shorthandNotation += "A";
				break;
			case CardFace.Two:
				shorthandNotation += "2";
				break;
			case CardFace.Three:
				shorthandNotation += "3";
				break;
			case CardFace.Four:
				shorthandNotation += "4";
				break;
			case CardFace.Five:
				shorthandNotation += "5";
				break;
			case CardFace.Six:
				shorthandNotation += "6";
				break;
			case CardFace.Seven:
				shorthandNotation += "7";
				break;
			case CardFace.Eight:
				shorthandNotation += "8";
				break;
			case CardFace.Nine:
				shorthandNotation += "9";
				break;
			case CardFace.Ten:
				shorthandNotation += "10";
				break;
			case CardFace.Jack:
				shorthandNotation += "J";
				break;
			case CardFace.Queen:
				shorthandNotation += "Q";
				break;
			case CardFace.King:
				shorthandNotation += "K";
				break;
			}

			var suit = Enum.GetName( typeof(CardSuit), Suit )?.ToLower()[0];

			return shorthandNotation + suit;
		}
	}
}