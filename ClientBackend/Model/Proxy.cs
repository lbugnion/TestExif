using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ClientBackend.Model
{
    public static class Proxy
    {
        private static HttpClient _http;

        public static HttpClient Http
        {
            get
            {
                if (_http == null)
                {
                    _http = new HttpClient();
                }

                return _http;
            }
        }
    }
}
