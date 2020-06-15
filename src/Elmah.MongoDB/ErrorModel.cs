using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elmah
{
    public class ErrorModel
    {
        public ObjectId _id { get; set; }

        public Error Error { get; set; }
    }
}
