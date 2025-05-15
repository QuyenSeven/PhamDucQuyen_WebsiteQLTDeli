
using QLyNhaHangTDeli.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Unity;
using Unity.Mvc5;

namespace QLyNhaHangTDeli
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
           
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            System.Web.HttpContext.Current.Application["StaticFiles"] = new object();


            var container = new UnityContainer();
            container.RegisterType<QLyNhaHangTDeli.Services.Interfaces.IVnPayService, QLyNhaHangTDeli.Services.VnPayService>();

            DependencyResolver.SetResolver(new UnityDependencyResolver(container));
        }
    }
}
