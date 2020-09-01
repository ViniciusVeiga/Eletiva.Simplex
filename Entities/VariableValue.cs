using System;

namespace Eletiva.Simplex.Entities
{
    public sealed class VariableValue : HasValue
    {
        public VariableValue(Variable variable, decimal value)
        {
            Variable = variable;
            Value = value;
        }

        public Variable Variable { get; private set; }
    }
}
