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
            new EntityOne("001","000","", "\u3042\u3042\uFF01\uDBB8\uDF3A\u3055\u3063\u304D\u8D85\u5927\u304D\u306A\u30B4\u30AD\u30D6\u30EA\u304C\u90E8\u5C4B\u306B\u306F\u3057\u3063\u3066\u305F\u3088\u3001\u3069\u3046\u3057\u3087\u3046\uFF1F\uDBB8\uDF39\u79C1\u3053\u308C\u304C\u4E00\u756A\u6016\u3044\u3088\uFF01\u5BDD\u308C\u306A\u3044\u306A\u2026\u306A\u3093\u304B\u65B9\u6CD5\u304C\u3042\u308B\u304B\u306A\uDBB8\uDF3C\uDBB8\uDF41"),
            new EntityOne("001","001","", "\uDBB8\uDF3A\uDBB8\uDF39\u2026\uDBB8\uDF3C\uDBB8\uDF41"),
            new EntityOne("001","002","\uD867\uDE3D", "ホッケ"),
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

        void InsertOrMerge(CloudTableClient tables, string tableName)
        {
            var context = GetTableServiceContext(tables);

            foreach (var e in data)

            {
                context.AttachTo(tableName, e);
                context.UpdateObject(e);
            }

            var option = SaveChangesOptions.None; //  | SaveChangesOptions.Batch;
            context.SaveChangesWithRetries(option);

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

                    case "-im":
                        program.InsertOrMerge(tables, tableName);
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
