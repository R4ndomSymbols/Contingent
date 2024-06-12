using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;
using Contingent.Auth;
using Contingent.Controllers.DTO.Out;
using Contingent.DTOs.In;
using Contingent.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Contingent.Controllers;

public class AuthController : Controller
{

    [HttpGet]
    [Route("/login")]
    [AllowAnonymous]
    public IActionResult GetLoginPage()
    {
        return View(@"Views/Auth/Login.cshtml");
    }
    [HttpPost]
    [AllowAnonymous]
    [Route("/login/token")]
    public IActionResult GetToken()
    {
        using var reader = new StreamReader(Request.Body);
        var body = reader.ReadToEndAsync().Result;
        LoginDTO? login;
        try
        {
            login = JsonSerializer.Deserialize<LoginDTO>(body);
        }
        catch
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный формат данных"));
        }
        if (login is null)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный формат данных"));
        }
        var userResult = ContingentUser.LogIn(login);
        if (userResult.IsSuccess)
        {
            var user = userResult.ResultObject;
            var identity = user.GetIdentity();
            var jwt = new JwtSecurityToken(
                issuer: Authentication.ISSUER,
                audience: Authentication.AUDIENCE,
                claims: identity.Claims,
                expires: DateTime.UtcNow.Add(TimeSpan.FromSeconds(Authentication.LIFETIME)),
                signingCredentials: new SigningCredentials(Authentication.JWTSecurityKey, SecurityAlgorithms.HmacSha256)
            );
            var encoded = new JwtSecurityTokenHandler().WriteToken(jwt);
            return Json(new { jwt = encoded });
        }
        return BadRequest(new ErrorCollectionDTO(new ValidationError("auth", "Неверный логин или пароль")));
    }




}