using System;

namespace Eletiva.Simplex.Entities
{
    public sealed class Variable
    {
        public Variable(string name, int index)
        {
            Id = Guid.NewGuid();
            Name = name;
            Index = index;
        }

        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public int Index { get; private set; }
    }
}
