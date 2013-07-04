using Orchard.UI.Resources;

namespace Datwendo.ClientConnector {
    public class ResourceManifest : IResourceManifestProvider {
        public void BuildManifests(ResourceManifestBuilder builder) {
            var manifest = builder.Add();
            manifest.DefineStyle("ClientConnectorSettings").SetUrl("datwendo-clientconnector-settings.css");
        }
    }
}
