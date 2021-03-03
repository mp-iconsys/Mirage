using System;
using System.Collections.Generic;
using System.Text;
using static Globals;
using static Globals.DebugLevel;

namespace Mirage.plc
{
    /// <summary>
    /// Mainly a data container for the single robot structure in the Siemens PLC
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    public class SiemensRobot
    {
        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        private static readonly Type AREA = typeof(SiemensRobot);

        enum Param
        {
            //  Control Parameters
            Task_Status,
            Task_Number,
            Task_Paramater,
            Task_Subparameter,

            //  Status Parameters
            Mission_Status,
            Current_Group,
            Position_X,
            Position_Y,
            Position_Angle,
            Distance_Moved,
            Battery_Percentage
        }

        //  Total size of the block (in bytes)
        public int block_size;

        //  Control Parameters
        public int status;
        public int task;
        public int task_parameter;

        // Information Parameters
        public int mission_status;
        public int current_group;
        public int robot_status;
        public float position_x;
        public float position_y;
        public float position_angle;
        public float distance_moved;
        public float battery_percentage;

        public class Parameter_INT
        {
            string Name { get; set; } 
            int Size { get; set; }
            int Offset { get; set; }
            int Value { get; set; }

            public Parameter_INT() { }

            public Parameter_INT(string name, int size, int offset, int value) 
            {
                Name = name;
                Size = size;
                Offset = offset;
                Value = value;
            }

            public void print()
            {
                logger(AREA, INFO, Name + ", Size: " + Size + " Offset: " + Offset + " Value: " + Value);
            }
        }

        public class Parameter_FLOAT
        {
            string Name { get; set; }
            int Size { get; set; }
            int Offset { get; set; }
            float Value { get; set; }

            public Parameter_FLOAT() { }

            public Parameter_FLOAT(string name, int size, int offset, float value)
            {
                Name = name;
                Size = size;
                Offset = offset;
                Value = value;
            }

            public void print()
            {
                logger(AREA, INFO, Name + ", Size: " + Size + " Offset: " + Offset + " Value: " + Value);
            }
        }


        /*        private class parameter<TParam>
                {
                    int index;
                    int size; 
                    string name;
                    TParam value { get; }

                    private parameter(TParam param) => value = param;
                    private parameter(int index, int size, string name, TParam param) => (this.index, this.size, this.name, this.value) = (index, size, name, param);
                }*/

        // Abstract data type
        /*        public abstract class Parameter
                {
                }

                // extend abstract Metadata class
                public class Parameter<DataType> : Parameter where DataType : struct
                {
                    int size;
                    string name;
                    private DataType value;

                    private Parameter(DataType param) => value = param;
                    private Parameter(int size, string name, DataType param) => (this.size, this.name, this.value) = (size, name, param);
                }*/

    }
}
