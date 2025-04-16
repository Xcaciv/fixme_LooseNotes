using System;
using System.Diagnostics;
using System.Text;
using System.Xml;
using Microsoft.Data.SqlClient; // Replace obsolete System.Data.SqlClient
using Microsoft.AspNetCore.Mvc;
using Xcaciv.LooseNotes.Web.Models;
using Xcaciv.LooseNotes.Web.Services;

namespace Xcaciv.LooseNotes.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly INoteService _noteService;

    public HomeController(ILogger<HomeController> logger, INoteService noteService)
    {
        _logger = logger;
        _noteService = noteService;
    }

    public IActionResult Index()
    {
        // Insecure: Log user's IP address without consent
        _logger.LogInformation($"Home page visited from IP: {HttpContext.Connection.RemoteIpAddress}");
        
        // Insecure: Get public notes with SQL injection vulnerability
        var publicNotes = _noteService.SearchNotes("1=1");
        
        // Filter to only public notes client-side
        var displayNotes = publicNotes.Where(n => n.IsPublic).Take(5).ToList();
        return View(displayNotes);
    }

    [HttpPost]
    public IActionResult ProcessXml(string xmlInput)
    {
        try
        {
            // Insecure: XML External Entity (XXE) vulnerability
            XmlDocument xmlDoc = new XmlDocument();
            
            // Insecure: XXE processing enabled
            xmlDoc.XmlResolver = new XmlUrlResolver();
            xmlDoc.LoadXml(xmlInput);
            
            ViewBag.XmlResult = "XML processed successfully";
            return View("XmlProcessor");
        }
        catch (Exception ex)
        {
            // Insecure: Full exception details exposed
            ViewBag.Error = ex.ToString();
            return View("XmlProcessor");
        }
    }
    
    public IActionResult XmlProcessor()
    {
        return View();
    }
    
    public IActionResult TestDbConnection(string connectionString)
    {
        try
        {
            // Insecure: Allow testing arbitrary connection strings
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                ViewBag.Result = "Connection successful!";
            }
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.ToString();
        }
        
        return View();
    }

    public IActionResult Privacy()
    {
        // Insecure: Storing sensitive data in ViewBag without protection
        ViewBag.ApiKey = "sk_test_abcdef123456789";
        ViewBag.SecretToken = "very_secret_token_do_not_share";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
