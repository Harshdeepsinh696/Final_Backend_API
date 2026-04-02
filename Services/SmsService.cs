using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Final_Backend_API.Services
{
    public class SmsService
    {
        private readonly HttpClient _httpClient;

        // ── BhashSMS credentials (SMS) ──
        private const string BhashUser = "success";
        private const string BhashPass = "Bulk@12";
        private const string BhashSender = "BHAINF";
        private const string BhashUrl = "http://bhashsms.com/api/sendmsg.php";

        // ── Twilio credentials (WhatsApp) ──
        private const string TwilioAccountSid = "ACc23a973905571c8cf23c8264f0f96872";
        private const string TwilioAuthToken = "9035985020a4ebea1307b216aaa8f820";
        private const string TwilioWhatsApp = "whatsapp:+14155238886";

        public SmsService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
        }

        public async Task SendSmsAsync(string phone, string message)
        {
            // ══════════════════════════════════════
            // STEP 1 — Print in console FIRST
            // ══════════════════════════════════════
            Console.WriteLine("============================");
            Console.WriteLine("📋 REMINDER TRIGGERED!");
            Console.WriteLine($"📱 Phone   : {phone}");
            Console.WriteLine($"💊 Message : {message}");
            Console.WriteLine($"🕐 Time    : {DateTime.Now:hh:mm:ss tt}");
            Console.WriteLine("============================");

            // ══════════════════════════════════════
            // STEP 2 — Send SMS via BhashSMS
            // ══════════════════════════════════════
            var smsUrl = $"{BhashUrl}" +
                         $"?user={BhashUser}" +
                         $"&pass={BhashPass}" +
                         $"&sender={BhashSender}" +
                         $"&phone={phone}" +
                         $"&text={Uri.EscapeDataString(message)}" +
                         $"&priority=ndnd" +
                         $"&stype=normal";

            try
            {
                Console.WriteLine("📤 Sending SMS via BhashSMS...");
                var smsRequest = new HttpRequestMessage(HttpMethod.Get, smsUrl);
                var smsResponse = await _httpClient.SendAsync(smsRequest);
                var smsResult = await smsResponse.Content.ReadAsStringAsync();

                Console.WriteLine("============================");
                Console.WriteLine("📨 SMS Response: " + smsResult);

                if (smsResult.StartsWith("S.") || smsResult.Contains("Submitted"))
                    Console.WriteLine("✅ SMS Sent Successfully to " + phone);
                else
                    Console.WriteLine("❌ SMS Failed! Reason: " + smsResult);

                Console.WriteLine("============================");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ SMS Error: " + ex.Message);
            }

            // ══════════════════════════════════════
            // STEP 3 — Send WhatsApp via Twilio
            // ══════════════════════════════════════
            try
            {
                Console.WriteLine("📤 Sending WhatsApp via Twilio...");

                var twilioUrl = $"https://api.twilio.com/2010-04-01/Accounts/{TwilioAccountSid}/Messages.json";

                var whatsappData = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "From", TwilioWhatsApp },
                    { "To",   $"whatsapp:+91{phone}" },
                    { "Body", message }
                };

                var waRequest = new HttpRequestMessage(HttpMethod.Post, twilioUrl);

                // Basic Auth with Account SID and Auth Token
                var credentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{TwilioAccountSid}:{TwilioAuthToken}")
                );
                waRequest.Headers.Add("Authorization", $"Basic {credentials}");
                waRequest.Content = new FormUrlEncodedContent(whatsappData);

                var waResponse = await _httpClient.SendAsync(waRequest);
                var waResult = await waResponse.Content.ReadAsStringAsync();

                Console.WriteLine("============================");
                Console.WriteLine("📨 WhatsApp Response: " + waResult);

                if (waResponse.IsSuccessStatusCode)
                    Console.WriteLine("✅ WhatsApp Sent Successfully to " + phone);
                else
                    Console.WriteLine("❌ WhatsApp Failed! Reason: " + waResult);

                Console.WriteLine("============================");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ WhatsApp Error: " + ex.Message);
            }
        }
    }
}