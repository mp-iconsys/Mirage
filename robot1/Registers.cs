/*	Contains the classes that handle the PLC registers
 * 
 * 
 * 
 * 
 * 
*/

namespace x
{
	using System;

	public class Registers
	{
		public intRegister[] allIntRegisters = new intRegister[100];
		public floatRegister[] allFloatRegisters = new floatRegister[100];

		public Registers()
		{
			// First, instantiate the ints and then floats so that IDs line up
			for (int i = 0; i < 100; i++)	
			{ 
				allIntRegisters[i] = new intRegister(); 
				allIntRegisters[i].toString(); 
			}
			for (int i = 0; i < allFloatRegisters.Length; i++) { allFloatRegisters[i] = new floatRegister(); allFloatRegisters[i].toString(); }

		}
	}

	public class Register
	{
		// Used to iterate the ID field on instantiation in an array
		private static int globalID = 0;

		public int ID { get; set; }
		public string Label { get; set; }
		public string URL { get; set; }

		public Register()
		{
			globalID++; // Registers start at 1
			ID = globalID;
		}

		public string getURL()
		{
			return "registers/" + ID;
		}

		public void toString()
		{
			Console.WriteLine("Global ID: " + globalID + " id: " + ID + " Label: " + Label + " url: " + URL + " getURL: " + getURL());
		}
		
	}

	// Inherits from Register. intRegister allows for integer values
	public class intRegister : Register
	{
		//private int value;
		public int Value { get; set; }
	}

	// Inherits from Register. floatRegister allows for float values
	public class floatRegister : Register
	{
		//private float value;
		public float Value { get; set; }
	}
}