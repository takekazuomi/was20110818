Windows Azure Table x-ms-version: 2011-08-18 サンプル
=========



upsert
==========

Windows Azure Storage Insert or Replace/Merge sample code.

//BUILD/ と同時に公開になったWindows Azure Storage Insert or Replace/Merge のサンプルコードです。  
動作を見るために簡単に書いているので、変なコメントが残っています。
SDK1.4/SDK1.5の両方で動きます。


サンプルの動かしかた
----------

例：
`./ConsoleApplication1.exe <option> <TabelName> "DefaultEndpointsProtocol=http;AccountName=...;AccountKey=...` 
 
option の内容
----------

 1. -i 初期データを作る
 2. -l データをdump。
 3. -ir Inset or Replaceを実行
 4. -im Inset or Mergeを実行

実例
----------

最初のデータを作って
`./ConsoleApplication1.exe -i EntityOne00 "DefaultEndpointsProtocol=http;AccountName=...;AccountKey=...` 

Insert or Mergeを実行
`./ConsoleApplication1.exe -im EntityOne00 "DefaultEndpointsProtocol=http;AccountName=...;AccountKey=...` 

内容確認は、VSなどでやったほうが便利です。



要点１：ヘッダーでバージョンを指定する
---------- 

SDK1.5に、同梱されているMicrosoft.WindowsAzure.StorageClient.dllを使っ
ても、x-ms-version: 2009-09-19 のままです。
新機能を使うには、x-ms-version: 2011-08-18 にする必要があります。
TableServiceContextの、SendingRequestを引っ掛けて、ヘッダを変更します。

`  
    var context = tables.GetDataServiceContext();

    context.SendingRequest += (sender, args) => {
      var request = args.Request as HttpWebRequest;
      request.Headers["x-ms-version"] = "2011-08-18";
    };
`	    
https://github.com/takekazuomi/was20110818/blob/master/upsert/ConsoleApplication1/Program.cs#L78



要点２：If-Matchを付けない
---------- 

upsert として提供されている機能は、従来のREST APIの口と同じです。
If-Matchが付いているかどうかで動きが変わり、Insert or Replace/Mergeになります。
If-Matchが付いていると、従来と同じ動きになります。

Insert or Replaceが、従来のUpdate、Insert or Mergeが、従来のMergeに相当します。

サンプルでは、新しいTableServiceContextを用意してAttachしています。

`
    var context = GetTableServiceContext(tables);

    (省略)

    foreach (var e in es)
    {
       context.AttachTo(tableName, e);
       context.UpdateObject(e);
    }

    var option = SaveChangesOptions.ReplaceOnUpdate; //  | SaveChangesOptions.Batch;
    context.SaveChangesWithRetries(option);

`

従来は、Etag無しのパターンが無く Attachの時にEtagの指定が必要でした。
無条件上書きの時は"*"を指定していましたが、今回は指定しないとうのがポイ
ントです。指定しないと、If-Match ヘッダーが生成されずにupsert になります。



要点３：ReplaceとMergeの切り替えはSaveChangesOption
---------- 

Mergeになるか、Replaceになるかは、SaveChangesOptionで決まります。これは、従来から同じルールです。

1. ReplaceOnUpdate で、Merge (NULLのプロパティが、サーバー上のデータが残ります)
2. None で、Replace (NULLのプロパティも書きこまれます）



その他
---------- 

1. 開発ストレージは、2011-08-18 をサポートしていない。
2. SDK1.5でもMicrosoft.WindowsAzure.StorageClient.dllは1.1.0.0のまま



参考リンク
---------- 

* http://blogs.msdn.com/b/windowsazure/archive/2011/09/14/just-announced-build-new-windows-azure-toolkit-for-windows-8-windows-azure-sdk-1-5-geo-replication-for-azure-storage-and-more.aspx

* [Insert Or Replace Entity](http://msdn.microsoft.com/en-us/library/hh452242.aspx)

* [Insert Or Merge Entity](http://msdn.microsoft.com/en-us/library/hh452241.aspx)





