using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Datwendo.ClientConnector.Models
{
    #region  Base Requests

    public class CCtrRequest
    {
        public string Ky { get; set; }
    }

    public class CCtrResponse
    {
        public int Cd { get; set; }
        public int Vl { get; set; }
    }

    public class CCtrRequest2 : CCtrRequest
    {
        public int Dl { get; set; }
    }

    // With publisher for Put
    public class PubCCtrRequest : CCtrRequest
    {
        public int Pb { get; set; }
    }

    public class CCtrResponse2
    {
        public int Cd { get; set; }
        public int Pr { get; set; }
        public string Ky { get; set; }
    }

    #endregion // Base Requests

    #region String

    public class CCtrRequestSt
    {
        public int Ix { get; set; }
        public string Ky { get; set; }

    }

    public class CCtrResponseSt
    {
        public int Cd { get; set; }
        public string St { get; set; }
    }

    public class StringStorRequest : PubCCtrRequest
    {
        public string St { get; set; }
    }

    #endregion // String

    #region Blob

    public class CCtrResponseBlob
    {
        public int Cd { get; set; }
        public IEnumerable<FileDesc> Lst { get; set; }
    }

    public class CCtrResponseBlob2
    {
        public int Cd { get; set; }
        public FileDesc Fi { get; set; }
    }

    public class BinStorRequest : CCtrRequest
    {
        public Byte[] St { get; set; }
    }

    #endregion // Blob

    #region Trace

    public class CCtrRequestTr : CCtrRequest
    {
        public bool St { get; set; }
    }

    public class CCtrResponseTr
    {
        public int Cd { get; set; }
        public int Pr { get; set; }
        public bool St { get; set; }
        public string Ky { get; set; }
    }

    public class TraceStorRequest : CCtrRequest
    {
        public string St { get; set; }
    }

    public class TraceStorResponse 
    {
        public int Cd { get; set; }
        public long Dt { get; set; }
        public long Lv { get; set; }
        public string St { get; set; }
    }

    #endregion //Trace
}