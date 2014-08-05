using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Specialized;

/*THIS CLASS HAS BEEN MODIFIED FROM ITS ORIGINAL FORM BY A NON-OWNER*/

namespace Elmah
{
	public class NameValueCollectionSerializer : BsonBaseSerializer
	{
		private static readonly NameValueCollectionSerializer instance = new NameValueCollectionSerializer();

		public static NameValueCollectionSerializer Instance
		{
			get { return instance; }
		}

		public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
		{
			return Deserialize(bsonReader, nominalType, options);
		}

		public override object Deserialize(
			BsonReader bsonReader,
			Type nominalType,
			IBsonSerializationOptions options
			)
		{
			var bsonType = bsonReader.GetCurrentBsonType();
			if (bsonType == BsonType.Null)
			{
				bsonReader.ReadNull();
				return null;
			}

			var nvc = new NameValueCollection();

			bsonReader.ReadStartArray();
			while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
			{
				bsonReader.ReadStartArray();
				var key = (string)StringSerializer.Instance.Deserialize(bsonReader, typeof(string), options);
				var val = (string)StringSerializer.Instance.Deserialize(bsonReader, typeof(string), options);
				bsonReader.ReadEndArray();
				nvc.Add(key, val);
			}
			bsonReader.ReadEndArray();

			return nvc;
		}

		public override void Serialize(
			BsonWriter bsonWriter,
			Type nominalType,
			object value,
			IBsonSerializationOptions options
			)
		{
		    var nvc = value as NameValueCollection;
			if (nvc == null)
			{
				bsonWriter.WriteNull();
				return;
			}

			bsonWriter.WriteStartArray();
			foreach (var key in nvc.AllKeys)
			{
				foreach (var val in nvc.GetValues(key) ?? new string[0])
				{
					bsonWriter.WriteStartArray();
					StringSerializer.Instance.Serialize(bsonWriter, typeof(string), key, options);
				    if (key.ToLower().Contains("password"))
				    {
				        StringSerializer.Instance.Serialize(bsonWriter, typeof (string), string.Empty, options);
				    }
				    else
				    {
                        StringSerializer.Instance.Serialize(bsonWriter, typeof(string), val, options);
				    }
				    bsonWriter.WriteEndArray();
				}
			}
			bsonWriter.WriteEndArray();
		}
	}
}