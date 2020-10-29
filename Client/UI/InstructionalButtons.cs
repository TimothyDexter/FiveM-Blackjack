//  InstructionalButtons.cs
//  Author: Timothy Dexter
//  Release: 0.0.1
//  Date: 04/21/2019
//  
//   
//  Known Issues
//   
//  Please send any edits/improvements/bugs to this script back to the author. 
//   
//  Usage 
//  - Initialize with Dictionary<Control, InstructionModel>.  
//    - Slots need to be unique, lowest number appears bottom right corner
//    - Call ShowInstructions(OrientationEnum)
//	- If default constructor, call ShowInstructions with dictionary above and orientation.
//	- InstructionModel consists of slot # and Control Instruction string
//   
//  History:
//  Revision 0.0.1 2019/05/02 9:56 PM EDT TimothyDexter 
//  - Initial release
//   

using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Roleplay.SharedClasses;

namespace Roleplay.Client.Classes.Environment.UI
{
	public class InstructionViewer
	{
		public enum OrientationEnum
		{
			Horizontal = -1,
			Vertical = 1
		}

		private Dictionary<Control, InstructionModel> _controlInstructions;

		private bool _hasResourceLoaded;
		private Scaleform _scaleform;

		public InstructionViewer( Dictionary<Control, InstructionModel> controlInstructions ) {
			_controlInstructions = controlInstructions;
		}

		/// <summary>
		/// Shows the instructions.
		/// </summary>
		/// <param name="controlInstructions">The control instructions.</param>
		/// <param name="orientationEnum">The orientation enum.</param>
		public async void ShowInstructions( Dictionary<Control, InstructionModel> controlInstructions,
			OrientationEnum orientationEnum ) {
			if( !Session.HasJoinedRP || _controlInstructions == null || !_controlInstructions.Any() ) return;

			if( !_hasResourceLoaded ) {
				_scaleform = new Scaleform( "instructional_buttons" );
				var timeout = DateTime.Now.AddSeconds( 60 );
				while( !_scaleform.IsLoaded && DateTime.Now.CompareTo( timeout ) < 0 ) await BaseScript.Delay( 10 );
				InitializeInstructions( controlInstructions, orientationEnum );
				_hasResourceLoaded = true;
			}

			_scaleform?.Render2D();
		}

		/// <summary>
		/// Shows the instructions.
		/// </summary>
		/// <param name="orientationEnum">The orientation enum.</param>
		public void ShowInstructions( OrientationEnum orientationEnum ) {
			ShowInstructions( _controlInstructions, orientationEnum );
		}

		/// <summary>
		/// Initializes the instructions.
		/// </summary>
		/// <param name="controlInstructions">The control instructions.</param>
		/// <param name="orientationEnum">The orientation enum.</param>
		/// <returns></returns>
		private bool InitializeInstructions( Dictionary<Control, InstructionModel> controlInstructions,
			OrientationEnum orientationEnum ) {
			if( controlInstructions == null || !controlInstructions.Any() ) return false;

			_controlInstructions = VerifyUniqueSlots( controlInstructions );
			InitializeScaleform( orientationEnum );
			return true;
		}

		/// <summary>
		/// Verifies the unique slots.
		/// </summary>
		/// <param name="controlInstructions">The control instructions.</param>
		/// <returns></returns>
		private Dictionary<Control, InstructionModel> VerifyUniqueSlots(
			Dictionary<Control, InstructionModel> controlInstructions ) {
			bool doSlotsRepeat = controlInstructions.GroupBy( k => k.Value.Slot )
				.Where( g => g.Count() > 1 )
				.Select( y => y.Key )
				.ToList().Any();

			if( doSlotsRepeat ) {
				Log.Info( "Error: duplicate slots. One slot per instruction." );

				int slot = 0;
				foreach( var kvp in controlInstructions ) {
					kvp.Value.Slot = slot;
					slot = slot + 1;
				}
			}

			return controlInstructions;
		}

		/// <summary>
		/// Initializes the scaleform.
		/// </summary>
		/// <param name="orientationEnum">The orientation enum.</param>
		private void InitializeScaleform( OrientationEnum orientationEnum ) {
			_scaleform.CallFunction( "SET_DATA_SLOT_EMPTY" );
			//_scaleform.CallFunction( "TOGGLE_MOUSE_BUTTONS", 1 );
			foreach( var kvp in _controlInstructions ) {
				int control = (int)kvp.Key;
				var instruction = kvp.Value;
				_scaleform.CallFunction( "SET_DATA_SLOT", instruction.Slot,
					API.GetControlInstructionalButton( 2, control, 1 ), instruction.Message );
				_scaleform.CallFunction( "DRAW_INSTRUCTIONAL_BUTTONS", (int)orientationEnum );
			}
		}

		public class InstructionModel
		{
			public InstructionModel( string message, int slot ) {
				Message = message;
				Slot = slot;
			}

			public string Message { get; }
			public int Slot { get; set; }
		}
	}
}