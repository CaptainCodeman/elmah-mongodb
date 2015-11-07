using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Linq;
using System.Collections.Specialized;

namespace Elmah
{
    public class NameValueCollectionSerializer : SerializerBase<NameValueCollection>
    {
        BsonDocumentSerializer documentserializer = new BsonDocumentSerializer();
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, NameValueCollection value)
        {
            BsonDocument doc = new BsonDocument();
            if (value != null)
            {
                foreach (var key in value.AllKeys.Distinct())
                {
                    var val = value.GetValues(key).FirstOrDefault();
                    if(val != null)
                        doc.Add(key.Replace('.','|'), val);
                    
                }
            }

            documentserializer.Serialize(context, doc);
        }

        public override NameValueCollection Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            BsonDocument doc = documentserializer.Deserialize(context);
            var nvc = new NameValueCollection();
            foreach (var prop in doc)
            {
                nvc.Add(prop.Name.Replace('|','.'), prop.Value.ToString());
            }

            return nvc;
        }
    }
    //public class NameValueCollectionSerializer : BsonBaseSerializer
    //{
    //    private static readonly NameValueCollectionSerializer instance = new NameValueCollectionSerializer();

    //    public static NameValueCollectionSerializer Instance
    //    {
    //        get { return instance; }
    //    }

    //    public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
    //    {
    //        return Deserialize(bsonReader, nominalType, options);
    //    }

    //    public override object Deserialize(
    //        BsonReader bsonReader,
    //        Type nominalType,
    //        IBsonSerializationOptions options
    //        )
    //    {
    //        var bsonType = bsonReader.GetCurrentBsonType();
    //        if (bsonType == BsonType.Null)
    //        {
    //            bsonReader.ReadNull();
    //            return null;
    //        }

    //        var nvc = new NameValueCollection();

    //        bsonReader.ReadStartArray();
    //        while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
    //        {
    //            bsonReader.ReadStartArray();
    //            var key = (string)StringSerializer.Instance.Deserialize(bsonReader, typeof(string), options);
    //            var val = (string)StringSerializer.Instance.Deserialize(bsonReader, typeof(string), options);
    //            bsonReader.ReadEndArray();
    //            nvc.Add(key, val);
    //        }
    //        bsonReader.ReadEndArray();

    //        return nvc;
    //    }

    //    public override void Serialize(
    //        BsonWriter bsonWriter,
    //        Type nominalType,
    //        object value,
    //        IBsonSerializationOptions options
    //        )
    //    {
    //        if (value == null)
    //        {
    //            bsonWriter.WriteNull();
    //            return;
    //        }

    //        var nvc = (NameValueCollection)value;

    //        bsonWriter.WriteStartArray();
    //        foreach (var key in nvc.AllKeys)
    //        {
    //            foreach (var val in nvc.GetValues(key))
    //            {
    //                bsonWriter.WriteStartArray();
    //                StringSerializer.Instance.Serialize(bsonWriter, typeof(string), key, options);
    //                StringSerializer.Instance.Serialize(bsonWriter, typeof(string), val, options);
    //                bsonWriter.WriteEndArray();
    //            }
    //        }
    //        bsonWriter.WriteEndArray();
    //    }
    //}
}