using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Orchard.ContentManagement.MetaData.Builders;
using Datwendo.ClientConnector.Models;
using Orchard.Localization;
using System;
using System.Web.UI.WebControls;
using System.Web.Mvc;

namespace Datwendo.ClientConnector.Settings {

    public enum RequestType { NoData = 0, DataString = 1, DataBlob = 2 }
    
    public class RequestTypeSt
    {
        public static LocalizedString Label(Localizer T, RequestType s)
        {
            switch (s)
            {
                default:
                case RequestType.NoData:
                    return T("NoData");
                case RequestType.DataString:
                    return T("DataString");
                case RequestType.DataBlob:
                    return T("DataBlob");

            }
        }


        public static IEnumerable<SelectListItem> GetRequestTypeLst(Localizer T, RequestType selected)
        {
            return Enum.GetValues(typeof(RequestType))
                            .Cast<RequestType>()
                            .Select(i =>
                            new SelectListItem
                            {
                                Text = RequestTypeSt.Label(T, i).Text,
                                Value = i.ToString(),
                                Selected = i == selected
                            });

        }

    }

    /// <summary>
    /// Settings when attaching part to a content item
    /// </summary>
    public class ClientConnectorSettings {

        public ClientConnectorSettings()
        {
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
        public string contentItemName { get; set; }
        public IEnumerable<dynamic> AllParts { get; set; }
        public IEnumerable<dynamic> AllProperties { get; set; }
        public IEnumerable<dynamic> AllFields { get; set; }
        public virtual RequestType RequestType { get; set; }
        public virtual bool KeepDataCopy { get; set; }
        public virtual string PartName { get; set; }
        public virtual string PropertyName { get; set; }
        public virtual string FieldName { get; set; } 

        public void Build(ContentTypePartDefinitionBuilder builder) {
            builder.WithSetting("ClientConnectorSettings.ConnectorId", ConnectorId.ToString(CultureInfo.InvariantCulture));
            builder.WithSetting("ClientConnectorSettings.SecretKey", SecretKey);
            builder.WithSetting("ClientConnectorSettings.IsFast", IsFast.ToString(CultureInfo.InvariantCulture));
            builder.WithSetting("ClientConnectorSettings.RequestType", ((int)RequestType).ToString());
            builder.WithSetting("ClientConnectorSettings.KeepDataCopy", KeepDataCopy.ToString(CultureInfo.InvariantCulture));
            builder.WithSetting("ClientConnectorSettings.PartName",PartName);
            builder.WithSetting("ClientConnectorSettings.PropertyName", string.IsNullOrEmpty(PropertyName) ? string.Empty : PropertyName);
            builder.WithSetting("ClientConnectorSettings.FieldName", string.IsNullOrEmpty(FieldName) ? string.Empty : FieldName);
        }
    }
}
