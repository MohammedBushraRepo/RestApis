﻿using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Movies.Api.Auth;
using Movies.Api.Mapping;
using Movies.Application.Services;
using Movies.Contracts.Requests;
using Movies.Contracts.Responses;

namespace Movies.Api.Controllers;

[ApiController]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;
    private readonly IOutputCacheStore _outputCacheStore;

    public MoviesController(IMovieService movieService, IOutputCacheStore outputCacheStore)
    {
        _movieService = movieService;
        _outputCacheStore = outputCacheStore;
    }




    [ServiceFilter(typeof(ApiKeyAuthFilter))]//hint those services filter using api-key based authentication 
    [ApiVersion(1.0)]
    //[Authorize(AuthConstants.TrustedMemberPolicyName)]
    [HttpPost(ApiEndpoints.Movies.Create)]
    [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status201Created)]
    // [ProducesResponseType(typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMovieRequest request,
       CancellationToken token)
    {
        var movie = request.MapToMovie();
        await _movieService.CreateAsync(movie, token);
        await _outputCacheStore.EvictByTagAsync("movies", token);
        var movieResponse = movie.MapToResponse();
        return CreatedAtAction(nameof(Get), new { idOrSlug = movie.Id }, movieResponse);
    }

    // [ApiVersion(1.0)]
    // [Authorize]
    // [HttpGet(ApiEndpoints.Movies.Get)]
    // public async Task<IActionResult> GetV1([FromRoute] string idOrSlug,
    //         CancellationToken token)
    // {
    //     var userId = HttpContext.GetUserId();

    //     var movie = Guid.TryParse(idOrSlug, out var id)
    //         ? await _movieService.GetByIdAsync(id, userId, token)
    //         : await _movieService.GetBySlugAsync(idOrSlug, userId, token);
    //     if (movie is null)
    //     {
    //         return NotFound();
    //     }

    //     var response = movie.MapToResponse();
    //     return Ok(response);
    // }

    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    [ApiVersion(1.0)]
    //[Authorize] 
    [HttpGet(ApiEndpoints.Movies.Get)]
    // [OutputCache(PolicyName = "MovieCache")] // also disable authentiation in post man ==> No Auth in order for this to work 
    //[ResponseCache(Duration = 30, VaryByHeader = "Accept , Accept-Encoding ", Location = ResponseCacheLocation.Any)]
    // [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status200OK)]
    // [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string idOrSlug,
                  CancellationToken token)
    {
        var userId = HttpContext.GetUserId();

        var movie = Guid.TryParse(idOrSlug, out var id)
            ? await _movieService.GetByIdAsync(id, userId, token)
            : await _movieService.GetBySlugAsync(idOrSlug, userId, token);
        if (movie is null)
        {
            return NotFound();
        }

        var response = movie.MapToResponse();
        return Ok(response);
    }
    [ApiVersion(1.0)]
    [Authorize]
    [HttpGet(ApiEndpoints.Movies.GetAll)]
    [ProducesResponseType(typeof(MoviesResponse), StatusCodes.Status200OK)]
    //[ResponseCache(Duration = 30, VaryByQueryKeys = new []{"title", "year", "sortBy", "page", "pageSize"}, VaryByHeader = "Accept, Accept-Encoding", Location = ResponseCacheLocation.Any)]

    public async Task<IActionResult> GetAll(
    [FromQuery] GetAllMoviesRequest request,
    CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var options = request.MapToOptions()
        .WithUser(userId);
        var movies = await _movieService.GetAllAsync(options, token);
        var movieCount = await _movieService.GetCountAsync(options.Title, options.YearOfRelease, token);
        var moviesResponse = movies.MapToResponse(request.Page, request.PageSize, movieCount);
        return Ok(moviesResponse);
    }

    [ApiVersion(1.0)]
    [Authorize(AuthConstants.TrustedMemberPolicyName)]
    [HttpPut(ApiEndpoints.Movies.Update)]
    [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute] Guid id,
        [FromBody] UpdateMovieRequest request,
        CancellationToken token)
    {
        var movie = request.MapToMovie(id);
        var userId = HttpContext.GetUserId();
        var updatedMovie = await _movieService.UpdateAsync(movie, userId, token);
        if (updatedMovie is null)
        {
            return NotFound();
        }

        await _outputCacheStore.EvictByTagAsync("movies", token);
        var response = updatedMovie.MapToResponse();
        return Ok(response);
    }

    [ApiVersion(1.0)]
    [Authorize(AuthConstants.AdminUserPolicyName)]
    [HttpDelete(ApiEndpoints.Movies.Delete)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id,
        CancellationToken token)
    {
        var deleted = await _movieService.DeleteByIdAsync(id, token);

        if (!deleted)
        {
            return NotFound();
        }
        await _outputCacheStore.EvictByTagAsync("movies", token);
        return Ok();
    }
}
