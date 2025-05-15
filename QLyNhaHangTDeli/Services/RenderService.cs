using System.IO;
using System.Web.Mvc;

public class RenderService
{
    public static string RenderRazorViewToString(ControllerContext context, string viewName, object model)
    {
        var viewData = new ViewDataDictionary { Model = model };
        var tempData = new TempDataDictionary();

        using (var sw = new StringWriter())
        {
            var viewResult = ViewEngines.Engines.FindPartialView(context, viewName);
            var viewContext = new ViewContext(context, viewResult.View, viewData, tempData, sw);
            viewResult.View.Render(viewContext, sw);
            return sw.GetStringBuilder().ToString();
        }
    }
}
