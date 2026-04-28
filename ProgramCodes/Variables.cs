namespace LexorInterpreter.ProgramCodes
{
    public enum DataType
    {
        INT, CHAR, BOOL, FLOAT
    }

    public class Variable
    {
        public string Name { get; set; }
        public DataType DataType { get; set; }
        public object? Value { get; set; }

        public Variable(string name, DataType dataType, object? value = null)
        {
            Name = name;
            DataType = dataType;
            Value = value;
        }

        // Returns the printable representation of this variable's value.
        // BOOL values always display as uppercase TRUE / FALSE per spec.
        public string GetDisplayValue()
        {
            if (Value == null) return "";

            return DataType switch
            {
                DataType.BOOL => (bool)Value ? "TRUE" : "FALSE", _ => Value.ToString()!
            };
        }
    }
}