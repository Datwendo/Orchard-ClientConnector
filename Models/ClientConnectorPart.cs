using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement.Records;
using System.ComponentModel.DataAnnotations;
using Orchard.Environment.Extensions;
using System.Web.Mvc;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Mvc.Html;
using Orchard.Services;
using Orchard.ContentManagement.Aspects;
using Orchard.Core.Title.Models;


namespace Datwendo.ClientConnector.Models
{
    [OrchardFeature("Datwendo.ClientConnector")]
    public class ClientConnectorPart : ContentPart<ClientConnectorPartRecord>
    {
        public int CIndex
        {
            get { return Record.CIndex; }
            set { Record.CIndex = value; }
        }

        public string DataSent
        {
            get { return Record.DataSent; }
            set { Record.DataSent = value; }
        }          
    }
}
