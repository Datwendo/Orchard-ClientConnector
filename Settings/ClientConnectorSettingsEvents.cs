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
using Orchard.Core.Contents.Settings;
using Orchard.Utility.Extensions;
using Orchard.ContentTypes.Extensions;
using Datwendo.ContentHelpers.Services;

namespace Datwendo.ClientConnector.Settings {
    public class ClientConnectorSettingsHooks : ContentDefinitionEditorEventsBase {
        private readonly INotifier _notifier;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IContentHelpersService _contentHelpersService;
        
        static string[] ForbidenParts =  { "ConnectorFeederPart", "ConnectorListenerPart", "ClientConnectorPart","DummyAntiForgeryPart" };

        public ClientConnectorSettingsHooks(INotifier notifier,IContentDefinitionManager contentDefinitionManager
            ,IContentHelpersService contentHelpersService) {
            _contentHelpersService      = contentHelpersService;
            T                           = NullLocalizer.Instance;
            _contentDefinitionManager   = contentDefinitionManager;
            _notifier                   = notifier;
        }

        public Localizer T { get; set; }

        IEnumerable<dynamic> GetParts()
        {
            return _contentDefinitionManager.ListPartDefinitions()
                .Where(c => /*c.Name.EndsWith("part", StringComparison.InvariantCultureIgnoreCase) && */!ForbidenParts.Contains(c.Name) && c.Settings.GetModel<ContentPartSettings>().Attachable)
                .Select(c => new { Name = c.Name, DisplayName = c.Name.TrimEnd("Part").CamelFriendly() }).OrderBy(c => c.DisplayName); 
        }
        
        public override IEnumerable<TemplateViewModel> TypePartEditor(ContentTypePartDefinition definition) {

            if (definition.PartDefinition.Name != "ClientConnectorPart")
                yield break;

            var settings                = definition.Settings.GetModel<ClientConnectorSettings>();
            settings.contentItemName    = definition.ContentTypeDefinition.Name;

            ContentPartDefinition ct = _contentDefinitionManager.GetPartDefinition(definition.ContentTypeDefinition.Name);
            settings.AllFields          = ct.Fields.Any() ? ct.Fields.Select(f => new { Name = f.Name, DisplayName = f.DisplayName }) : new[] { new { Name = string.Empty, DisplayName = "No Fields" } };
            settings.AllParts = definition.ContentTypeDefinition.Parts.Where(c => c.PartDefinition.Name == definition.ContentTypeDefinition.Name || (!ForbidenParts.Contains(c.PartDefinition.Name) && c.PartDefinition.Settings.GetModel<ContentPartSettings>().Attachable))
                .Select(c => new { Name = c.PartDefinition.Name, DisplayName = c.PartDefinition.Name.TrimEnd("Part").CamelFriendly() }).OrderBy(c => c.DisplayName); ;
            settings.AllProperties      = _contentHelpersService.GetProperties(string.IsNullOrEmpty(settings.PartName) ? settings.AllParts.FirstOrDefault().Name : settings.PartName);
            yield return DefinitionTemplate(settings);
        }

        public override IEnumerable<TemplateViewModel> TypePartEditorUpdate(ContentTypePartDefinitionBuilder builder, IUpdateModel updateModel)
        {
            if (builder.Name != "ClientConnectorPart")
                yield break;

            var settings        = new ClientConnectorSettings();
            if (updateModel.TryUpdateModel(settings, "ClientConnectorSettings", null, null)) {
                settings.Build(builder);
            }

            yield return DefinitionTemplate(settings);
        }
    }
}
