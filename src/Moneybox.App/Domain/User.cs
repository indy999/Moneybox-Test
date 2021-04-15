namespace Moneybox.App.Domain
{
    using System;

    public class User
    {
        public User(Guid id, string name, string email)
        {
            Id = id;
            Name = name;
            Email = email;
        }

        public Guid Id { get; private set; }

        public string Name { get; private set; }

        public string Email { get; private set; }
    }
}
