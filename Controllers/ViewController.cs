using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.Models;

namespace StudentTracking.Controllers;
public class ViewController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public ViewController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }
    public IActionResult Index(string query)
    {
        List<StudentModel> model = new List<StudentModel>();
        if (string.IsNullOrWhiteSpace(query)){
            model = StudentModel.GetAllStudents();
        }
        else if (query.Length > 1){
            model = StudentModel.GetStudentsBySearchText(query.Trim());
        }
        return View(model);
    }
}
