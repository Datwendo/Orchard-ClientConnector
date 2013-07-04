using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Datwendo.ClientConnector.Models
{
    public class FileDesc
    {
        public int CounterId { get; set; }
        public int CounterVal { get; set; }
        public string Filename { get; set; }
        public int ImgIdx { get; set; }
        public string Blobname { get; set; }
        public string ContentType { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
    }
}