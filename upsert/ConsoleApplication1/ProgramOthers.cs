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
    partial class Program
    {
        void List(CloudTableClient tables, string tableName)
        {
            var context = GetTableServiceContext(tables);
            var query = context.CreateQuery<EntityOne>(tableName);

            foreach(var e in query) {
                logger.Info(m => m("{0}\t{1}\t{2}\t{3}", e.PartitionKey, e.RowKey, e.Name, e.Note));
            }

//
            DumpContext("Dump TableServiceContext", context);

        }


        void ListTable(CloudTableClient tables, string name)
        {
            logger.Info("Drop Table");

            foreach (var t in tables.ListTables(name))
            {
                logger.Info(t);
            }
        }

        void DropTable(CloudTableClient tables, string name)
        {
            logger.Info("List Table");

           foreach (var t in tables.ListTables(name))
            {
                if (tables.DeleteTableIfExist(t))
                {
                    logger.Info(m=>m("{0} deleted", t));
                }
                else
                {
                     logger.Info(m=>m("{0} not deleted", t));
               }
            }
        }
    }
}
