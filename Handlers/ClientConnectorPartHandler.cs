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
using Datwendo.ClientConnector.Settings;
using System.Collections.Specialized;
using System.Reflection;


namespace Datwendo.ClientConnector.Handlers
{
    [OrchardFeature("Datwendo.ClientConnector")]
    public class ClientConnectorPartHandler : ContentHandler {

        private IClientConnectorService _clientConnectorService;
        private IOrchardServices _orchardServices;
        public Localizer T { get; set; }

        public ClientConnectorPartHandler(IRepository<ClientConnectorPartRecord> repository, IClientConnectorService clientConnectorService, IOrchardServices orchardServices)
        {            
            _clientConnectorService = clientConnectorService;
            _orchardServices        = orchardServices;
            T                       = NullLocalizer.Instance;
            Filters.Add(StorageFilter.For(repository));
            OnUpdated<ClientConnectorPart>(AssignIdentity);
        }
          
        protected void AssignIdentity(UpdateContentContext context, ClientConnectorPart part)
        {
            if (part.CIndex != 0)
                return;
            var requestType = RequestType.NoData;
            if (part.Settings.ContainsKey("ClientConnectorSettings.RequestType"))
                requestType = (RequestType)int.Parse(part.Settings["ClientConnectorSettings.RequestType"]);
            switch (requestType )
            {
                default:
                case RequestType.NoData:
                {
                    int nval = 0;
                    if (_clientConnectorService.ReadNext(part, out nval))
                    {
                        part.CIndex = nval;
                        return;
                    }
                    break;
                }
                case RequestType.DataString:
                {
                    string targetpartName       = part.Settings["ClientConnectorSettings.PartName"];
                    string targetPropertyName   = part.Settings["ClientConnectorSettings.PropertyName"];
                    /*
                    string targetwtPart         = (targetpartName.EndsWith("part",StringComparison.InvariantCultureIgnoreCase)) ? targetpartName.Substring(0,targetpartName.Length-4): targetpartName;
                    var formField               = string.Format("{0}.{1}",targetwtPart,targetPropertyName);
                    //ContentPart targetPart = part.ContentItem.Parts.Where(p => p.PartDefinition.Name == targetpartName).FirstOrDefault();
                    NameValueCollection form    =_orchardServices.WorkContext.HttpContext.Request.Form;
                    var strval                  = form[formField].ToString();
                     * */
                    ContentPart targetPart      = part.ContentItem.Parts.Where(p => p.PartDefinition.Name == targetpartName).FirstOrDefault();
                    var strval                  = string.Empty;                    
                    Type type                   = targetPart.GetType() ;
                    PropertyInfo propertyInfo   = type.GetProperty( targetPropertyName, BindingFlags.Instance|BindingFlags.Public , null , typeof(String) , new Type[0] , null );
                    if (propertyInfo != null)
                    {
                        strval                  = propertyInfo.GetValue(targetPart).ToString();
                    }

                    int nval                    = 0;
                    if (_clientConnectorService.ReadNextWithData(part,strval, out nval))
                    {
                        part.CIndex             = nval;
                        return;
                    }
                    break;
                }
                case RequestType.DataBlob:
                {
                    string targetpartName       = part.Settings["ClientConnectorSettings.PartName"];

                    string targetPropertyName   = part.Settings["ClientConnectorSettings.PropertyName"];
                    /*
                     string targetwtPart         = (targetpartName.EndsWith("part",StringComparison.InvariantCultureIgnoreCase)) ? targetpartName.Substring(0,targetpartName.Length-4): targetpartName;
                    var formField               = string.Format("{0}.{1}",targetwtPart,targetPropertyName);
                    NameValueCollection form    =_orchardServices.WorkContext.HttpContext.Request.Form;
                    var strval                  = form[formField].ToString();
                     * */
                    ContentPart targetPart      = part.ContentItem.Parts.Where(p => p.PartDefinition.Name == targetpartName).FirstOrDefault();
                    var strval                  = string.Empty;
                    Type type                   = targetPart.GetType();
                    PropertyInfo propertyInfo   = type.GetProperty(targetPropertyName, BindingFlags.Instance | BindingFlags.Public, null, typeof(String), new Type[0], null);
                    if (propertyInfo != null)
                    {
                        strval                  = propertyInfo.GetValue(targetPart).ToString();
                    }
                    IEnumerable<string> fileList= new string[]{strval};
                    IEnumerable<FileDesc> nVal  = null;

                    if (_clientConnectorService.ReadNextWithBlob(part,fileList, out nVal))
                    {            
                        part.CIndex             = (nVal == null || nVal.Count() == 0 ) ? 0: nVal.First().CounterVal;
                        return;
                    }
                    break;
                }
            }
            Logger.Debug(string.Format("ClientConnectorPartHandler  AssignIdentity Error for ContentItem: {0}, CounterId: {1}", new Object[] { part.ContentItem.TypeDefinition.Name, _clientConnectorService.GetConnectorId(part)}));
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