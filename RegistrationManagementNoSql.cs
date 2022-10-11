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
using Azure.Data.Tables;

namespace MCT.Function
{
    public static class RegistrationManagementNoSql
    {
        [FunctionName("GetRegistrationsV2")]
        public static async Task<IActionResult> GetRegistrationsV2(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v2/registrations")] HttpRequest req,
            ILogger log)
        {
            try
            {
                


                return new OkObjectResult("");
            }
            catch (System.Exception ex)
            {
                log.LogError(ex.Message);
                return new StatusCodeResult(500);
            }
        }


        [FunctionName("PostRegistrationsV2")]
        public  static async Task<IActionResult> PostRegistrationsV2(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v2/registrations")] HttpRequest req,
            ILogger log)
        {
            try
            {
                var json = await new StreamReader(req.Body).ReadToEndAsync();
                RegistrationAdd reg = JsonConvert.DeserializeObject<RegistrationAdd>(json);
                reg.RegistrationId = Guid.NewGuid();

                string storageAccountName = Environment.GetEnvironmentVariable("StorageAccountName");
                string storageAccountKey = Environment.GetEnvironmentVariable("StorageAccountKey");
                string storageURI = Environment.GetEnvironmentVariable("RegistrationTable");
                
                string partionKey = "zipcode";
                string rowKey = reg.RegistrationId.ToString();

                var tableClient = new TableClient(new Uri(storageURI),"registrations",new TableSharedKeyCredential(storageAccountName,storageAccountKey));

                await tableClient.CreateIfNotExistsAsync();

                var entity = new TableEntity(partionKey, rowKey){
                    {"FirstName", reg.FirstName},
                    {"LastName", reg.LastName},
                    {"Email", reg.Email},
                    {"ZipCode", reg.ZipCode},
                    {"Age", reg.Age},
                    {"IsFirstTimer", reg.IsFirstTimer}
                };
                
                tableClient.AddEntity(entity);

                return new CreatedResult($"/v2/registrations/{reg.RegistrationId}", reg);
            }
            catch (System.Exception ex)
            {
                log.LogError(ex.Message);
                return new StatusCodeResult(500);
            }

        }
    }
}
