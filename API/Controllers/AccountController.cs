using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        public AccountController(DataContext context , ITokenService tokenService)
        {
            _tokenService = tokenService;
            _context = context;
            
        }

    [HttpPost ("register")]
    public async Task<ActionResult<UserDto>> Register (RegisterDto registerDto)
    {
        if (await UserExists(registerDto.Username)) return new BadRequestObjectResult("Username is taken");


    using var hmac = new HMACSHA512();


   var user = new AppUser 
{
    UserNAme = registerDto.Username,
    PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
    PasswordSalt = hmac.Key // Assign the salt value to PasswordSalt
};
    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    return new UserDto
    {
Username = user.UserNAme,
Token=_tokenService.CreateToken(user)
    };

    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>>Login (LoginDto loginDto)
    {
        var user = await _context.Users.SingleOrDefaultAsync(x=>x.UserNAme==loginDto.Username);

        if(user==null) return new UnauthorizedObjectResult("Invalid Username");
            using var hmac = new HMACSHA512(user.PasswordSalt);
            var ComputedHash=hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (int i=0 ; i<ComputedHash.Length ; i++)
            {
                if (ComputedHash[i]!=user.PasswordHash[i]) return new UnauthorizedObjectResult("Invalid Password");
            }
    return new UserDto
    {
Username = user.UserNAme,
Token=_tokenService.CreateToken(user)
    };
   }



        private async Task<bool>UserExists(string username)
{
    return await _context.Users.AnyAsync(x=>x.UserNAme==username);
}



    }
}