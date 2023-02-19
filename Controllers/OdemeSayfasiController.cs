using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using System.Net;
using Newtonsoft.Json.Linq;

namespace aspnetcore_paytr_entegrasyon.Controllers
{
    public class OdemeSayfasiController : Controller
    {
        [HttpPost]
        public IActionResult PayTR()
        {
            string merchant_id = "id";
            string merchant_key = "key";
            string merchant_salt = "salt";

            string emailstr = "email@email.com"; // Müşterinizin email bilgisi
            int payment_amountstr = 100; // Toplam Siparis Tutarı
            string merchant_oid = Guid.NewGuid().ToString().Substring(0, 8).ToUpper().Replace("-", ""); // Eşsiz sipariş numarası
            string user_namestr = "Ad Soyad"; // Müşterinin adı soyadı
            string user_addressstr = "Adres"; // Müşterinin adresi
            string user_phonestr = "Telefon"; // Müşterinin telefonu
            string merchant_ok_url = "https://" + Request.Host + "/OdemeSayfasi/Basarili"; // Başarılı sonuçlanırsa yönlenecek bilgi adresi (siparis onaylama adresi değil)
            string merchant_fail_url = "https://" + Request.Host + "/OdemeSayfasi/Basarisiz"; // Başarısız sonuçlanırsa yönlenecek bilgi adresi (siparis reddetme adresi değil)
            string user_ip = "999.999.999.999"; // Müşterinin ip adresi

            // Örnek sepet 
            object[][] user_basket = {
            new object[] {"Örnek ürün 1", "18.00", 1}, // 1. ürün (Ürün Ad - Birim Fiyat - Adet)
            new object[] {"Örnek ürün 2", "33.25", 2}, // 2. ürün (Ürün Ad - Birim Fiyat - Adet)
            new object[] {"Örnek ürün 3", "45.42", 1}, // 3. ürün (Ürün Ad - Birim Fiyat - Adet)
            };

            string timeout_limit = "30"; // islem zaman asımı süresi (dakika cinsinden)
            string debug_on = "1"; // debug mod
            string test_mode = "1"; // test mod
            string no_installment = "0"; // taksit
            string max_installment = "0"; // ekranda görülecek taksit adedi
            string currency = "TL"; // para birimi
            string lang = ""; // dil

            NameValueCollection data = new NameValueCollection();
            data["merchant_id"] = merchant_id;
            data["user_ip"] = user_ip;
            data["merchant_oid"] = merchant_oid;
            data["email"] = emailstr;
            data["payment_amount"] = payment_amountstr.ToString();

            string user_basket_json = JsonSerializer.Serialize(user_basket.ToArray());
            string user_basketstr = Convert.ToBase64String(Encoding.UTF8.GetBytes(user_basket_json));
            data["user_basket"] = user_basketstr;

            string Birlestir = string.Concat(merchant_id, user_ip, merchant_oid, emailstr, payment_amountstr.ToString(), user_basketstr, no_installment, max_installment, currency, test_mode, merchant_salt);
            HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(merchant_key));
            byte[] b = hmac.ComputeHash(Encoding.UTF8.GetBytes(Birlestir));
            data["paytr_token"] = Convert.ToBase64String(b);
            data["debug_on"] = debug_on;
            data["test_mode"] = test_mode;
            data["no_installment"] = no_installment;
            data["max_installment"] = max_installment;
            data["user_name"] = user_namestr;
            data["user_address"] = user_addressstr;
            data["user_phone"] = user_phonestr;
            data["merchant_ok_url"] = merchant_ok_url;
            data["merchant_fail_url"] = merchant_fail_url;
            data["timeout_limit"] = timeout_limit;
            data["currency"] = currency;
            data["lang"] = lang;

            TempData["merchant_oid"] = merchant_oid;

            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    byte[] result = client.UploadValues("https://www.paytr.com/odeme/api/get-token", "POST", data);
                    string ResultAuthTicket = Encoding.UTF8.GetString(result);
                    dynamic json = JValue.Parse(ResultAuthTicket);

                    if (json.status == "success")
                    {
                        ViewBag.tokenData = "https://www.paytr.com/odeme/guvenli/" + json.token;
                    }
                    else
                    {
                        TempData["sonuc"] = "PAYTR IFRAME failed. reason:" + json.reason + "";
                    }
                }
            }
            catch (Exception x)
            {
                TempData["sonuc"] = x.Message.ToString();
            }

            return View();
        }

        [HttpPost]
        public IActionResult GeriDonusPayTR()
        {
            string merchant_key = "key";
            string merchant_salt = "salt";

            string merchant_oid = Request.Form["merchant_oid"];
            string status = Request.Form["status"];
            string total_amount = Request.Form["total_amount"];
            string hash = Request.Form["hash"];

            string Birlestir = string.Concat(merchant_oid, merchant_salt, status, total_amount);
            HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(merchant_key));
            byte[] b = hmac.ComputeHash(Encoding.UTF8.GetBytes(Birlestir));
            string token = Convert.ToBase64String(b);

            if (hash.ToString() != token)
            {
                TempData["gelen"] = "PAYTR notification failed: bad hash";
            }

            if (status == "success")
            { 
                //Ödeme Onaylandı
                TempData["gelen"] = "OK";
                // siparisi burada onaylayabilirsiniz
            }
            else
            { 
                //Ödemeye Onay Verilmedi
                TempData["gelen"] = "OK";
                // siparisi burada reddedebilirsiniz.
            }

            return View();

        }



    }
}
