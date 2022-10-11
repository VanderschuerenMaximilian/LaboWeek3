using System;

namespace tijdreeks_groep1.Models;

public class RegistrationRequest
{
    public Guid RegistrationId { get; set; }
    public string LastName { get; set; }
    public string FirstName { get; set; }
    public string Email { get; set; }
    public int ZipCode { get; set; }
    public int Age { get; set; }
    public bool IsFirstTimer { get; set; }



    //idk wat het is maar mss nodig
    // public static implicit operator RegistrationRequest(RegistrationRequest v)
    // {
    //     throw new NotImplementedException();
    // }
}