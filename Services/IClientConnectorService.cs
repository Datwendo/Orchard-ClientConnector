using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard;
using Datwendo.ClientConnector.Models;
using Datwendo.ClientConnector.Settings;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.ContentManagement;
using Orchard.UI.Notify;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Mvc;
using Orchard.ContentManagement.MetaData;
using Orchard.Core.Contents.Settings;
using System.IO;
using System.Reflection;

namespace Datwendo.ClientConnector.Services
{
    public interface IClientConnectorService : IDependency
    {
        int GetConnectorId(ClientConnectorPart part);
        string GetSecretKey(ClientConnectorPart part);

        bool Read(ClientConnectorPart part,out int newVal);
        bool ReadNext(ClientConnectorPart part,out int newVal);
        bool ReadData(ClientConnectorPart part, int Idx,out string strVal);
        bool ReadNextWithData(ClientConnectorPart part, string strval, out int newVal);
        bool ReadBlob(ClientConnectorPart part, int idx, out IEnumerable<FileDesc> newVal);
        bool ReadNextWithBlob(ClientConnectorPart part, IEnumerable<string> fileList, out IEnumerable<FileDesc> newVal);        
        
        bool TransacKey(ClientConnectorPart part,out string NewKey);

        //bool Start();
        //bool Stop();
        //bool SetTrace();
    }

    public class ClientConnectorService : IClientConnectorService
    {
        public IOrchardServices Services { get; set; }
        //private readonly INotifier _notifier;
        
        private const string CCtorAPIController     = "CCtor";
        private const string DataCCtorAPIController = "DataCCtor";
        private const string BlobCCtorAPIController = "BlobCCtor";

        private const string AdminCCtorAPIController = "AdminCCtor";
        private const string TraceCCtorAPIController = "TraceCCtor";

        private readonly IContentDefinitionManager _contentDefinitionManager;


        public ClientConnectorService(IOrchardServices services
            //,INotifier notifier
            ,IContentDefinitionManager contentDefinitionManager) 
        {
            Services                    = services;
            _contentDefinitionManager   = contentDefinitionManager;
            T                           = NullLocalizer.Instance;
            Logger                      = NullLogger.Instance;
            //_notifier                   = notifier;
        }

        public ILogger Logger { get; set; }

        public Localizer T { get; set; }
        
        #region settings

        ClientConnectorAdminSettingsPart _Settings = null;
        ClientConnectorAdminSettingsPart Settings
        {
            get
            {
                if (_Settings == null)
                    _Settings = Services.WorkContext.CurrentSite.As<ClientConnectorAdminSettingsPart>();
                return _Settings;
            }
        }        
        
        public string ServiceProdUrl
        {
            get
            {
                string srv          = Settings.ServiceProdUrl.ToLower().Replace("api/v1", string.Empty).TrimEnd('/');
                srv                 = string.Format("{0}/api/v{1}", srv, Settings.CurrentAPI);
                return srv;
            }
        }

        public int TransacKeyDelay
        {
            get
            {
                return Settings.TransactionDelay;
            }
        }

        int PublisherId
        {
            get
            {
                return Settings.PublisherId;
            }
        }
        
        #endregion // Settings

        #region Settings attached to Content Type

        ClientConnectorSettings GetTypeSettings(ClientConnectorPart part)
        {
                return part.TypePartDefinition.Settings.GetModel<ClientConnectorSettings>();
        }
        
        public int GetConnectorId(ClientConnectorPart part)
        {
            return GetTypeSettings(part).ConnectorId;
        }

        public string GetSecretKey(ClientConnectorPart part) 
        {
            return GetTypeSettings(part).SecretKey;
        }

        public RequestType GetRequestType(ClientConnectorPart part)
        {
            return GetTypeSettings(part).RequestType;
        }

        public string GetPartName(ClientConnectorPart part)
        {
            return GetTypeSettings(part).PartName;
        }

        public string GetPropertyName(ClientConnectorPart part)
        {
            return GetTypeSettings(part).PropertyName;
        }

        bool IsFast(ClientConnectorPart part)
        {
            return GetTypeSettings(part).IsFast;
        }

        #endregion // Settings attached to Content Type

        #region WebAPI Calls

        #region Transac Calls

        // Extract a new transaction key from server
        public bool TransacKey(ClientConnectorPart part,out string NewKey)
        {
            Logger.Debug("ClientConnectorService: TransacKey BEG.");
            bool ret                        = false;
            NewKey                          = string.Empty;

            CCtrRequest2 CParam             = new CCtrRequest2
            {
                Ky                          = GetSecretKey(part),
                Dl                          = TransacKeyDelay
            };

            Logger.Debug("ClientConnectorService: TransacKey Ky: {0}, Dl: {1}", CParam.Ky,CParam.Dl);
            try
            {
                var tsk                     = Post4TransacAsync(GetConnectorId(part), CParam);
                CCtrResponse2 CRep          = tsk.Result;
                Logger.Debug("ClientConnectorService: TransacKey CRep.Cd: {0}", CRep.Cd);
                if (CRep.Cd == 0)
                {
                    ret                     = true;
                    NewKey                  = CRep.Ky;
                    Logger.Debug("ClientConnectorService: TransacKey NewKey: {0}", NewKey);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "ClientConnectorService TransacKey {0} - {1}", new Object[] { GetSecretKey(part), GetConnectorId(part) });
                ret                         = false;
            }
            Logger.Debug("ClientConnectorService: TransacKey END ret {0}", ret);
            return ret;
        }

        private async Task<CCtrResponse2> Post4TransacAsync(int CId, CCtrRequest2 CReq)
        {
            HttpClient client                   = new HttpClient();
            CCtrResponse2 result                = null;
            try
            {
                Uri address                     = new Uri(string.Format("{0}/{1}/{2}", ServiceProdUrl, CCtorAPIController, CId));
                HttpResponseMessage response    = client.PostAsJsonAsync(address.ToString(),CReq).Result;
                response.EnsureSuccessStatusCode();
                result                          = await response.Content.ReadAsAsync<CCtrResponse2>();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "ClientConnectorService Post4TransacAsync Error reading from WebAPI");
                throw;
            }
            return result;
        }

        #endregion // transac call

        #region Base Connector

        // Read the actual value for Connector
        public bool Read(ClientConnectorPart part,out int Val)
        {
            bool ret                            = false;
            Val                                 = int.MinValue;

            string NewKey                       = GetSecretKey(part);
            if (!IsFast(part) && !TransacKey(part,out NewKey))
                return false;
            UrlHelper uh                        = new UrlHelper(Services.WorkContext.HttpContext.Request.RequestContext);
            string ky                           = uh.Encode(NewKey);
            
            try
            {
                var tsk                         = GetAsync(GetConnectorId(part),ky);
                CCtrResponse CRep               = tsk.Result;
                if (CRep.Cd == 0 )
                {
                    ret                         = true;
                    Val                         = CRep.Vl;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "ClientConnectorService read {0} - {1}", new Object[] { GetSecretKey(part), GetConnectorId(part) });
                return false;
            }
            return ret;
        }

        private async Task<CCtrResponse> GetAsync(int CId,string Ky)
        {
            HttpClient client                   = new HttpClient();
            CCtrResponse result                 = null;
            try
            {
                Uri address                     = new Uri(string.Format("{0}/{1}/{2}?Ky={3}", ServiceProdUrl, CCtorAPIController, CId, Ky));
                HttpResponseMessage response    = client.GetAsync(address.ToString()).Result;
                response.EnsureSuccessStatusCode();
                result                          = await response.Content.ReadAsAsync<CCtrResponse>();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "ClientConnectorService GetAsync Error reading from WebAPI");
                throw;
            }
            return result;
        }

        public bool ReadNext(ClientConnectorPart part,out int newVal)
        {
            Logger.Debug("ClientConnectorService: ReadNext BEG.");
            switch(GetRequestType(part))
            {
                default:
                case RequestType.NoData:
                    return ReadNextNoData(part,out newVal);
                case RequestType.DataString:
                    return ReadNextData(part,out newVal);
                case RequestType.DataBlob:
                    return ReadNextBlob(part, out newVal);
            }
        }


        public bool ReadNextNoData(ClientConnectorPart part,out int newVal)
        {
            Logger.Debug("ClientConnectorService: ReadNextNoData BEG.");
            bool ret                        = false;
            newVal                          = int.MinValue;
      
            string NewKey                   = GetSecretKey(part);
            if ( !IsFast(part)  && !TransacKey(part,out NewKey))
                return false;

            PubCCtrRequest CReq             = new PubCCtrRequest
            {
                Ky                          = NewKey,
                Pb                          = PublisherId
            };                

            try
            {
                var tsk                     = PutAsync(GetConnectorId(part),CReq);
                CCtrResponse CRep           = tsk.Result;
                Logger.Debug("ClientConnectorService: ReadNextNoData CRep.Cd: {0}",CRep.Cd);
                if (CRep.Cd == 0)
                {
                    newVal                  = CRep.Vl;
                    ret                     = true;
                }
            }
            catch (Exception ex)
            { 

                Logger.Log(LogLevel.Error, ex, "ClientConnectorService ReadNextNoData {0} - {1}", new Object[] {GetSecretKey(part), GetConnectorId(part) });
                ret                         = false;
            }
            Logger.Debug("ClientConnectorService: ReadNextNoData END : {0}", ret);
            return ret;
        }

        private async Task<CCtrResponse> PutAsync(int CId, PubCCtrRequest CReq)
        {
            HttpClient client                   = new HttpClient();
            CCtrResponse result                 = null;
            try
            {
                Uri address                     = new Uri(string.Format("{0}/{1}/{2}", ServiceProdUrl, CCtorAPIController, CId));
                HttpResponseMessage response    = client.PutAsJsonAsync(address.ToString(), CReq).Result;
                response.EnsureSuccessStatusCode();
                result                          = await response.Content.ReadAsAsync<CCtrResponse>();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "ClientConnectorService PutAsync Error reading from WebAPI");
                throw;
            }
            return result;
        }

        #endregion Base Connector

        #region DataStorage Connector

        // Read the actual value for Connector
        public bool ReadData(ClientConnectorPart part, int idx, out string strVal)
        {
            bool ret        = false;
            strVal          = string.Empty;
            string NewKey   = GetSecretKey(part);

            if (!IsFast(part) && !TransacKey(part, out NewKey))
                return false;
            UrlHelper uh    = new UrlHelper(Services.WorkContext.HttpContext.Request.RequestContext);
            string ky       = uh.Encode(NewKey);

            try
            {
                var tsk     = GetWithDataAsync(GetConnectorId(part), idx, ky);
                CCtrResponseSt CRep = tsk.Result;
                if (CRep.Cd == 0)
                {
                    ret     = true;
                    strVal  = CRep.St;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "ClientConnectorService ReadData {0} - {1}", new Object[] { GetSecretKey(part), GetConnectorId(part) });
                return false;
            }
            return ret;
        }

        private async Task<CCtrResponseSt> GetWithDataAsync(int CId, int Ix, string Ky)
        {
            HttpClient client                   = new HttpClient();
            CCtrResponseSt result               = null;
            try
            {
                //Get(int id,int ix,string Ky)
                Uri address                     = new Uri(string.Format("{0}/{1}/{2}?ix={3}&Ky={4}", new object[] { ServiceProdUrl, DataCCtorAPIController, CId, Ix, Ky }));
                HttpResponseMessage response    = client.GetAsync(address.ToString()).Result;
                response.EnsureSuccessStatusCode();
                result = await response.Content.ReadAsAsync<CCtrResponseSt>();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "ClientConnectorService GetAsync Error reading from WebAPI");
                throw;
            }
            return result;
        }

        public bool ReadNextData(ClientConnectorPart part, out int newVal)
        {
            string strval       = string.Empty;
            newVal              = 0;

            string partName     = GetPartName(part);
            string propName     = GetPropertyName(part);
            var targetPart      = part.ContentItem.Parts.Where(p => p.PartDefinition.Name == partName).SingleOrDefault();
            if ((targetPart == null) || !(targetPart is ContentPart))
                return false;
            ContentPart cpart   = targetPart as ContentPart;

            Type type           = targetPart.GetType() ;
            PropertyInfo propertyInfo = type.GetProperty( propName, BindingFlags.Instance|BindingFlags.Public , null , typeof(String) , new Type[0] , null );
            if (propertyInfo == null)
                return false;
            var val             = propertyInfo.GetValue(targetPart); 
            
            strval              = (val == null) ? string.Empty:val.ToString();

            return ReadNextWithData(part, strval, out newVal);
        }

        public bool ReadNextWithData(ClientConnectorPart part, string strval, out int newVal)
        {
            bool ret                = false;
            newVal                  = int.MinValue;

            string NewKey           = GetSecretKey(part);

            if (!IsFast(part) && !TransacKey(part, out NewKey))
                return false;

            StringStorRequest CReq  = new StringStorRequest
            {
                Ky                  = NewKey,
                Pb                  = PublisherId,
                St                  = strval
            };

            try
            {
                var tsk             = PutWithDataAsync(GetConnectorId(part), CReq);
                CCtrResponse CRep   = tsk.Result;
                if (CRep.Cd == 0)
                {
                    newVal          = CRep.Vl;
                    ret             = true;
                }
            }
            catch (Exception ex)
            {

                Logger.Log(LogLevel.Error, ex, "ClientConnectorService ReadNextWithData {0} - {1}", new Object[] { GetSecretKey(part), GetConnectorId(part) });
                return false;
            }
            return ret;
        }

        private async Task<CCtrResponse> PutWithDataAsync(int CId, StringStorRequest CReq)
        {
            HttpClient client                   = new HttpClient();
            CCtrResponse result                 = null;
            try
            {
                Uri address                     = new Uri(string.Format("{0}/{1}/{2}", ServiceProdUrl, DataCCtorAPIController, CId));
                HttpResponseMessage response    = client.PutAsJsonAsync(address.ToString(), CReq).Result;
                response.EnsureSuccessStatusCode();
                result                          = await response.Content.ReadAsAsync<CCtrResponse>();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "ClientConnectorService PutWithDataAsync Error reading from WebAPI");
                throw;
            }
            return result;
        }              

        #endregion // DataStorage Connector

        #region Blob Storage


        // Read the actual value for Connector
        public bool ReadBlob(ClientConnectorPart part, int idx, out IEnumerable<FileDesc> newVal)
        {
            bool ret        = false;
            newVal          = null;
            string NewKey   = GetSecretKey(part);

            if (!IsFast(part) && !TransacKey(part, out NewKey))
                return false;

            UrlHelper uh    = new UrlHelper(Services.WorkContext.HttpContext.Request.RequestContext);
            string ky       = uh.Encode(NewKey);

            try
            {
                var tsk     = GetBlobAsync(GetConnectorId(part), idx, ky);
                CCtrResponseBlob CRep = tsk.Result;
                if (CRep.Cd == 0)
                {
                    ret     = true;
                    newVal  = CRep.Lst;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "ClientConnectorService ReadBlob {0} - {1}", new Object[] { GetSecretKey(part), GetConnectorId(part) });
                return false;
            }
            return ret;
        }

        //GET api/v1/BlobCCtor/{id}?Ix={Ix}&Ky={Ky}
        private async Task<CCtrResponseBlob> GetBlobAsync(int CId, int Ix, string Ky)
        {
            HttpClient client                   = new HttpClient();
            CCtrResponseBlob result             = null;
            try
            {
                //Get(int id,int ix,string Ky)
                Uri address                     = new Uri(string.Format("{0}/{1}/{2}?Ix={3}&Ky={4}", new object[] { ServiceProdUrl, BlobCCtorAPIController, CId, Ix, Ky }));
                HttpResponseMessage response    = client.GetAsync(address.ToString()).Result;
                response.EnsureSuccessStatusCode();
                result                          = await response.Content.ReadAsAsync<CCtrResponseBlob>();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "ClientConnectorService GetBlobAsync Error reading from WebAPI");
                throw;
            }
            return result;
        }


        public bool ReadNextBlob(ClientConnectorPart part, out int newVal)
        {
            IEnumerable<string> fileList    = null;
            IEnumerable<FileDesc> nVal      = null;
            bool ret                        = ReadNextWithBlob(part, fileList, out nVal);
            newVal                          = (nVal == null || nVal.Count() == 0 ) ? 0: nVal.First().CounterVal;
            return ret;
        }

        public bool ReadNextWithBlob(ClientConnectorPart part, IEnumerable<string> fileList, out IEnumerable<FileDesc> newVal)
        {
            bool ret                    = false;
            newVal                      = null;

            string NewKey               = GetSecretKey(part);

            if (!IsFast(part) && !TransacKey(part, out NewKey))
                return false;

            try
            {
                var tsk                 = PostBlobAsync(GetConnectorId(part), PublisherId, NewKey, fileList);
                CCtrResponseBlob CRep   = tsk.Result;
                if (CRep.Cd == 0)
                {
                    newVal              = CRep.Lst;
                    ret                 = true;
                }
            }
            catch (Exception ex)
            {

                Logger.Log(LogLevel.Error, ex, "ClientConnectorService ReadNextWithBlob {0} - {1}", new Object[] { GetSecretKey(part), GetConnectorId(part) });
                return false;
            }
            return ret;
        }

        // POST api/v1/BlobCCtor/{Id}?Pb={Pb}&Ky={Ky}
        private async Task<CCtrResponseBlob> PostBlobAsync(int CId, int PublisherId, string NewKey, IEnumerable<string> fileList)
        {
            HttpClient client                   = new HttpClient();
            CCtrResponseBlob result             = null;
            try
            {
                Uri address                     = new Uri(string.Format("{0}/{1}/{2}?Pb={3}&Ky={4}", new object[] { ServiceProdUrl, BlobCCtorAPIController, CId, PublisherId, NewKey }));
                using (var content              = new MultipartFormDataContent())
                {
                    foreach (string file in fileList)
                    {
                        var fileContent         = new StreamContent(File.OpenRead(file));
                        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                        {
                            FileName = file
                        };
                        content.Add(fileContent);
                    }

                    HttpResponseMessage response    = client.PostAsync(address, content).Result;
                    response.EnsureSuccessStatusCode();
                    result                          = await response.Content.ReadAsAsync<CCtrResponseBlob>();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "ClientConnectorService PostBlobAsync Error reading from WebAPI");
                throw;
            }
            return result;
        }


        #endregion // Blob Storage

        #region Admin calls
        /*
        public bool SetTrace(bool TraceState)
        {
            bool ret                = false;

            string NewKey           = SecretKey;
            if (!IsFast && !TransacKey(out NewKey))
                return false;

            CCtrRequestTr CReq      = new CCtrRequestTr
            {
                Ky                  = NewKey,
                St                  = TraceState
            };

            try
            {
                var tsk             = SetTraceAsync(ConnectorId, CReq);
                CCtrResponseTr CRep = tsk.Result;
                if (CRep.Cd == 0 || CRep.Cd == -1)
                {
                    ret             = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "ClientConnectorService Start {0} - {1}", new Object[] { SecretKey, ConnectorId });
                return false;
            }
            return ret;
        }

        private async Task<CCtrResponseTr> SetTraceAsync(int CId, CCtrRequestTr CReq)
        {
            HttpClient client                   = new HttpClient();
            CCtrResponseTr result               = null;
            try
            {
                Uri address                     = new Uri(string.Format("{0}/{1}/{2}", ServiceAdminUrl, TraceCCtorAPIController, CId));
                HttpResponseMessage response    = client.PutAsJsonAsync(address.ToString(), CReq).Result;
                response.EnsureSuccessStatusCode();
                result                          = await response.Content.ReadAsAsync<CCtrResponseTr>();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "SetTraceAsync Error reading from WebAPI");
                throw;
            }
            return result;
        }


        public bool Start()
        {
            bool ret                        = false;

            string NewKey                   = SecretKey;
            if ( !IsFast && !TransacKey( out NewKey))
                return false;

            CCtrRequest2 CReq               = new CCtrRequest2
            {
                Ky                          = NewKey,
                Dl                          = TransacKeyDelay
            };                

            try
            {
                var tsk                     = StartAsync(ConnectorId,CReq);
                CCtrResponse2 CRep          = tsk.Result;
                if (CRep.Cd == 0 || CRep.Cd == -1)
                {
                    ret                     = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "ClientConnectorService Start {0} - {1}", new Object[] {SecretKey, ConnectorId });
                return false;
            }
            return ret;
        }

        private async Task<CCtrResponse2> StartAsync(int CId, CCtrRequest2 CReq)
        {
            HttpClient client                       = new HttpClient();
            CCtrResponse2 result                    = null;
            try
            {
                Uri address                         = new Uri(string.Format("{0}/{1}/{2}", ServiceAdminUrl, AdminCCtorAPIController, CId));
                HttpResponseMessage response        = client.PostAsJsonAsync(address.ToString(), CReq).Result;
                response.EnsureSuccessStatusCode();
                result                              = await response.Content.ReadAsAsync<CCtrResponse2>();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "StartAsync Error reading from WebAPI");
                throw;
            }
            return result;
        }

        public bool Stop()
        {
            bool ret                        = false;

            string NewKey                   = SecretKey;
            if (!IsFast && !TransacKey(out NewKey))
                return false;

            UrlHelper uh                    = new UrlHelper(Services.WorkContext.HttpContext.Request.RequestContext);
         
            string ky                       = uh.Encode(NewKey);
            try
            {
                var tsk                     = StopAsync(ConnectorId,ky);
                CCtrResponse2 CRep          = tsk.Result;
                if (CRep.Cd == 0 || CRep.Cd == -1)
                {
                    ret                     = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "ClientConnectorService Stop {0} - {1}", new Object[] {SecretKey, ConnectorId });
                return false;
            }
            return ret;
        }

        private async Task<CCtrResponse2> StopAsync(int CId, string Ky)
        {
            HttpClient client                           = new HttpClient();
            CCtrResponse2 result                        = null;
            try
            {
                Uri address                             = new Uri(string.Format("{0}/{1}/{2}?Ky={3}", ServiceAdminUrl, AdminCCtorAPIController, CId,Ky));
                HttpResponseMessage response            = client.DeleteAsync(address.ToString()).Result;
                response.EnsureSuccessStatusCode();
                result                                  = await response.Content.ReadAsAsync<CCtrResponse2>();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "StopAsync Error reading from WebAPI");
                throw;
            }
            return result;
        }
         * */
        #endregion // admin calls

        #endregion // WebAPI Calls
    }
}