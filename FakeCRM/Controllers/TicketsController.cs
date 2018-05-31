using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeCRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;

namespace FakeCRM.Controllers
{
    public class TicketsController : Controller
    {
        private IConfiguration _configuration;

        public TicketsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var documentClient =
                new DocumentClient(
                    new Uri(_configuration["CosmosDbUri"]),
                    _configuration["CosmosDbKey"]
                );

            var db = documentClient
                .CreateDatabaseQuery()
                .ToList()
                .SingleOrDefault(xx => xx.Id == "fakecrm");

            var coll = documentClient
                .CreateDocumentCollectionQuery(db.SelfLink)
                .ToList()
                .SingleOrDefault(xx => xx.Id == "docs");

            var query = documentClient.CreateDocumentQuery<TicketIndexDto>(
                coll.SelfLink,
                "SELECT c.id, c.cameraName, c.imageUri FROM c"
                );

            var dto = query.ToList();

            return View(dto);
        }

        public IActionResult Details(string id)
        {
            var documentClient =
                new DocumentClient(
                    new Uri(_configuration["CosmosDbUri"]),
                    _configuration["CosmosDbKey"]
                );

            var db = documentClient
                .CreateDatabaseQuery()
                .ToList()
                .SingleOrDefault(xx => xx.Id == "fakecrm");

            var coll = documentClient
                .CreateDocumentCollectionQuery(db.SelfLink)
                .ToList()
                .SingleOrDefault(xx => xx.Id == "docs");

            var query = documentClient.CreateDocumentQuery<TicketDetailsDto>(
                coll.SelfLink,
                $"SELECT c.id, c.cameraName, c.imageUri FROM c WHERE c.id = '{id}'"
            );

            var dto = query.ToList();

            return View(dto[0]);
        }
    }
}