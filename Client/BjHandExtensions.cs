using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Roleplay.Client.Classes.Institutions.GamblingGames.Blackjack
{
	internal static class BjHandExtensions
	{
		/// <summary>
		///     Draws the active hand.
		/// </summary>
		/// <param name="hand">The hand.</param>
		/// <param name="xStartPos">The x start position.</param>
		/// <param name="yPos">The y position.</param>
		/// <param name="width">The width.</param>
		public static async void DrawActiveHand( this BjHand hand, float xStartPos, float yPos, float width ) {
			if( hand?.Cards == null ) return;
			await LoadCardTextures();
			BjDraw.DrawActiveHandBackground( width, xStartPos + width * 2.2f, yPos );

			float offSet = 0f;
			for( int index = 0; index < hand.Cards.Count; index++ ) {
				float offsetMultiplier = 1.1f;
				if( hand.Cards.Count > 5 && index < hand.Cards.Count - 1 ) offsetMultiplier = 0.2f;

				float offsetDelta = width * offsetMultiplier;
				offSet = offSet + offsetDelta;

				if( index == 0 ) offSet = 0f;

				BjDraw.DrawCard( hand.Cards[index], xStartPos + offSet, yPos, width );
			}
		}

		/// <summary>
		///     Draws the initial dealer hand.
		/// </summary>
		/// <param name="hand">The hand.</param>
		/// <param name="screenX">The screen x.</param>
		/// <param name="screenY">The screen y.</param>
		/// <param name="width">The width.</param>
		public static async void DrawInitialDealerHand( this BjHand hand, float screenX, float screenY, float width ) {
			if( hand?.Cards == null ) return;
			await LoadCardTextures();
			const float heightMultiplier = 2.5f;
			string dict = "standard_cards";

			BjDraw.DrawDealerBackground( width );
			API.DrawSprite( dict, "back", screenX, screenY, width,
				width * heightMultiplier, 0,
				255, 255, 255, 255 );

			BjDraw.DrawCard( hand.Cards[1], screenX + width * 1.1f, screenY, width );
		}

		/// <summary>
		///     Loads the card textures.
		/// </summary>
		/// <returns></returns>
		public static async Task LoadCardTextures() {
			string dict = "standard_cards";
			if( !API.HasStreamedTextureDictLoaded( dict ) ) {
				API.RequestStreamedTextureDict( dict, false );
				var timeout = DateTime.Now.AddSeconds( 60 );
				while( !API.HasStreamedTextureDictLoaded( dict ) && DateTime.Now.CompareTo( timeout ) < 0 )
					await BaseScript.Delay( 50 );
			}
		}
	}
}