using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Datwendo.ClientConnector.Models;
using Datwendo.ClientConnector.Settings;
using Datwendo.ClientConnector.ViewModels;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using Orchard.Environment.Extensions;
using Orchard.Localization;


namespace Datwendo.ClientConnector.Drivers
{
    [OrchardFeature("Datwendo.ClientConnector")]
    public class ClientConnectorPartDriver : ContentPartDriver<ClientConnectorPart>
    {
        public ClientConnectorPartDriver()
        {
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        
       protected override string Prefix
        {
            get { return "ClientConnector"; }
        }


        protected override DriverResult Display(
            ClientConnectorPart part, string displayType, dynamic shapeHelper) {
                return ContentShape("Parts_ClientConnector", () => shapeHelper.Parts_ClientConnector(Content: part));
        }

        //GET
        protected override DriverResult Editor(ClientConnectorPart part, dynamic shapeHelper) {
            return Editor(part, null, shapeHelper);
            /*
                return ContentShape("Parts_ClientConnector_Edit",
                () => shapeHelper.EditorTemplate(
                    TemplateName: "Parts/ClientConnector",
                    Model: part,
                    Prefix: Prefix));*/
        }
        
        //POST
        protected override DriverResult Editor(ClientConnectorPart part, IUpdateModel updater, dynamic shapeHelper) {

            var settings        = part.TypePartDefinition.Settings.GetModel<ClientConnectorSettings>();

            var viewModel       = new ClientConnectorPartEditViewModel
            {
                Settings = settings
            };

            if (updater != null && updater.TryUpdateModel(viewModel, Prefix, null, null))
            {
                if (settings.ConnectorId == 0 )
                {
                    updater.AddModelError("ConnectorId", T("You have not defined the Connector Id for this Content Type."));
                }

                if ( string.IsNullOrEmpty(settings.SecretKey))
                {
                    updater.AddModelError("SecretKey", T("You have not defined the Secret Key for this Content Type."));
                }

            }

            return ContentShape("Parts_ClientConnector_Edit",
                () => shapeHelper.EditorTemplate(TemplateName: "Parts.ClientConnector.Edit", Model: viewModel, Prefix: Prefix));
        }



        protected override void Importing(ClientConnectorPart part, ImportContentContext context)
        {
            var cIndex = context.Attribute(part.PartDefinition.Name, "CIndex");
            
            int pri = 0;
            if (int.TryParse(cIndex, out pri))
                part.CIndex = pri;
        }

        protected override void Exporting(ClientConnectorPart part, ExportContentContext context) {
            context.Element(part.PartDefinition.Name).SetAttributeValue("CIndex", part.CIndex);
        }
    }
}