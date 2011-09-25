using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Services.Client;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.StorageClient.Protocol;
using Microsoft.WindowsAzure.StorageClient.Tasks;
using System.Net;
using Common.Logging;

namespace ConsoleApplication1
{
    class EntityOne : TableServiceEntity
    {
        public EntityOne()
        {
        }

        public EntityOne(string partitionKey, string rowKey, string name, string note)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
            this.Name = name;
            this.Note = note;
        }

        public string Name { get; set; } 
        public string Note { get; set; } 

    }

    partial class Program
    {
        static readonly ILog logger = LogManager.GetCurrentClassLogger();

        EntityOne[] data = new[] {
            new EntityOne("001","000","", "赤い"),
        };

        void DumpContext(string title, TableServiceContext context)
        {
            logger.Info(title);

            foreach (var e in context.Entities)
            {
                logger.Info(m => m("{0}\t{1}\t{2}\t{3}", e.Entity, e.ETag, e.Identity, e.ReadStreamUri));
            }
        }

        TableServiceContext GetTableServiceContext(CloudTableClient tables)
        {
            var context = tables.GetDataServiceContext();

            context.SendingRequest += (sender, args) => {
                var request = args.Request as HttpWebRequest;
                request.Headers["x-ms-version"] = "2011-08-18";
            };

            return context;
        }

        void AddObjects(CloudTableClient tables, string tableName)
        {
            var context = GetTableServiceContext(tables);

            foreach (var e in data)
            {
                context.AddObject(tableName, e);
            }

            var option = SaveChangesOptions.None; //  | SaveChangesOptions.Batch;
            context.SaveChangesWithRetries(option);

        }

        void List(CloudTableClient tables, string tableName)
        {
            var context = GetTableServiceContext(tables);
            var query = context.CreateQuery<EntityOne>(tableName);

            foreach(var e in query) {
                logger.Info(m => m("{0}\t{1}\t{2}\t{3}", e.PartitionKey, e.RowKey, e.Name, e.Note));
            }

        }

        /// <summary>
        /// UseDevelopmentStorage=true
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            try
            {

                var tableName = args[1] == "-" ? "" : args[1]; // empty の代わりに"-"を指定

                var connection = args[2];
                var tables = CloudStorageAccount.Parse(connection).CreateCloudTableClient();

                // 
                if (tables.CreateTableIfNotExist(tableName))
                {
                    logger.Debug("table created");
                }

                var program = new Program();
                switch (args[0])
                {
                    case "-lt":
                        program.ListTable(tables, tableName);
                        break;

                    case "-dt":
                        program.DropTable(tables, tableName);
                        break;

                    case "-l":
                        program.List(tables, tableName);
                        break;

                    case "-a":
                        program.AddObjects(tables, tableName);
                        break;
                }
            }
            catch (Exception e)
            {
                logger.Error("Error:", e);
            }
            
        }
    }
}
