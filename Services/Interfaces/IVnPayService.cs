using QLyNhaHangTDeli.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace QLyNhaHangTDeli.Services.Interfaces
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(PaymentInformationModel model, HttpContextBase context);
        PaymentResponseModel PaymentExecute(NameValueCollection collections);
    }
}
