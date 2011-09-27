using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using Microsoft.WindowsAzure;

namespace wazsqry
{
    class StorageRequest
    {
        private Stream GetTableStream(CloudStorageAccount account, string tableName, string query)
        {
            var requestUri = new StringBuilder();
            requestUri.AppendFormat("{0}{1}()", account.TableEndpoint.ToString(), tableName);
            if (String.IsNullOrWhiteSpace(query))
            {
                requestUri.AppendFormat("?{0}", query);
            }

            // create Http Request
            var request = (HttpWebRequest)HttpWebRequest.Create(requestUri.ToString());

            // For requests using the $select query option, the request must be made using version 2011-08-18
            // or newer. In addition, the DataServiceVersion and MaxDataServiceVersion headers must be set
            // to 2.0.
            // http://msdn.microsoft.com/en-us/library/dd179421.aspx
            request.Headers.Add("x-ms-version", "2011-08-18");
            request.Headers.Add("MaxDataServiceVersion", "2.0");


            // signs request using the specified credentials under the Shared Key Lite authentication
            account.Credentials.SignRequestLite(request);

            var response = (HttpWebResponse)request.GetResponse();

            // get stream from response with response encoding
            var sr = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(response.CharacterSet));

            return sr.BaseStream;
        }

        
    }
}
