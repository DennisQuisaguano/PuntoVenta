using Bogus;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedWithBogus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var f = new Faker("es");
            var random = new Random();

            // 1. Generate and Insert 200 Products
            var products = new List<dynamic>();
            for (int i = 1; i <= 200; i++)
            {
                var p = new {
                    Id = i,
                    Name = f.Commerce.ProductName() + " " + f.Commerce.Color(),
                    UnitPrice = Math.Round(f.Random.Decimal(1, 100), 2),
                    Stock = f.Random.Int(10, 500)
                };
                products.Add(p);

                migrationBuilder.InsertData(
                    table: "Products",
                    columns: new[] { "Id", "Name", "UnitPrice", "Stock", "IsActive", "CreatedAt", "CreatedBy" },
                    values: new object[] { p.Id, p.Name, p.UnitPrice, p.Stock, true, DateTime.UtcNow.AddDays(-f.Random.Int(60, 100)), "BogusSeeder" }
                );
            }

            // 2. Generate and Insert 200 Customers
            var customers = new List<dynamic>();
            for (int i = 1; i <= 200; i++)
            {
                var address = f.Address.StreetAddress();
                if (address.Length > 50) address = address.Substring(0, 50);

                var c = new {
                    Id = i,
                    IDCard = f.Random.ReplaceNumbers("##########"),
                    Name = f.Name.FirstName(),
                    LastName = f.Name.LastName(),
                    Phone = f.Random.ReplaceNumbers("09########"),
                    Address = address,
                    Email = f.Internet.Email()
                };
                customers.Add(c);

                migrationBuilder.InsertData(
                    table: "Customers",
                    columns: new[] { "Id", "IDCard", "Name", "LastName", "Phone", "Address", "Email", "IsActive", "CreatedAt", "CreatedBy" },
                    values: new object[] { c.Id, c.IDCard, c.Name, c.LastName, c.Phone, c.Address, c.Email, true, DateTime.UtcNow.AddDays(-f.Random.Int(30, 60)), "BogusSeeder" }
                );
            }

            // 3. Generate Sales Data (Cabeceras)
            var salesToInsert = new List<dynamic>();
            var detailsToInsert = new List<dynamic>();
            int detailCounter = 1;

            for (int i = 1; i <= 200; i++)
            {
                var customer = customers[random.Next(customers.Count)];
                var saleDate = DateTime.UtcNow.AddDays(-f.Random.Int(1, 30));
                
                int numDetails = random.Next(1, 6);
                decimal subtotal = 0;

                for (int j = 0; j < numDetails; j++)
                {
                    var product = products[random.Next(products.Count)];
                    int amount = random.Next(1, 5);
                    decimal lineTotal = (decimal)product.UnitPrice * amount;
                    subtotal += lineTotal;

                    detailsToInsert.Add(new {
                        Id = detailCounter++,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Amount = amount,
                        UnitPrice = product.UnitPrice,
                        SaleId = i
                    });
                }

                decimal vat = Math.Round(subtotal * 0.15m, 2, MidpointRounding.AwayFromZero);
                decimal total = subtotal + vat;

                salesToInsert.Add(new {
                    Id = i,
                    Date = saleDate,
                    SubTotal = subtotal,
                    Vat = vat,
                    Total = total,
                    Customer = customer
                });
            }

            // 4. Insert Sales (MUST BE BEFORE DETAILS)
            foreach (var s in salesToInsert)
            {
                migrationBuilder.InsertData(
                    table: "Sales",
                    columns: new[] { "Id", "IssueDate", "InvoiceNumber", "CustomerId", "CustomerName", "CustomerLastName", "CustomerIDCard", "CustomerAddress", "CustomerPhone", "CustomerEmail", "SellerName", "SellerLastName", "SubTotal", "VatPercentage", "VatAmount", "Total", "Status", "IsActive", "CreatedAt", "CreatedBy" },
                    values: new object[] { 
                        s.Id, 
                        s.Date, 
                        $"FAC-{s.Id:D5}", 
                        s.Customer.Id, 
                        s.Customer.Name, 
                        s.Customer.LastName, 
                        s.Customer.IDCard, 
                        s.Customer.Address, 
                        s.Customer.Phone, 
                        s.Customer.Email,
                        "Admin", "Sistema",
                        s.SubTotal, 15.0m, s.Vat, s.Total, 1, true, s.Date, "BogusSeeder" 
                    }
                );
            }

            // 5. Insert SaleDetails
            foreach (var d in detailsToInsert)
            {
                migrationBuilder.InsertData(
                    table: "SaleDetails",
                    columns: new[] { "Id", "ProductId", "ProductName", "Amount", "UnitPrice", "SaleId" },
                    values: new object[] { d.Id, d.ProductId, d.ProductName, d.Amount, d.UnitPrice, d.SaleId }
                );
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Sales WHERE CreatedBy = 'BogusSeeder'");
            migrationBuilder.Sql("DELETE FROM Customers WHERE CreatedBy = 'BogusSeeder'");
            migrationBuilder.Sql("DELETE FROM Products WHERE CreatedBy = 'BogusSeeder'");
        }
    }
}

