using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace comp4870project.Model;

public class User : IdentityUser
{
    public User() : base() { }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsSubscribed { get; set; }
}