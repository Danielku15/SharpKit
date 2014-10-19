using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace SharpKit.Compiler.Utils
{
    public class CompilerServiceClient
    {
        public CompilerServiceClient(string serviceUrl)
        {
            Client = new JsonClient(serviceUrl);
        }
        public JsonClient Client { get; set; }
        public CompileResponse Compile(CompileRequest args)
        {
            return Client.Invoke<CompileResponse>("Compile", args);
        }

        public void Test()
        {
            Client.Invoke<object>("Test", null);
        }
    }

    [DataContract]
    public class CompileRequest
    {
        [DataMember]
        public string CommandLineArgs { get; set; }
    }

    [DataContract]
    public class CompileResponse
    {
        [DataMember]
        public List<string> Output { get; set; }
        [DataMember]
        public int ExitCode { get; set; }
    }


    public class JsonClient
    {
        private readonly string _serviceUrl;

        public JsonClient(string serviceUrl)
        {
            _serviceUrl = serviceUrl;
        }

        public TResult Invoke<TResult>(string action, object arg)
        {
            var req = WebRequest.CreateHttp(_serviceUrl + action);
            req.Method = "POST";
            if (arg != null)
            {
                var ser = new DataContractJsonSerializer(arg.GetType());
                ser.WriteObject(req.GetRequestStream(), arg);
            }
            else
            {
                req.ContentLength = 0;
            }
            var res = (HttpWebResponse)req.GetResponse();
            if (res.StatusCode != HttpStatusCode.OK)
                throw new Exception(res.StatusCode + ", " + res.StatusDescription);
            if (typeof(TResult) != typeof(object))
            {
                var ser2 = new DataContractJsonSerializer(typeof(TResult));
                var x = (TResult)ser2.ReadObject(res.GetResponseStream());
                return x;
            }
            return default(TResult);
        }
    }
}
