using JetBrains.Annotations;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Datwendo.ClientConnector.Models;
using Orchard.Localization;
using Orchard.ContentManagement;
using Orchard.Environment.Extensions;

namespace Datwendo.ClientConnector.Handlers
{
    [UsedImplicitly]
    [OrchardFeature("Datwendo.ClientConnector")]
    public class ClientConnectorAdminSettingsPartHandler : ContentHandler
    {
        public ClientConnectorAdminSettingsPartHandler(IRepository<ClientConnectorAdminSettingsPartRecord> repository)
        {
            T = NullLocalizer.Instance;
            Filters.Add(new ActivatingFilter<ClientConnectorAdminSettingsPart>("Site"));
            Filters.Add(StorageFilter.For(repository));
        }


        public Localizer T { get; set; }

        protected override void GetItemMetadata(GetContentItemMetadataContext context)
        {
            if (context.ContentItem.ContentType != "Site")
                return;            
            base.GetItemMetadata(context);
            context.Metadata.EditorGroupInfo.Add(new GroupInfo(T("ClientConnector"))
            {
                Id = "ClientConnectorSettings",
                Position = "7"
            });
        }

    }
}
