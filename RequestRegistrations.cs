using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;        //zelf toegevoed
using System.Collections.Generic;   //zelf toegevoed
using tijdreeks_groep1.Models;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace MCT.Function
{
    public class RequestRegistrations
    {
        [FunctionName("RequestRegistrations")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/registrations")] HttpRequest req,
            ILogger log)
        {
            try
            {

                //var connectionstring = Environment.GetEnvironmentVariable("ConnectionString");
                var kvConnectionString = Environment.GetEnvironmentVariable("KeyVaultURI");
                var secretClient = new SecretClient(new Uri(kvConnectionString), new DefaultAzureCredential());
                var secret = secretClient.GetSecret("ConnectionString");
                var storageURI = secret.Value.Value;

                var credential = new DefaultAzureCredential();
                var token = credential.GetToken(new Azure.Core.TokenRequestContext(new[] { "https://database.windows.net/.default" }));

                var registrations = new List<RegistrationAdd>();

                // om SqlConnection te laten werken moet je de libraries boven inladen als in de csproj de extra lijn toevoegen van de package
                using (SqlConnection connection = new SqlConnection(storageURI))
                {
                    connection.AccessToken = token.Token;
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand())
                    {
                        command.Connection = connection;
                        command.CommandText = "SELECT RegistrationId,LastName, FirstName, Email, Zipcode, Age, IsFirstTimer FROM Registrations";

                        SqlDataReader reader = await command.ExecuteReaderAsync();
                        List<RegistrationRequest> result = new List<RegistrationRequest>();
                        while (await reader.ReadAsync())
                        {
                            RegistrationRequest registration = new RegistrationRequest();
                            registration.RegistrationId = Guid.Parse(reader["RegistrationId"].ToString());
                            registration.LastName = reader["LastName"].ToString();
                            registration.FirstName = reader["FirstName"].ToString();
                            registration.Email = reader["Email"].ToString();
                            registration.ZipCode = Convert.ToInt32(reader["ZipCode"]);
                            registration.Age = Convert.ToInt32(reader["Age"]);
                            registration.IsFirstTimer = Convert.ToBoolean(reader["IsFirstTimer"]);
                            result.Add(registration);

                        }
                        return new OkObjectResult(result);    //status code 200
                    }
                }
            }
            catch (System.Exception ex)
            {
                log.LogError(ex.Message);
                return new StatusCodeResult(500);
            }
        }
    }
}
