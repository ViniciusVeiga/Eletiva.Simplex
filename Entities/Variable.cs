using System;

namespace Eletiva.Simplex.Entities
{
    public sealed class Variable
    {
        public Variable(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
