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

namespace MCT.Function
{
    public class AddRegistrations
    {
        [FunctionName("AddRegistrations")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous,"post", Route = "v1/registrations")] HttpRequest req,
            ILogger log)
        {   
            try{
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<RegistrationAdd>(requestBody);
                data.RegistrationId = Guid.NewGuid();
                var connectionstring = Environment.GetEnvironmentVariable("ConnectionString");

                // om SqlConnection te laten werken moet je de libraries boven inladen als in de csproj de extra lijn toevoegen van de package
                using(SqlConnection connection = new SqlConnection(connectionstring)){
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand()){
                        command.Connection = connection;
                        command.CommandText = "INSERT INTO Registrations (RegistrationId, LastName, FirstName, Email, Zipcode, Age, IsFirstTimer) VALUES (@RegistrationId, @LastName, @FirstName, @Email, @Zipcode, @Age, @IsFirstTimer)";
                        command.Parameters.AddWithValue("@RegistrationId", data.RegistrationId);
                        command.Parameters.AddWithValue("@LastName", data.LastName);
                        command.Parameters.AddWithValue("@FirstName", data.FirstName);
                        command.Parameters.AddWithValue("@Email", data.Email);
                        command.Parameters.AddWithValue("@Zipcode", data.ZipCode);
                        command.Parameters.AddWithValue("@Age", data.Age);
                        command.Parameters.AddWithValue("@IsFirstTimer", data.IsFirstTimer);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                return new CreatedResult($"/v1/registrations/{data.RegistrationId}",data);
            }
            catch (System.Exception ex) {
                log.LogError(ex.Message);
                return new StatusCodeResult(500);
            }

        }
    }
}
