using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ServiceWorker.Services
{
    public interface IHttpService
    {
        Task<HttpResponseMessage> PostAsync(string uri, HttpContent content);
    }

}
