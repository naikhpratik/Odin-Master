using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using System;
using System.Linq;

namespace Odin.UITests
{
    //[TestClass]
    public class UnitTest1
    {
        private string baseURL = "http://localhost:49986";
        private RemoteWebDriver driver;
        private string browser;
        public TestContext TestContext { get; set; }

        //[TestMethod]
        public void Chrome_Login_Logout()
        {
            driver = new ChromeDriver();
            driver.Manage().Window.Maximize();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
            driver.Navigate().GoToUrl(this.baseURL);

            Login("austin.emser@dwellworks.com", "Consultant5$");
            Logout();
        }

        private void Login(string username, string password)
        {
            if (driver.Url.Split('/').Last().Contains("Login"))
            {
                driver.FindElement(By.Id("Email")).SendKeys(username);
                driver.FindElement(By.Id("Password")).SendKeys(password);
                driver.FindElement(By.ClassName("btn-default")).Click();
            }
        }

        [TestCleanup()]
        public void CleanUp()
        {
            driver.Quit();
        }

        private void Logout()
        {
            driver.FindElement(By.Id("logoutForm")).Submit();
        }
    }
}
