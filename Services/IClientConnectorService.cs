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

namespace Datwendo.ClientConnector.Services
{
    public interface IClientConnectorService : IDependency
    {
        bool Read(ClientConnectorPart part,out int newVal);
        bool ReadNext(ClientConnectorPart part,out int newVal);
        //bool Start();
        //bool Stop();
        bool TransacKey(ClientConnectorPart part,out string NewKey);
        //bool SetTrace();
    }

    public class ClientConnectorService : IClientConnectorService
    {
        public IOrchardServices Services { get; set; }
        private readonly INotifier _notifier;
        
        private const string CCtorAPIController = "CCtor";
        private const string AdminCCtorAPIController = "AdminCCtor";
        private const string TraceCCtorAPIController = "TraceCCtor";

        private readonly IContentDefinitionManager _contentDefinitionManager;


        public ClientConnectorService(IOrchardServices services, INotifier notifier,IContentDefinitionManager contentDefinitionManager) 
        {
            Services                    = services;
            _contentDefinitionManager   = contentDefinitionManager;
            T                           = NullLocalizer.Instance;
            Logger                      = NullLogger.Instance;
            _notifier                   = notifier;
        }

        public ILogger Logger { get; set; }

        public Localizer T { get; set; }
        
        #region settings

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


        ClientConnectorSettings GetTypeSettings(ClientConnectorPart part)
        {
                return part.TypePartDefinition.Settings.GetModel<ClientConnectorSettings>();
        }
        
        int GetConnectorId(ClientConnectorPart part)
        {
            return GetTypeSettings(part).ConnectorId;
        }

        string GetSecretKey(ClientConnectorPart part) 
        {
            return GetTypeSettings(part).SecretKey;
        }

        int PublisherId
        {
            get
            {
                return Settings.PublisherId;
            }
        }

        bool IsFast(ClientConnectorPart part)
        {
            return GetTypeSettings(part).IsFast;
        }

        #endregion // Settings

        #region WebAPI Calls

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
                Logger.Log(LogLevel.Error, ex, "Post4TransacAsync Error reading from WebAPI");
                throw;
            }
            return result;
        }
        
        // Read the actual value for Connector
        public bool Read(ClientConnectorPart part,out int Val)
        {
            bool ret                            = false;
            Val                                 = int.MinValue;

            string NewKey = GetSecretKey(part);
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
                Logger.Log(LogLevel.Error, ex, "GetAsync Error reading from WebAPI");
                throw;
            }
            return result;
        }

        public bool ReadNext(ClientConnectorPart part,out int newVal)
        {
            Logger.Debug("ClientConnectorService: ReadNext BEG.");
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
                Logger.Debug("ClientConnectorService: ReadNext CRep.Cd: {0}",CRep.Cd);
                if (CRep.Cd == 0)
                {
                    newVal                  = CRep.Vl;
                    ret                     = true;
                }
            }
            catch (Exception ex)
            { 

                Logger.Log(LogLevel.Error, ex, "ClientConnectorService ReadNext {0} - {1}", new Object[] {GetSecretKey(part), GetConnectorId(part) });
                ret                         = false;
            }
            Logger.Debug("ClientConnectorService: ReadNext END : {0}", ret);
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
                Logger.Log(LogLevel.Error, ex, "PutAsync Error reading from WebAPI");
                throw;
            }
            return result;
        }
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

        #endregion // WebAPI Calls
    }
}