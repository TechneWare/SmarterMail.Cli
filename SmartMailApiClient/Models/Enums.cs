using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace SmartMailApiClient.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ServiceType
    {
        Smtp,
        Imap,
        Pop,
        Delivery,
        Ldap,
        EmailHarvesting,
        GreyListLegacy,
        Xmpp,
        WebMail,
        ActiveSync,
        MapiEws,
        AuthUsers
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum IpBlockInfoSortType
    {
        count,
        ip,
        protocol,
        blockType,
        ruleDescription,
        ipLocation,
        timeLeft
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BlockType
    {
        Undefined,
        Harvesting,
        BruteForce,
        DOS,
        BouncesIndicateSpammer,
        InternalSpammer,
        PasswordRetrievalBruteForce,
        LoginBruteForceByEmail,
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum IPAccessDataType
    {
        WhiteList,
        BlackList
    }
}
