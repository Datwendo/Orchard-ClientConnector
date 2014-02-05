using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Orchard.ContentManagement.Records;
using Orchard.Environment.Extensions;
using Orchard.Localization;



namespace Datwendo.ClientConnector.Models
{
    [OrchardFeature("Datwendo.ClientConnector")]
    public class ClientConnectorPartRecord: ContentPartRecord 
    {
        public virtual int CIndex { get; set; }
        public virtual string DataSent { get; set; } 
    }

}
