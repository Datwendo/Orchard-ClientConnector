using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Orchard.ContentManagement.MetaData.Builders;

namespace Datwendo.ClientConnector.Settings {

    /// <summary>
    /// Settings when attaching part to a content item
    /// </summary>
    public class ClientConnectorSettings {

        public ClientConnectorSettings() {
            ConnectorId     = 0;
            SecretKey       = string.Empty;
            IsFast          = true;
        }

        public virtual int ConnectorId
        {
            get;
            set;
        }

        public virtual string SecretKey
        {
            get;
            set;
        }

        public virtual bool IsFast
        {
            get;
            set;
        }

        public void Build(ContentTypePartDefinitionBuilder builder) {
            builder.WithSetting("ClientConnectorSettings.ConnectorId", ConnectorId.ToString(CultureInfo.InvariantCulture));
            builder.WithSetting("ClientConnectorSettings.SecretKey", SecretKey);
            builder.WithSetting("ClientConnectorSettings.IsFast", IsFast.ToString(CultureInfo.InvariantCulture));
        }
    }
}
