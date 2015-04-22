using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Threading.Tasks;
using System.Linq;
using MongoDB.Bson.Serialization.Serializers;
namespace Elmah
{
	public class MongoErrorLog : ErrorLog
	{
		private readonly string _connectionString;
		private readonly string _collectionName;
		private readonly long _maxDocuments;
		private readonly long _maxSize;
		//private MongoInsertOptions _mongoInsertOptions;

        private IMongoCollection<ErrorModel> _collection;

		private const int MaxAppNameLength = 60;
		private const int DefaultMaxDocuments = int.MaxValue;
		private const int DefaultMaxSize = 100 * 1024 * 1024;	// in bytes (100mb)

		private static readonly object Sync = new object();

		/// <summary>
		/// Initializes a new instance of the <see cref="MongoErrorLog"/> class
		/// using a dictionary of configured settings.
		/// </summary>
		public MongoErrorLog(IDictionary config)
		{
			if (config == null)
				throw new ArgumentNullException("config");

			var connectionString = GetConnectionString(config);

			//
			// If there is no connection string to use then throw an 
			// exception to abort construction.
			//

			if (connectionString.Length == 0)
				throw new ApplicationException("Connection string is missing for the SQL error log.");

			_connectionString = connectionString;

			//
			// Set the application name as this implementation provides
			// per-application isolation over a single store.
			//

			var appName = (string)config["applicationName"] ?? string.Empty;

			if (appName.Length > MaxAppNameLength)
			{
				throw new ApplicationException(string.Format(
					"Application name is too long. Maximum length allowed is {0} characters.",
					MaxAppNameLength.ToString("N0")));
			}

			ApplicationName = appName;

			_collectionName = appName.Length > 0 ? "Elmah-" + appName : "Elmah";
			_maxDocuments = GetCollectionLimit(config);
			_maxSize = GetCollectionSize(config);

			Initialize();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SqlErrorLog"/> class
		/// to use a specific connection string for connecting to the database.
		/// </summary>

		public MongoErrorLog(string connectionString)
		{
			if (connectionString == null)
				throw new ArgumentNullException("connectionString");

			if (connectionString.Length == 0)
				throw new ArgumentException(null, "connectionString");

			_connectionString = connectionString;

			Initialize();
		}

		static MongoErrorLog()
		{
            BsonSerializer.RegisterSerializer(typeof(NameValueCollection), new NameValueCollectionSerializer());
			BsonClassMap.RegisterClassMap<Error>(cm =>
			{
				cm.MapProperty(c => c.ApplicationName);
				cm.MapProperty(c => c.HostName).SetElementName("host");
				cm.MapProperty(c => c.Type).SetElementName("type");
				cm.MapProperty(c => c.Source).SetElementName("source");
				cm.MapProperty(c => c.Message).SetElementName("message");
				cm.MapProperty(c => c.Detail).SetElementName("detail");
				cm.MapProperty(c => c.User).SetElementName("user");
				cm.MapProperty(c => c.Time).SetElementName("time");
				cm.MapProperty(c => c.StatusCode).SetElementName("statusCode");
				cm.MapProperty(c => c.WebHostHtmlMessage).SetElementName("webHostHtmlMessage");
				cm.MapField("_serverVariables").SetElementName("serverVariables");
				cm.MapField("_queryString").SetElementName("queryString");
				cm.MapField("_form").SetElementName("form");
				cm.MapField("_cookies").SetElementName("cookies");
			});

		}

		private void Initialize()
		{
			lock (Sync)
			{
				var mongoUrl = MongoUrl.Create(_connectionString);
				var mongoClient = new MongoClient(mongoUrl);
				//var server = mongoClient.GetServer();

                var database = mongoClient.GetDatabase(mongoUrl.DatabaseName);
                //var filter = new Filter(new BsonDocument("name", _collectionName));
                var findThisOne = new ListCollectionsOptions();
                findThisOne.Filter = Builders<BsonDocument>.Filter.Eq("name", _collectionName);
                var cursor = database.ListCollectionsAsync(findThisOne).Result;
                var list = cursor.ToListAsync().GetAwaiter().GetResult();
                var allCollections = list.Select(c => c["name"].AsString).OrderBy(n => n).ToList();
                if (!allCollections.Contains(_collectionName))
				{
					var options = new CreateCollectionOptions();
                    options.Capped = true;
                    options.AutoIndexId = true;
                    options.MaxSize = _maxSize;

                    database.CreateCollectionAsync(_collectionName, options).GetAwaiter().GetResult();
				}

                _collection = database.GetCollection<ErrorModel>(_collectionName);
				//_mongoInsertOptions = new MongoInsertOptions { CheckElementNames = false };
			}
		}

		/// <summary>
		/// Gets the name of this error log implementation.
		/// </summary>
		public override string Name
		{
			get { return "MongoDB Error Log"; }
		}

		/// <summary>
		/// Gets the connection string used by the log to connect to the database.
		/// </summary>
		public virtual string ConnectionString
		{
			get { return _connectionString; }
		}

		/// <summary>
		/// Logs an error in log for the application.
		/// </summary>
		/// <param name="error"></param>
		/// <returns></returns>
		public override string Log(Error error)
		{
			if (error == null)
				throw new ArgumentNullException("error");

			error.ApplicationName = ApplicationName;
            var toStore = new ErrorModel();
            

			var id = ObjectId.GenerateNewId();
			toStore._id = id;
            toStore.Error = error;
			_collection.InsertOneAsync(toStore).GetAwaiter().GetResult();

			return id.ToString();
		}

		/// <summary>
		/// Retrieves a single application error from log given its
		/// identifier, or null if it does not exist.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public override ErrorLogEntry GetError(string id)
		{
			if (id == null) throw new ArgumentNullException("id");
			if (id.Length == 0) throw new ArgumentException(null, "id");

            var document = _collection.Find<ErrorModel>(x=> x._id == ObjectId.Parse(id)).SingleOrDefaultAsync().Result;

			if (document == null)
				return null;

			return new ErrorLogEntry(this, id, document.Error);
		}

		/// <summary>
		/// Retrieves a page of application errors from the log in
		/// descending order of logged time.
		/// </summary>
		/// <param name="pageIndex"></param>
		/// <param name="pageSize"></param>
		/// <param name="errorEntryList"></param>
		/// <returns></returns>
		public override int GetErrors(int pageIndex, int pageSize, IList errorEntryList)
		{
			if (pageIndex < 0) throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);
			if (pageSize < 0) throw new ArgumentOutOfRangeException("pageSize", pageSize, null);

			var documents = _collection.Find(new BsonDocument()).Sort("{$natural: -1}").Skip(pageIndex * pageSize)
                .Limit(pageSize).ToListAsync()
                .GetAwaiter().GetResult();//.SetSortOrder(SortBy.Descending("$natural")).SetSkip(pageIndex * pageSize).SetLimit(pageSize);

			foreach (var document in documents)
			{
                var error = document.Error;
			    error.Time = error.Time.ToLocalTime();
				errorEntryList.Add(new ErrorLogEntry(this, document._id.ToString(), error));
			}

			return Convert.ToInt32(_collection.CountAsync(new BsonDocument()).Result);
		}

		public static int GetCollectionLimit(IDictionary config)
		{
			int result;
			return int.TryParse((string)config["maxDocuments"], out result) ? result : DefaultMaxDocuments;
		}

		public static int GetCollectionSize(IDictionary config)
		{
			int result;
			return int.TryParse((string)config["maxSize"], out result) ? result : DefaultMaxSize;
		}

		public virtual string GetConnectionString(IDictionary config)
		{
			//
			// First look for a connection string name that can be 
			// subsequently indexed into the <connectionStrings> section of 
			// the configuration to get the actual connection string.
			//

			var connectionStringName = (string)config["connectionStringName"] ?? string.Empty;

			if (connectionStringName.Length > 0)
			{
				var settings = ConfigurationManager.ConnectionStrings[connectionStringName];

				if (settings == null)
					return string.Empty;

				return settings.ConnectionString ?? string.Empty;
			}


			//
			// Connection string name not found so see if a connection 
			// string was given directly.
			//

			var connectionString = (string)config["connectionString"] ?? string.Empty;

			if (connectionString.Length > 0)
				return connectionString;

			//
			// As a last resort, check for another setting called 
			// connectionStringAppKey. The specifies the key in 
			// <appSettings> that contains the actual connection string to 
			// be used.
			//

			var connectionStringAppKey = (string)config["connectionStringAppKey"] ?? string.Empty;

			return connectionStringAppKey.Length == 0 ? string.Empty : ConfigurationManager.AppSettings[connectionStringAppKey];
		}
	}
}
