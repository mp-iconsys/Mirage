using System;
using System.Collections.Generic;
using System.Text;
using static Globals;
using static Globals.DebugLevel;

namespace Mirage.plc
{
    public class RobotBlock
    {
        // ID of the block
        public int ID;

        // Offset of the blocks (total - this is the starting position of the read)
        public int Offset;

        public int Size;

        // Parameter list
        public List<IParameters> Param;

        public RobotBlock()
        {
            ID = 0;
            Offset = 0;
            Size = 0;
            Param = new List<IParameters>();
        }

        public RobotBlock(int ID, int Offset)
        {
            this.ID = ID;
            this.Offset = Offset;
            Size = 0;
            Param = new List<IParameters>();
        }

        public RobotBlock(int ID, int Offset, int Size)
        {
            this.ID = ID;
            this.Offset = Offset;
            this.Size = Size;
            Param = new List<IParameters>();
        }

        public short getTaskStatus()
        {
            return Param[4].getValue();
        }

        public short getPLCTaskStatus()
        {
            return Param[0].getValue();
        }

        public short getTaskNumber()
        {
            return Param[1].getValue();
        }

        public short getTaskParameter()
        {
            return Param[2].getValue();
        }

        public short getTaskSubparameter()
        {
            return Param[3].getValue();
        }

        public void setTaskStatus(int value)
        {
            Param[4].setValue(value);
        }
    }

    public class Parameters<T> where T : IParameters
    {
        public List<T> list = new List<T>();
    }

    public class ParametersPLC
    {
    }

    public class Parameter_INT : IParameters
    {
        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        private static readonly Type AREA = typeof(ParametersPLC);

        string Name { get; set; }
        int Size { get; set; }
        int Offset { get; set; }
        Int16 Value { get; set; }

        public Parameter_INT() { }

        public Parameter_INT(string name, int size, int offset)
        {
            Name = name;
            Size = size;
            Offset = offset;
            Value = 0;
        }

        public Parameter_INT(string name, int size, int offset, int value)
        {
            Name = name;
            Size = size;
            Offset = offset;
            Value = (short)value;
        }

        public void print()
        {
            logger(AREA, DEBUG, Name + ", Size: " + Size + " Offset: " + Offset + " Value: " + Value);
        }

        public int getSize()
        {
            return Size;
        }

        public int getOffset()
        {
            return Offset;
        }

        public void setValue(int Value)
        {
            this.Value = (short)Value;
        }

        public Int16 getValue()
        {
            return Value;
        }

        public float getFloat()
        {
            return (float)-100.0;
        }

        public string getName()
        {
            return Name;
        }

        public void simulateConsole()
        {
            Console.WriteLine("Enter " + Name + ": ");
            Value = Int16.Parse(Console.ReadLine());
        }

        public void setValueFloat(float Value)
        {
            this.Value = (short)Value;
        }

        public void setValueDouble(double Value)
        {
            this.Value = (short)Value;
        }
    }

    public class Parameter_FLOAT : IParameters
    {
        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        private static readonly Type AREA = typeof(ParametersPLC);

        string Name { get; set; }
        int Size { get; set; }
        int Offset { get; set; }
        float Value { get; set; }

        public Parameter_FLOAT() { }

        public Parameter_FLOAT(string name, int size, int offset)
        {
            Name = name;
            Size = size;
            Offset = offset;
            Value = 0;
        }

        public Parameter_FLOAT(string name, int size, int offset, float value)
        {
            Name = name;
            Size = size;
            Offset = offset;
            Value = value;
        }

        public void print()
        {
            logger(AREA, DEBUG, Name + ", Size: " + Size + " Offset: " + Offset + " Value: " + Value);
        }

        public int getSize()
        {
            return Size;
        }

        public int getOffset()
        {
            return Offset;
        }

        public void setValue(int Value)
        {
            this.Value = Value;
        }

        public void setValueFloat(float Value)
        {
            this.Value = Value;
        }

        public void setValueDouble(double Value)
        {
            this.Value = (float)Value;
        }

        public Int16 getValue()
        {
            return -100;
        }

        public float getFloat()
        {
            return Value;
        }

        public string getName()
        {
            return Name;
        }

        public void simulateConsole()
        {
            Console.WriteLine("Enter " + Name + ": ");
            Value = Int16.Parse(Console.ReadLine());
        }
    }

    public interface IParameters
    {
        void print();
        int getSize();
        int getOffset();
        void setValue(int Value);
        void setValueFloat(float Value);
        void setValueDouble(double Value);
        Int16 getValue();
        float getFloat();
        string getName();

        void simulateConsole();
    }
}
