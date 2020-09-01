using System;

namespace Eletiva.Simplex.Entities
{
    public sealed class BaseValue : HasValue
    {
        public BaseValue(Base @base, decimal value)
        {
            Base = @base;
            Value = value;
        }

        public Base Base { get; private set; }
    }
}
