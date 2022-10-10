using System;
namespace tijdreeks_groep1.Models;

public class RegistrationAdd
{
    public Guid  RegistrationId { get; set;}
    public string LastName { get; set; }
    public string FirstName { get; set; }
    public string Email { get; set; }
    public int ZipCode { get; set;}
    public int Age { get; set;}
    public bool IsFirstTimer { get; set;}
}