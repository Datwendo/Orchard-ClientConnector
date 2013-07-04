using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement.Drivers;
using Datwendo.ClientConnector.Models;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.UI.Notify;
using Orchard.Settings;
using Orchard.Environment.Extensions;
using Orchard;
using System.Globalization;
using Orchard.Security;
using Orchard.ContentManagement;

namespace Datwendo.ClientConnector.Drivers
{
    [OrchardFeature("Datwendo.ClientConnector")]
    public class ClientConnectorAdminSettingsPartDriver : ContentPartDriver<ClientConnectorAdminSettingsPart>
    {
        public IOrchardServices Services { get; set; }
        private readonly Lazy<CultureInfo> _cultureInfo;
        private readonly INotifier _notifier;
        private readonly IAuthenticationService _authenticationService;
        private readonly IAuthorizationService _authorizationService;

        public ClientConnectorAdminSettingsPartDriver(IOrchardServices services
            , IAuthenticationService authenticationService
            , IAuthorizationService authorizationService
            , INotifier notifier)
        {
            Services = services;
            _authenticationService = authenticationService;
            _authorizationService = authorizationService;
            T = NullLocalizer.Instance;
            _cultureInfo = new Lazy<CultureInfo>(() => CultureInfo.GetCultureInfo(Services.WorkContext.CurrentCulture));
            _notifier = notifier;
        }

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        protected override string Prefix { get { return "ClientConnectorAdminSettings"; } }

        private const string TemplateName = "Parts/ClientConnectorAdminSettings";

        protected override DriverResult Editor(ClientConnectorAdminSettingsPart part, dynamic shapeHelper)
        {

            return ContentShape("Parts_ClientConnectorAdminSettings",
                       () => shapeHelper.EditorTemplate(
                           TemplateName: TemplateName,
                           Model: part,
                           Prefix: Prefix)).OnGroup("ClientConnectorSettings"); ;
        }

        protected override DriverResult Editor(ClientConnectorAdminSettingsPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            if (!_authorizationService.TryCheckAccess(StandardPermissions.SiteOwner, _authenticationService.GetAuthenticatedUser(), part))
                return null;
            if (updater.TryUpdateModel(part, Prefix, null, null))
            {
                // _notifier.Information(T("ClientConnector settings updated successfully"));
            }
            else
            {
                _notifier.Error(T("ClientConnector settings update error!"));
            }
            return Editor(part, shapeHelper);
        }
    }
}