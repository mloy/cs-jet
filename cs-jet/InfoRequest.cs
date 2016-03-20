using System.Runtime.Serialization;

[DataContract]
class InfoRequest
{
    [DataMember]
    string jsonrpc = "2.0";

    [DataMember]
    int id = 3;

    [DataMember]
    string method = "info";
}