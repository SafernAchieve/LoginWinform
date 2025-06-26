using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Login
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Load += new EventHandler(Form1_Load);
        }
        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                await webView21.EnsureCoreWebView2Async(null);

                // Add a filter to monitor all HTTP requests
                webView21.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);

                // Handle HTTP requests to extract headers
                webView21.CoreWebView2.WebResourceRequested += WebResourceRequestedHandler;

                // Handle navigation completed to extract cookies
                webView21.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;

                // Navigate to the login page
                webView21.CoreWebView2.Navigate("https://app.skedda.com/account/loginwithsso");
            }
            catch (Exception)
            {
                MessageBox.Show("Form1_Load");
            }
        }
        private string skeddaVerificationToken = null;
        private void WebResourceRequestedHandler(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            try
            {
                var request = e.Request;

                // Loop through all request headers
                foreach (var header in request.Headers)
                {

                    Console.WriteLine($"{header.Key}: {header.Value}");

                    // Extract the verification token
                    if (header.Key.Equals("X-Skedda-RequestVerificationToken", StringComparison.OrdinalIgnoreCase))
                    {
                        skeddaVerificationToken = header.Value;

                        Console.WriteLine($"Extracted Verification Token: {header.Value}");
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("WebResourceRequestedHandler");
            }
        }

        private async void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {

                if (e.IsSuccess)
                {
                    Console.WriteLine("Navigation completed successfully. Extracting cookies...");
                    await ExtractCookies();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("WebView_NavigationCompleted");
            }
        }

        private string xSkeddaApplicationCookie = null;
        private string skeddaVerificationCookie = null;
        private async Task ExtractCookies()
        {
            try
            {
                var cookies = await webView21.CoreWebView2.CookieManager.GetCookiesAsync("https://app.skedda.com");

                // Declare variables to hold the cookie values


                foreach (var cookie in cookies)
                {


                    // Capture specific cookies and store them in variables
                    if (cookie.Name.Equals("X-Skedda-ApplicationCookie", StringComparison.OrdinalIgnoreCase))
                    {
                        xSkeddaApplicationCookie = cookie.Value;
                        Console.WriteLine($"Extracted X-Skedda-ApplicationCookie: {cookie.Value}");
                    }
                    if (cookie.Name.Equals("X-Skedda-RequestVerificationCookie", StringComparison.OrdinalIgnoreCase))
                    {
                        skeddaVerificationCookie = cookie.Value;
                        Console.WriteLine($"Extracted SkeddaVerificationCookie: {cookie.Value}");
                    }
                }

                // Now you can use these variables elsewhere in your code
                Console.WriteLine($"Stored X-Skedda-ApplicationCookie: {xSkeddaApplicationCookie}");
                Console.WriteLine($"Stored SkeddaVerificationCookie: {skeddaVerificationCookie}");
                if (xSkeddaApplicationCookie != null && skeddaVerificationCookie != null && skeddaVerificationToken != null)
                {
                    await SendDataToServer();
                }

            }
            catch (Exception)
            {
                MessageBox.Show("ExtractCookies");
            }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Error extracting cookies: {ex.Message}");
            //}
        }


        private bool isNavigated = false;

        private async Task SendDataToServer()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Create the payload (data to send)
                    var payload = new
                    {
                        VerificationToken = skeddaVerificationToken,
                        ApplicationCookie = xSkeddaApplicationCookie,
                        VerificationCookie = skeddaVerificationCookie
                    };

                    var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

                    // Prepare the POST request
                    var buffer = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                    var byteContent = new ByteArrayContent(buffer);
                    byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                    // Send the request to the server (replace with your server URL)
                    var response = await client.PostAsync("https://abhroomserver.azurewebsites.net/tokens", byteContent);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Data successfully sent to the server.");

                        // Ensure navigation happens only once
                        if (!isNavigated)
                        {
                            // Redirect to the new URL
                            webView21.CoreWebView2.Navigate("https://abhroomtracker.azurewebsites.net/");
                            isNavigated = true;

                            // Delay for a few seconds to ensure redirection happens
                            await Task.Delay(1000);

                            // Close the form (which will close Skedda)
                            // this.Invoke((Action)(() => this.Close()));
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to send data: {response.StatusCode}");
                    }
                }

            }
            catch (Exception)
            {
                MessageBox.Show("SendDataToServer");

            }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Error sending data to server: {ex.Message}");
            //}
        }




    }
}
