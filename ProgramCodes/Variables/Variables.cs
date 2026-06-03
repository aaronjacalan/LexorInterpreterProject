using System.Collections.Generic;
using System.Linq;

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

        public string GetDisplayValue()
        {
            if (Value == null) return "";

            if (TypeHelper.IsArrayType(DataType))
            {
                var arr = (object[])Value;
                var elems = arr.Select(e => FormatElement(e, TypeHelper.ElementType(DataType)));
                return "[" + string.Join(", ", elems) + "]";
            }

            if (TypeHelper.IsStackType(DataType))
            {
                var stack = (Stack<object>)Value;
                var elems = stack.Select(e => FormatElement(e, TypeHelper.ElementType(DataType)));
                return "[" + string.Join(", ", elems) + "]";
            }

            return DataType switch
            {
                DataType.BOOL => (bool)Value ? "TRUE" : "FALSE", _ => Value.ToString()!
            };
        }

        private static string FormatElement(object? v, DataType t)
        {
            if (v == null) return "";
            return t switch
            {
                DataType.BOOL => (bool)v ? "TRUE" : "FALSE",
                _ => v.ToString()!
            };
        }
    }
}
