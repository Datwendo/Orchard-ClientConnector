using Orchard;
using Orchard.ContentManagement.Records;
using System;
using System.ComponentModel.DataAnnotations;

namespace Datwendo.ClientConnector.Models {

    public class ClientConnectorAdminSettingsPartRecord : ContentPartRecord
    {
        public virtual int PublisherId
        {
            get;
            set;
        }

        public virtual string ServiceProdUrl
        {
            get;
            set;
        }

        public virtual int CurrentAPI
        {
            get;
            set;
        }

        public virtual int TransactionDelay
        {
            get;
            set;
        }     
    }
}