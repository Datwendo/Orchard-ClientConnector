using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Datwendo.ClientConnector.Services;
using Datwendo.ClientConnector.Models;
using Orchard.ContentManagement.Handlers;
using Orchard;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Data;

using Orchard.ContentManagement;
using Orchard.Core.Common.Models;


namespace Datwendo.ClientConnector.Handlers
{
    [OrchardFeature("Datwendo.ClientConnector")]
    public class ClientConnectorPartHandler : ContentHandler {

        private IClientConnectorService _clientConnectorService;

        public Localizer T { get; set; }

        public ClientConnectorPartHandler(IRepository<ClientConnectorPartRecord> repository,IClientConnectorService clientConnectorService)
        {            
            _clientConnectorService=clientConnectorService;
            T               = NullLocalizer.Instance;
            Filters.Add(StorageFilter.For(repository));
            OnCreating<ClientConnectorPart>(AssignIdentity);
        }

        protected void AssignIdentity(CreateContentContext context, ClientConnectorPart part)
        {
            int nval =0;
            if ( _clientConnectorService.ReadNext(part,out nval) )
                part.CIndex = nval;
            else throw new OrchardException (T("Error Reading Cloud Connector"));
        }
        protected override void GetItemMetadata(GetContentItemMetadataContext context)
        {
            var part = context.ContentItem.As<ClientConnectorPart>();

            if (part != null && part.CIndex != 0)
            {
                context.Metadata.Identity.Add("Identifier", part.CIndex.ToString());
            }
        }
    }
}