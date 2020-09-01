using System;

namespace Eletiva.Simplex.Entities
{
    public sealed class Base
    {
        public Base(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
        }

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        public void ChangeBase(Variable variable) => 
            Name = variable.Name;
    }
}
