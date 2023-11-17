using System.Security.Cryptography;
using System.Text;
using API.controllers;
using API.Data;
using API.DTOs;
using API.Entites;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API;

public class AccountController : BaseApiController
{
	private readonly UserManager<AppUser> _userManager;
	private readonly ITokenService _tokenService;
	private readonly IMapper _mapper;

	public AccountController(UserManager<AppUser> userManager, ITokenService tokenService, IMapper mapper){
		_userManager = userManager;
		_tokenService = tokenService;
		_mapper = mapper;
	}

	[HttpPost("register")] // POST: api/account/register
	public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto){
		if (await UserExist(registerDto.Username)) return BadRequest("Username is take");

		var user = _mapper.Map<AppUser>(registerDto);

		user.UserName = registerDto.Username.ToLower();

		var result = await _userManager.CreateAsync(user, registerDto.Password);

		if (!result.Succeeded) return BadRequest(result.Errors); 

		return new UserDto {
			Username = user.UserName,
			Token = await _tokenService.CreateToken(user),
			KnownAs = user.KnownAs,
			Gender = user.Gender
		};
	}

	[HttpPost("login")]
	public async Task<ActionResult<UserDto>> Login(LoginDto loginDto){
		var user = await _userManager.Users
		.Include(p => p.Photos)
		.SingleOrDefaultAsync(x => x.UserName == loginDto.Username);

		if (user == null) return Unauthorized("No user with that username.");

		var result = await _userManager.CheckPasswordAsync(user, loginDto.Password);
		
		if (!result) return Unauthorized("Invalid password");

		var roleResult = await _userManager.AddToRoleAsync(user, "Member");

		if (!roleResult.Succeeded) return BadRequest(roleResult.Errors);
		
		return new UserDto {
			Username = user.UserName,
			Token = await _tokenService.CreateToken(user),
			PhotUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
			KnownAs = user.KnownAs,
			Gender = user.Gender
		};
	}

	private async Task<bool> UserExist(string username){
		return await _userManager.Users.AnyAsync(x => x.UserName == username.ToLower());
	}
}
