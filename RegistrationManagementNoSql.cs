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
using Azure;
using System.Text;
using System.Data;
using CsvHelper;
using System.Globalization;
using Azure.Storage.Blobs;

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
                string storageAccountName = Environment.GetEnvironmentVariable("StorageAccountName");
                string storageAccountKey = Environment.GetEnvironmentVariable("StorageAccountKey");
                string storageURI = Environment.GetEnvironmentVariable("RegistrationTable");

                var tableClient = new TableClient(new Uri(storageURI), "registrations", new TableSharedKeyCredential(storageAccountName, storageAccountKey));

                string partitionKey = "zipcode";
                Pageable<TableEntity> queryResultsFilter = tableClient.Query<TableEntity>(filter: $"PartitionKey eq '{partitionKey}'");

                var result = new List<RegistrationRequest>();
                // Iterate the <see cref="Pageable"> to access all queried entities.
                foreach (TableEntity qEntity in queryResultsFilter)
                {
                    var registration = new RegistrationRequest
                    {
                        // RegistrationId = Guid.Parse(qEntity.RowKey),
                        RegistrationId = Guid.Parse(qEntity.RowKey.ToString()),
                        FirstName = qEntity["FirstName"].ToString(),
                        LastName = qEntity["LastName"].ToString(),
                        Email = qEntity["Email"].ToString(),
                        ZipCode = int.Parse(qEntity["ZipCode"].ToString()),
                        Age = int.Parse(qEntity["Age"].ToString()),
                        IsFirstTimer = bool.Parse(qEntity["IsFirstTimer"].ToString())
                    };
                    result.Add(registration);
                }

                return new OkObjectResult(result);
            }
            catch (System.Exception ex)
            {
                log.LogError(ex.Message);
                return new StatusCodeResult(500);
            }
        }

        //------------------------------------------------------------------------------------------------//

        [FunctionName("PostRegistrationsV2")]
        public static async Task<IActionResult> PostRegistrationsV2(
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

                var tableClient = new TableClient(new Uri(storageURI), "registrations", new TableSharedKeyCredential(storageAccountName, storageAccountKey));

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

        //----------------------------------------------------------------------------------------------------//

        [FunctionName("Export")]
        public static async Task<IActionResult> Export(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v2/registrations/export")] HttpRequest req,
            ILogger log)
        {
            try
            {
                string storageAccountName = Environment.GetEnvironmentVariable("StorageAccountName");
                string storageAccountKey = Environment.GetEnvironmentVariable("StorageAccountKey");
                string storageURI = Environment.GetEnvironmentVariable("RegistrationTable");

                var tableClient = new TableClient(new Uri(storageURI), "registrations", new TableSharedKeyCredential(storageAccountName, storageAccountKey));

                string partitionKey = "zipcode";
                Pageable<TableEntity> queryResultsFilter = tableClient.Query<TableEntity>(filter: $"PartitionKey eq '{partitionKey}'");

                var result = new List<RegistrationRequest>();
                // Iterate the <see cref="Pageable"> to access all queried entities.
                foreach (TableEntity qEntity in queryResultsFilter)
                {
                    var registration = new RegistrationRequest
                    {
                        // RegistrationId = Guid.Parse(qEntity.RowKey),
                        RegistrationId = Guid.Parse(qEntity.RowKey.ToString()),
                        FirstName = qEntity["FirstName"].ToString(),
                        LastName = qEntity["LastName"].ToString(),
                        Email = qEntity["Email"].ToString(),
                        ZipCode = int.Parse(qEntity["ZipCode"].ToString()),
                        Age = int.Parse(qEntity["Age"].ToString()),
                        IsFirstTimer = bool.Parse(qEntity["IsFirstTimer"].ToString())
                    };
                    result.Add(registration);
                }

                string localFileName = $"registrations-{DateTime.Now.ToString("yyyyMMddHHmmss")}.csv";
                //we gaan de file tijdelijk opslaan locaal
                string localFilePath = $"{Path.GetTempPath()}{localFileName}";

                using(var writer = new StreamWriter(localFilePath)){
                    using(var csv = new CsvWriter(writer, CultureInfo.InvariantCulture)){
                        csv.WriteRecords(result);
                    }
                }

                string blobConnectionString = Environment.GetEnvironmentVariable("BlobConnectionString");

                BlobServiceClient blobServiceClient = new BlobServiceClient(blobConnectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("csv");
                BlobClient blobClient = containerClient.GetBlobClient(localFileName);
                await blobClient.UploadAsync(localFilePath);
                
                File.Delete(localFilePath);

                return new OkObjectResult("");
            }
            catch (System.Exception ex)
            {
                log.LogError(ex.Message);
                return new StatusCodeResult(500);
            }
        }
    }
}