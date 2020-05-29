/*	Contains the classes that handle the PLC registers
 * 
 * 
 * 
 * 
 * 
*/

using System;

public class Registers
{
	public intRegister[] allIntRegisters = new intRegister[100];
	public floatRegister[] allFloatRegisters = new floatRegister[100];

	public Registers()
	{
		// First, instantiate the ints and then floats so that IDs line up
		for (int i = 0; i < allIntRegisters.Length; i++)	
		{ allIntRegisters[i] = new intRegister(); 
			//allIntRegisters[i].toString(); 
		}
		for (int i = 0; i < allFloatRegisters.Length; i++)	
		{ allFloatRegisters[i] = new floatRegister(); 
			//allFloatRegisters[i].toString(); 
		}
	}
}

public class Register
{
	private static int globalID = 0;
	private int id = 0;
	private string label, url = "registers/";

	public int ID { get; set;  }
	public string Label { get; set; }
	public string URL { get; set; }

	public Register()
    {
		globalID++; // Registers start at 1
		this.id = globalID;
	}

	public string getURL()
    {
		return url + id;
    }

	public void toString()
    {
		Console.WriteLine("Global ID: " + globalID + " id: " + id + " Label: " + label + " url: " + url + " getURL: " + getURL());
    }
}

// Inherits from Register. intRegister allows for integer values
public sealed class intRegister : Register
{
	private int value;
    public int Value { get; set;  }
}

// Inherits from Register. floatRegister allows for float values

public sealed class floatRegister : Register
{
	private float value;
	public float Value { get; set; }
}