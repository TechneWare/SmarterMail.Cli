﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMailApiClient
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum HttpProtocol
    {
        http, 
        https
    }
    internal enum Verbs
    {
        GET,
        POST, 
        PUT, 
        DELETE
    }



}
