using System;
using System.Collections.Generic;
using System.Linq;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Builders;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.ContentManagement.ViewModels;
using Orchard.Localization;
using Orchard.UI.Notify;

namespace Datwendo.ClientConnector.Settings {
    public class ClientConnectorSettingsHooks : ContentDefinitionEditorEventsBase {
        private readonly INotifier _notifier;

        public ClientConnectorSettingsHooks(INotifier notifier) {
            _notifier = notifier;
        }

        public Localizer T { get; set; }

        public override IEnumerable<TemplateViewModel> TypePartEditor(ContentTypePartDefinition definition) {
            if (definition.PartDefinition.Name != "ClientConnectorPart")
                yield break;

            var settings = definition.Settings.GetModel<ClientConnectorSettings>();

            yield return DefinitionTemplate(settings);
        }

        public override IEnumerable<TemplateViewModel> TypePartEditorUpdate(ContentTypePartDefinitionBuilder builder, IUpdateModel updateModel) {
            if (builder.Name != "ClientConnectorPart")
                yield break;

            var settings = new ClientConnectorSettings();

            if (updateModel.TryUpdateModel(settings, "ClientConnectorSettings", null, null)) {

                // update the settings builder
                settings.Build(builder);
            }

            yield return DefinitionTemplate(settings);
        }
    }
}
