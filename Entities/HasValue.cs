using System;

namespace Eletiva.Simplex.Entities
{
    public abstract class HasValue
    {
        public decimal Value { get; protected set; }

        public void ChangeValue(decimal value) => Value = Math.Round(value, 2);
    }
}
