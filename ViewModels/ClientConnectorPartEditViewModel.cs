using Datwendo.ClientConnector.Settings;

namespace Datwendo.ClientConnector.ViewModels
{

    public class ClientConnectorPartEditViewModel {
        public ClientConnectorSettings Settings { get; set; }
        public int ConnectorId { get; set; }
        public string SecretKey { get; set; }
        public bool IsFast { get; set; }
    }
}
