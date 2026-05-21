namespace LexorInterpreter.ProgramCodes
{
    public class Variable
    {
        public string Name { get; set; }
        public DataType DataType { get; set; }
        public object? Value { get; set; }
        public bool IsInitialized { get; set; }

        public Variable(string name, DataType dataType, object? value = null, bool isInitialized = false)
        {
            Name = name;
            DataType = dataType;
            Value = value;
            IsInitialized = isInitialized;
        }

        // Returns the printable representation of this variable's value; BOOL is uppercase.
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
