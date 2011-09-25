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

    class EntityOneSub : TableServiceEntity
    {
        public EntityOneSub()
        {
        }

        public EntityOneSub(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public EntityOneSub(EntityOne e)
        {
            this.PartitionKey = e.PartitionKey;
            this.RowKey = e.RowKey;
        }
    }

    partial class Program
    {
        static readonly ILog logger = LogManager.GetCurrentClassLogger();

        EntityOne[] data = new[] {
            new EntityOne("001","000","Apple", "赤い"),
            new EntityOne("001","001","Orange","カリフォルニア産"),
            new EntityOne("001","002","Pineapple","沖縄産"),
            new EntityOne("001","003","Mango","フィリピン産"),
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

        void MakeInitialData(CloudTableClient tables, string tableName)
        {
            var context = GetTableServiceContext(tables);

            context.MergeOption = MergeOption.AppendOnly;
            foreach (var e in data)
            {
                context.AddObject(tableName, e);
            }

            DumpContext("Dump TableServiceContext Before Save", context);

            var option = SaveChangesOptions.None | SaveChangesOptions.Batch;
            context.SaveChangesWithRetries(option);

            DumpContext("Dump TableServiceContext After Save", context);
            
        }

        void InsertOrReplace(CloudTableClient tables, string tableName)
        {
            var context = GetTableServiceContext(tables);

            var es = new TableServiceEntity[] {
                new EntityOne("001","000","Apple II", "レインボーなやつ"), // replace
                new EntityOne("001","010","Apple IIc", "白かった"), // insert
                new EntityOneSub(data[2]), // replace
                new EntityOne("001","003", null, null), // replace
            };


            // Updateの時は、Etagが*あるいは読み込んだ時のEtagをのIf-Match ヘッダーを付ける。
            // Inset Or Replaceのときは、If-Matchヘッダー自体を付けない
            
            // したがって、Attachして、SendingRequest の時にIf-Matchヘッダーを削除する
            foreach (var e in es)
            {
                context.AttachTo(tableName, e);
                context.UpdateObject(e);
            }

            // 
            context.SendingRequest += (sender, args) => {
                var request = args.Request as HttpWebRequest;
                logger.Debug(m => m("Method:{0}, If-Match:{1}, Uri:{2}", request.Method, request.Headers["If-Match"], request.RequestUri));
            };

            // Insert Or Replace の場合は、MethodがPUT、SaveChangesOptions.ReplaceOnUpdateを指定するとPUTになる
            var option = SaveChangesOptions.ReplaceOnUpdate | SaveChangesOptions.Batch;
            context.SaveChangesWithRetries(option);

            DumpContext("Dump TableServiceContext", context);

        }

        void InsertOrMerge(CloudTableClient tables, string tableName)
        {
            var context = GetTableServiceContext(tables);

            var es = new TableServiceEntity[] {
                new EntityOne("001","000","Apple I", "実物見たことない"), // replace
                new EntityOne("001","011","Apple II compatible", "本田通商ですね"), // insert
                new EntityOneSub(data[2]), // merge
                new EntityOne("001","003", null, null), // merge
            };


            // Updateの時は、Etagが*あるいは読み込んだ時のEtagをのIf-Match ヘッダーを付ける。
            // Inset Or Replaceのときは、If-Matchヘッダー自体を付けない

            // したがって、Attachして、SendingRequest の時にIf-Matchヘッダーを削除する
            foreach (var e in es)
            {
                context.AttachTo(tableName, e);
                context.UpdateObject(e);
            }

            context.SendingRequest += (sender, args) =>
            {
                var request = args.Request as HttpWebRequest;
                logger.Debug(m => m("Method:{0}, If-Match:{1}, Uri:{2}", request.Method, request.Headers["If-Match"], request.RequestUri));

            };


            // Insert Or Replace の場合は、MethodがMERGE、SaveChangesOptions.Noneを指定するとMERGEになる
            var option = SaveChangesOptions.None | SaveChangesOptions.Batch;
            context.SaveChangesWithRetries(option);

            //
            DumpContext("Dump TableServiceContext", context);

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
                if (!String.IsNullOrEmpty(tableName) && tables.CreateTableIfNotExist(tableName))
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

                    case "-i":
                        program.MakeInitialData(tables, tableName);
                        break;

                    case "-l":
                        program.List(tables, tableName);
                        break;

                    case "-ir":
                        program.InsertOrReplace(tables, tableName);
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
