using System.Web.Mvc;
using Orchard.Localization;
using Orchard;
using Orchard.Mvc;
using Datwendo.ClientConnector.Models;
using Orchard.DisplayManagement;
using Orchard.ContentManagement;
using Orchard.Environment.Extensions;


namespace Datwendo.ClientConnector.Controllers {
    [OrchardFeature("Datwendo.ClientConnector")]
    public class ClientConnectorController : Controller, IUpdateModel  {
        public IOrchardServices Services { get; set; }
        private readonly dynamic _shapeFactory;
        public ClientConnectorController(IOrchardServices services, IShapeFactory shapeFactory) 
        {
            Services        = services;
            _shapeFactory   = shapeFactory;
            T               = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }



        bool IUpdateModel.TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties)
        {
            return TryUpdateModel(model, prefix, includeProperties, excludeProperties);
        }

        void IUpdateModel.AddModelError(string key, LocalizedString errorMessage)
        {
            ModelState.AddModelError(key, errorMessage.ToString());
        }
    }
}
