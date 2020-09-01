using System;

namespace Eletiva.Simplex.Entities
{
    public sealed class BaseVariableValue : HasValue
    {
        public BaseVariableValue(Base @base, Variable variable, decimal value)
        {
            Base = @base;
            Variable = variable;
            Value = value;
        }

        public Base Base { get; private set; }
        public Variable Variable { get; private set; }
    }
}
