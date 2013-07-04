using Orchard;
using Orchard.ContentManagement;
using System;

namespace Datwendo.ClientConnector.Models
{

    public class ClientConnectorAdminSettingsPart : ContentPart<ClientConnectorAdminSettingsPartRecord> 
    {
        public int PublisherId
        {
            get { return Record.PublisherId; }
            set { Record.PublisherId = value; }
        }

        public string ServiceProdUrl
        {
            get { return Record.ServiceProdUrl; }
            set { Record.ServiceProdUrl = value; }
        }

        public int CurrentAPI
        {
            get { return Record.CurrentAPI; }
            set { Record.CurrentAPI = value; }
        }

        public int TransactionDelay
        {
            get { return Record.TransactionDelay; }
            set { Record.TransactionDelay = value; }
        }
    }
}
